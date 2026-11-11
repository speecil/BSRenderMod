using System;
using System.Linq;
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
        private class MixTarget
        {
            public string Version;
            public long Rva;
            public int[] Pattern;
            public int StolenBytes;
        }

        private static readonly MixTarget[] Targets =
        {
            new MixTarget
            {
                Version = "1.40.8",
                Rva = 0x171EFF0,
                StolenBytes = 15, // mov [rsp+18h], rbx; mov [rsp+10h], rdx; mov [rsp+8], rcx
                Pattern = ParsePattern(
                    "48 89 5C 24 18 48 89 54 24 10 48 89 4C 24 08 55 56 57 41 54 41 55 41 56 41 57 " +
                    "48 81 EC ?? ?? ?? ?? 4C 8B 49 60 45 33 ED 41 8B F8 4C 8B E2")
            },
            new MixTarget
            {
                Version = "1.29.1",
                Rva = 0xF92F50,
                StolenBytes = 12, // mov rax, rsp; mov [rax+10h], rdx; mov [rax+8], rcx; push rbp
                Pattern = ParsePattern(
                    "48 8B C4 48 89 50 10 48 89 48 08 55 56 57 41 54 41 55 41 56 41 57 48 81 EC ?? ?? ?? ?? " +
                    "48 C7 80 38 FF FF FF FE FF FF FF 48 89 58 18 0F 29 70 B8 0F 29 78 A8 44 0F 29 40 98 " +
                    "44 0F 29 48 88 44 0F 29 90 78 FF FF FF 44 0F 29 98 68 FF FF FF 44 0F 29 A0 58 FF FF FF " +
                    "44 0F 29 A8 48 FF FF FF 41 8B F8 48 8B F1 45 33 E4 45 8B F4 44 89 64 24 40 4C 8B 49 60")
            },
            new MixTarget
            {
                // same UnityPlayer.dll as 1.40.13 - 1.44.3
                Version = "1.42.3",
                Rva = 0x175BA50,
                StolenBytes = 15, // mov [rsp+18h], rbx; mov [rsp+10h], rdx; mov [rsp+8], rcx
                Pattern = ParsePattern(
                    "48 89 5C 24 18 48 89 54 24 10 48 89 4C 24 08 55 56 57 41 54 41 55 41 56 41 57 " +
                    "48 81 EC ?? ?? ?? ?? 48 8B 41 60 45 33 D2 45 8B F0 48 8B F9 44 89 94 24 28 01 00 00 " +
                    "45 8B EA 48 8B 98 10 0C 00 00 48 8B A8 18 0C 00 00")
            },
        };

        private const int JumpSize = 12;         // mov rax, imm64; jmp rax (entry patch, rax is free at entry)
        private const int IndirectJumpSize = 14; // jmp [rip+0]; dq target (trampoline return, keeps rax: 1.29.1 uses it as a frame pointer)

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

            IntPtr target = FindMixFunction((byte*)module, out MixTarget mix);
            if (target == IntPtr.Zero)
                throw new InvalidOperationException("FMOD output mix function not found in UnityPlayer.dll (unsupported Unity build).");
            int stolenBytes = mix.StolenBytes;

            // trampoline
            IntPtr trampoline = VirtualAlloc(IntPtr.Zero, (UIntPtr)(stolenBytes + IndirectJumpSize), MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
            if (trampoline == IntPtr.Zero)
                throw new InvalidOperationException("VirtualAlloc failed.");

            Buffer.MemoryCopy((void*)target, (void*)trampoline, stolenBytes, stolenBytes);
            WriteIndirectJump((byte*)trampoline + stolenBytes, (byte*)target + stolenBytes);

            _hookDelegate = Hook;
            _original = Marshal.GetDelegateForFunctionPointer<MixFunction>(trampoline);
            IntPtr hook = Marshal.GetFunctionPointerForDelegate(_hookDelegate);

            if (!VirtualProtect(target, (UIntPtr)stolenBytes, PAGE_EXECUTE_READWRITE, out uint oldProtect))
            {
                _original = null;
                VirtualFree(trampoline, UIntPtr.Zero, MEM_RELEASE);
                throw new InvalidOperationException($"VirtualProtect failed: {Marshal.GetLastWin32Error()}");
            }

            WriteAbsoluteJump((byte*)target, (byte*)hook);
            for (int i = JumpSize; i < stolenBytes; i++)
                ((byte*)target)[i] = 0x90;

            VirtualProtect(target, (UIntPtr)stolenBytes, oldProtect, out _);
            FlushInstructionCache(GetCurrentProcess(), target, (UIntPtr)stolenBytes);

            log.Notice($"Audio sync hook installed for {mix.Version} at UnityPlayer.dll+0x{(long)target - (long)module:X}");
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

        private static IntPtr FindMixFunction(byte* module, out MixTarget mix)
        {
            byte* pe = module + *(int*)(module + 0x3C);
            uint sizeOfImage = *(uint*)(pe + 24 + 56);

            foreach (var target in Targets)
            {
                mix = target;
                if (target.Rva + target.Pattern.Length <= sizeOfImage && MatchesPattern(module + target.Rva, target.Pattern))
                    return (IntPtr)(module + target.Rva);
            }

            int sectionCount = *(ushort*)(pe + 6);
            byte* section = pe + 24 + *(ushort*)(pe + 20);
            for (int i = 0; i < sectionCount; i++, section += 40)
            {
                if (Marshal.PtrToStringAnsi((IntPtr)section, 8).TrimEnd('\0') != ".text")
                    continue;

                byte* start = module + *(uint*)(section + 12);
                foreach (var target in Targets)
                {
                    mix = target;
                    IntPtr found = ScanForPattern(start, *(uint*)(section + 8), target.Pattern);
                    if (found != IntPtr.Zero)
                        return found;
                }
                break;
            }

            mix = null;
            return IntPtr.Zero;
        }

        // returns the only match, or zero if there are none or more than one
        private static IntPtr ScanForPattern(byte* start, uint length, int[] pattern)
        {
            byte* end = start + length - pattern.Length;
            IntPtr found = IntPtr.Zero;
            for (byte* p = start; p <= end; p++)
            {
                if (*p != pattern[0] || !MatchesPattern(p, pattern))
                    continue;
                if (found != IntPtr.Zero)
                    return IntPtr.Zero;
                found = (IntPtr)p;
            }
            return found;
        }

        private static bool MatchesPattern(byte* address, int[] pattern)
        {
            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i] >= 0 && address[i] != (byte)pattern[i])
                    return false;
            }
            return true;
        }

        private static int[] ParsePattern(string pattern) =>
            pattern.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(b => b == "??" ? -1 : Convert.ToInt32(b, 16))
                .ToArray();

        private static void WriteAbsoluteJump(byte* at, byte* destination)
        {
            at[0] = 0x48; at[1] = 0xB8; // mov rax, imm64
            *(ulong*)(at + 2) = (ulong)destination;
            at[10] = 0xFF; at[11] = 0xE0; // jmp rax
        }

        private static void WriteIndirectJump(byte* at, byte* destination)
        {
            at[0] = 0xFF; at[1] = 0x25; // jmp qword ptr [rip+0]
            *(int*)(at + 2) = 0;
            *(ulong*)(at + 6) = (ulong)destination;
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
