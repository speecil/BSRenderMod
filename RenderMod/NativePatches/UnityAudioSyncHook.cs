using System;
using System.Runtime.InteropServices;
using System.Threading;
using IPALogger = IPA.Logging.Logger;

namespace RenderMod.Render
{
    // 1.40.8 tested
    // this patch was adapted from HollywoodQuest
    // additionally, this patch was assisted with claudes help in understanding the assembly and how to hook it properly as native patching is new to me
    internal static unsafe class UnityAudioSyncHook
    {
        // FMOD output mix, UnityPlayer.dll: base + 0x171EFF0.
        private const long KnownRva = 0x171EFF0;

        private static readonly int[] MixPattern =
        {
            0x48, 0x89, 0x5C, 0x24, 0x18,       // mov [rsp+18h], rbx
            0x48, 0x89, 0x54, 0x24, 0x10,       // mov [rsp+10h], rdx
            0x48, 0x89, 0x4C, 0x24, 0x08,       // mov [rsp+8], rcx
            0x55, 0x56, 0x57,                   // push rbp, rsi, rdi
            0x41, 0x54, 0x41, 0x55,             // push r12, r13
            0x41, 0x56, 0x41, 0x57,             // push r14, r15
            0x48, 0x81, 0xEC, -1, -1, -1, -1,   // sub rsp, imm32
            0x4C, 0x8B, 0x49, 0x60,             // mov r9, [rcx+60h]  (output->system)
            0x45, 0x33, 0xED,                   // xor r13d, r13d
            0x41, 0x8B, 0xF8,                   // mov edi, r8d
            0x4C, 0x8B, 0xE2                    // mov r12, rdx
        };

        private const int StolenBytes = 15;
        private const int JumpSize = 12; // mov rax, imm64; jmp rax

        private const int TimeoutLoops = 200;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int MixFunction(IntPtr output, IntPtr buffer, uint length);

        private static MixFunction _hookDelegate;
        private static MixFunction _original;

        private static IntPtr _lastOutput;
        private static bool _syncTimes;
        private static int _sampleRate;
        private static float _startGameTime;
        private static float _currentGameTime;
        private static long _startDspClock;
        private static long _lastGameDeltaSamples;
        private static int _timeout;
        private static int _timeoutCount;

        public static bool Installed => _original != null;
        public static bool SyncTimes => Volatile.Read(ref _syncTimes);

        // times the audio thread stopped waiting for the main thread
        public static int TimeoutCount => Volatile.Read(ref _timeoutCount);

