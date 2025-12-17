using IPA.Utilities;
using System;
using System.Diagnostics;
using System.IO;

namespace RenderMod.Render
{
    public static class EncoderHelpers
    {
        public static string SelectBestEncoder()
        {
            var possibleEncoders = new[]
            {
                "h264_nvenc", // NVIDIA
                "h264_amf",   // AMD
                "h264_qsv",   // Intel
                "libx264"     // CPU fallback
            };

            foreach (var encoder in possibleEncoders)
            {
                if (CheckFFmpegSupportsEncoder(encoder))
                    return encoder;
            }

            return "libx264"; // fallback (worst case scenario)
        }

        private static bool CheckFFmpegSupportsEncoder(string encoder)
        {
            try
            {
                using (var proc = new Process())
                {
                    proc.StartInfo.FileName = Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe");
                    proc.StartInfo.Arguments = "-hide_banner -encoders";
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.Start();

                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();

                    return TestEncoder(encoder) && output.Contains(encoder);
                }
            }
            catch
            {
                return false;
            }
        }

        //test encoder and check for exit 0
        public static bool TestEncoder(string encoder)
        {
            try
            {
                using (var proc = new Process())
                {
                    proc.StartInfo.FileName = Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe");
                    proc.StartInfo.Arguments = $"-hide_banner -f lavfi -i testsrc=size=1280x720:rate=30 -c:v {encoder} -t 1 -f null -";
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.CreateNoWindow = true;

                    proc.Start();

                    proc.WaitForExit();

                    return proc.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FFmpeg encoder test failed: {ex}");
                return false;
            }
        }


        public static (string encoder, string args) BuildEncoderArgs()
        {
            var encoder = SelectBestEncoder();
            return BuildEncoderArgs(encoder);
        }

        public static (string encoder, string args) BuildEncoderArgs(string encoder)
        {
            string presetArgs = "";
            string bitrateArg = $"-b:v {ReplayRenderSettings.BitrateKbps}k";

            switch (ReplayRenderSettings.Preset)
            {
                case QualityPreset.Low:
                    if (encoder.Contains("nvenc"))
                        presetArgs = $"-preset fast -rc vbr_hq -cq 28 {bitrateArg}";
                    else if (encoder.Contains("amf"))
                        presetArgs = $"-quality speed -q 28 {bitrateArg}";
                    else if (encoder.Contains("qsv"))
                        presetArgs = $"-preset veryfast -global_quality 30 {bitrateArg}";
                    else
                        presetArgs = $"-preset ultrafast -crf 28 {bitrateArg}";
                    break;

                case QualityPreset.Medium:
                    if (encoder.Contains("nvenc"))
                        presetArgs = $"-preset medium -rc vbr_hq -cq 23 {bitrateArg}";
                    else if (encoder.Contains("amf"))
                        presetArgs = $"-quality balanced -q 23 {bitrateArg}";
                    else if (encoder.Contains("qsv"))
                        presetArgs = $"-preset medium -global_quality 25 {bitrateArg}";
                    else
                        presetArgs = $"-preset medium -crf 23 {bitrateArg}";
                    break;

                case QualityPreset.High:
                    if (encoder.Contains("nvenc"))
                        presetArgs = $"-preset slow -rc vbr_hq -cq 18 {bitrateArg}";
                    else if (encoder.Contains("amf"))
                        presetArgs = $"-quality quality -q 18 {bitrateArg}";
                    else if (encoder.Contains("qsv"))
                        presetArgs = $"-preset slow -global_quality 20 {bitrateArg}";
                    else
                        presetArgs = $"-preset slow -crf 18 {bitrateArg}";
                    break;
            }

            string args =
                $"-c:v {encoder} " +
                $"{presetArgs} " +
                $"-pix_fmt {ReplayRenderSettings.PixelFormat} " +
                $"{ReplayRenderSettings.ExtraFFmpegArgs}";

            return (encoder, args);
        }
    }
}
