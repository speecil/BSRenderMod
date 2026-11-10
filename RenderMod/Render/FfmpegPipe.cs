using IPA.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace RenderMod.Render
{
    internal class FFmpegPipe
    {
        public static readonly string FFmpegPath = Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe");

        public static string RawFormat => ReplayRenderSettings.VideoCodec == "av1" ? "ivf" : ReplayRenderSettings.VideoCodec;

        private Process _ffmpeg;
        private Stream _input;
        private bool _closed;

        public FFmpegPipe(string args)
        {
            _ffmpeg = new Process { StartInfo = CreateStartInfo(args) };
            _ffmpeg.StartInfo.RedirectStandardInput = true;

            try
            {
                _ffmpeg.Start();
                _ffmpeg.BeginErrorReadLine(); // stop it from blocking
                _input = _ffmpeg.StandardInput.BaseStream;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start FFmpeg: {ex}");
            }
        }

        public void WriteFrame(byte[] frame)
        {
            if (_closed) return;

            try
            {
                _input?.Write(frame, 0, frame.Length);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogError($"FFmpeg WriteFrame failed: {ex.Message}");
                Close();
            }
        }

        public void Close()
        {
            if (_closed) return;
            _closed = true;

            try
            {
                _input?.Close();
                _input = null;
                if (_ffmpeg != null && !_ffmpeg.HasExited)
                    _ffmpeg.WaitForExit();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error closing FFmpeg: {ex.Message}");
            }

            _ffmpeg?.Dispose();
            _ffmpeg = null;
        }

        public static bool RemuxRawStreamToMp4(string rawStreamPath, string outputMp4Path, int fps)
        {
            if (!File.Exists(rawStreamPath))
            {
                Debug.LogError($"Raw video file not found: {rawStreamPath}");
                return false;
            }

            return Run($"-y -r {fps} -f {RawFormat} -i \"{rawStreamPath}\" -c:v copy \"{outputMp4Path}\"", "video remux")
                && File.Exists(outputMp4Path);
        }

        public static bool AddAudioToMp4(string videoMp4Path, string audioPath, string finalMp4Path, int audioBitrateKbps)
        {
            if (!File.Exists(audioPath))
            {
                Debug.LogError($"Audio WAV not found: {audioPath}");
                return false;
            }

            bool ok = Run(
                $"-y -i \"{videoMp4Path}\" -i \"{audioPath}\" " +
                $"-map 0:v:0 -map 1:a:0 -c:v copy -c:a aac -b:a {audioBitrateKbps}k " +
                $"-shortest -movflags +faststart \"{finalMp4Path}\"",
                "audio mux");

            try
            {
                File.Delete(videoMp4Path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to delete intermediate MP4: {ex}");
            }

            return ok && File.Exists(finalMp4Path);
        }

        // runs ffmpeg to completion, log stderr if it fails
        public static bool Run(string args, string what)
        {
            return Run(args, out string error) || Fail(what, error);
        }

        public static bool Run(string args, out string output)
        {
            try
            {
                using (var proc = new Process { StartInfo = CreateStartInfo(args) })
                {
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.Start();
                    // stop blocking
                    var stdout = proc.StandardOutput.ReadToEndAsync();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();
                    output = stdout.Result + stderr;
                    return proc.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                output = ex.ToString();
                return false;
            }
        }

        private static bool Fail(string what, string error)
        {
            Debug.LogError($"FFmpeg {what} failed: {error}");
            return false;
        }

        private static ProcessStartInfo CreateStartInfo(string args) => new ProcessStartInfo(FFmpegPath, args)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };
    }
}