        public static void Install(IPALogger log)
        {
            if (Installed)
                return;

            IntPtr module = GetModuleHandle("UnityPlayer.dll");
            if (module == IntPtr.Zero)
                throw new InvalidOperationException("UnityPlayer.dll is not loaded.");

            IntPtr target = FindMixFunction((byte*)module);
            if (target == IntPtr.Zero)
                throw new InvalidOperationException("FMOD output mix function not found in UnityPlayer.dll (unsupported Unity build).");

            // trampoline
            IntPtr trampoline = VirtualAlloc(IntPtr.Zero, (UIntPtr)(StolenBytes + JumpSize), MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
            if (trampoline == IntPtr.Zero)
                throw new InvalidOperationException("VirtualAlloc failed.");

            Buffer.MemoryCopy((void*)target, (void*)trampoline, StolenBytes, StolenBytes);
            WriteAbsoluteJump((byte*)trampoline + StolenBytes, (byte*)target + StolenBytes);

            _hookDelegate = Hook;
            _original = Marshal.GetDelegateForFunctionPointer<MixFunction>(trampoline);
            IntPtr hook = Marshal.GetFunctionPointerForDelegate(_hookDelegate);

            if (!VirtualProtect(target, (UIntPtr)StolenBytes, PAGE_EXECUTE_READWRITE, out uint oldProtect))
            {
                _original = null;
                VirtualFree(trampoline, UIntPtr.Zero, MEM_RELEASE);
                throw new InvalidOperationException($"VirtualProtect failed: {Marshal.GetLastWin32Error()}");
            }

            WriteAbsoluteJump((byte*)target, (byte*)hook);
            for (int i = JumpSize; i < StolenBytes; i++)
                ((byte*)target)[i] = 0x90;

            VirtualProtect(target, (UIntPtr)StolenBytes, oldProtect, out _);
            FlushInstructionCache(GetCurrentProcess(), target, (UIntPtr)StolenBytes);

            log.Notice($"Audio sync hook installed at UnityPlayer.dll+0x{(long)target - (long)module:X}");
        }

        public static void SetSyncTimes(bool value)
        {
            if (!value)
            {
                Volatile.Write(ref _syncTimes, false);
                return;
            }

            if (!Installed)
                throw new InvalidOperationException("Audio hook is not installed.");

            // the output pointer is captured on the first mix call, so the clock may briefly read 0
            long dspClock = GetDSPClock();
            for (int i = 0; dspClock == 0 && i < 200; i++)
            {
                Thread.Sleep(5);
                dspClock = GetDSPClock();
            }
            if (dspClock == 0)
                throw new InvalidOperationException("FMOD DSP clock was not available.");

            float gameTime = UnityEngine.Time.time;
            Volatile.Write(ref _sampleRate, UnityEngine.AudioSettings.outputSampleRate);
            Volatile.Write(ref _startGameTime, gameTime);
            Volatile.Write(ref _currentGameTime, gameTime);
            Interlocked.Exchange(ref _startDspClock, dspClock);
            Interlocked.Exchange(ref _lastGameDeltaSamples, 0);
            Interlocked.Exchange(ref _timeout, 0);
            Interlocked.Exchange(ref _timeoutCount, 0);
            Volatile.Write(ref _syncTimes, true);
        }

        public static void UpdateGameTime()
        {
            if (SyncTimes)
                Volatile.Write(ref _currentGameTime, UnityEngine.Time.time);
        }

        // main thread side: called once per frame before rendering
        public static void WaitForAudio()
        {
            while (SyncTimes)
            {
                UpdateGameTime();
                if (GameDeltaSamples() < DspDelta() + 1)
                    break;
                Thread.Sleep(5);
            }
        }

        private static long DspDelta() => GetDSPClock() - Interlocked.Read(ref _startDspClock);

        private static long GameDeltaSamples() =>
            (long)((Volatile.Read(ref _currentGameTime) - Volatile.Read(ref _startGameTime)) * Volatile.Read(ref _sampleRate));

        private static long GetDSPClock()
        {
            IntPtr output = Volatile.Read(ref _lastOutput);
            if (output == IntPtr.Zero)
                return 0;

            IntPtr system = Marshal.ReadIntPtr(output + 0x60);
            return system == IntPtr.Zero ? 0 : Marshal.ReadInt64(system + 0xC78);
        }

        // audio thread side: runs on FMOD's mixer thread before every mix
        private static int Hook(IntPtr output, IntPtr buffer, uint length)
        {
            Volatile.Write(ref _lastOutput, output);

            while (SyncTimes)
            {
                long gameDelta = GameDeltaSamples();
                if (DspDelta() < gameDelta)
                    break;

                // only time out if game time has stopped moving (main thread stalled or gone)
                if (gameDelta != Interlocked.Exchange(ref _lastGameDeltaSamples, gameDelta))
                    Interlocked.Exchange(ref _timeout, 0);

                if (Interlocked.Increment(ref _timeout) > TimeoutLoops)
                {
                    Interlocked.Increment(ref _timeoutCount);
                    break;
                }

                Thread.Sleep(5);
            }

            return _original(output, buffer, length);
        }

        private static IntPtr FindMixFunction(byte* module)
        {
            byte* pe = module + *(int*)(module + 0x3C);
            uint sizeOfImage = *(uint*)(pe + 24 + 56);

            if (KnownRva + MixPattern.Length <= sizeOfImage && MatchesPattern(module + KnownRva))
                return (IntPtr)(module + KnownRva);

            int sectionCount = *(ushort*)(pe + 6);
            byte* section = pe + 24 + *(ushort*)(pe + 20);
            for (int i = 0; i < sectionCount; i++, section += 40)
            {
                if (Marshal.PtrToStringAnsi((IntPtr)section, 8).TrimEnd('\0') != ".text")
                    continue;

                byte* start = module + *(uint*)(section + 12);
                byte* end = start + *(uint*)(section + 8) - MixPattern.Length;
                IntPtr found = IntPtr.Zero;
                for (byte* p = start; p <= end; p++)
                {
                    if (*p != MixPattern[0] || !MatchesPattern(p))
                        continue;
                    if (found != IntPtr.Zero)
                        return IntPtr.Zero; // ambiguous, refuse to hook
                    found = (IntPtr)p;
                }
                return found;
            }
            return IntPtr.Zero;
        }

        private static bool MatchesPattern(byte* address)
        {
            for (int i = 0; i < MixPattern.Length; i++)
            {
                if (MixPattern[i] >= 0 && address[i] != (byte)MixPattern[i])
                    return false;
            }
            return true;
        }

        private static void WriteAbsoluteJump(byte* at, byte* destination)
        {
            at[0] = 0x48; at[1] = 0xB8; // mov rax, imm64
            *(ulong*)(at + 2) = (ulong)destination;
            at[10] = 0xFF; at[11] = 0xE0; // jmp rax
        }

        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint MEM_RELEASE = 0x8000;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAlloc(IntPtr address, UIntPtr size, uint allocationType, uint protect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFree(IntPtr address, UIntPtr size, uint freeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr address, UIntPtr size, uint newProtect, out uint oldProtect);

        [DllImport("kernel32.dll")]
        private static extern bool FlushInstructionCache(IntPtr process, IntPtr address, UIntPtr size);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string moduleName);
    }
}
