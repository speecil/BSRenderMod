using IPA.Utilities;
using System;
using System.Diagnostics;
using System.IO;

namespace RenderMod.Render
{
    public static class EncoderHelpers
    {
        private static readonly string FFmpegPath =
            Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe");

        public static string SelectBestEncoder()
        {
            string encodersOutput = GetEncodersOutput();

            if (IsEncoderUsable("h264_nvenc", encodersOutput))
                return "h264_nvenc";

            if (IsEncoderUsable("h264_amf", encodersOutput))
                return "h264_amf";

            if (IsEncoderUsable("h264_qsv", encodersOutput))
                return "h264_qsv";

            return "libx264";
        }

        private static string GetEncodersOutput()
        {
            try
            {
                var proc = new Process();
                proc.StartInfo.FileName = FFmpegPath;
                proc.StartInfo.Arguments = "-hide_banner -encoders";
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;

                proc.Start();
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                proc.Close();
                proc.Dispose();

                return output;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsEncoderUsable(string encoder, string encodersOutput)
        {
            if (!encodersOutput.Contains(encoder))
                return false;

            return TestEncoder(encoder);
        }

        public static bool TestEncoder(string encoder)
        {
            try
            {
                var proc = new Process();
                proc.StartInfo.FileName = FFmpegPath;
                proc.StartInfo.Arguments =
                    $"-hide_banner -loglevel error " +
                    $"-f lavfi -i testsrc=size=1280x720:rate=30 " +
                    $"-c:v {encoder} -t 1 -f null -";

                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;

                proc.Start();
                proc.WaitForExit();

                int exitCode = proc.ExitCode;
                proc.Close();
                proc.Dispose();

                return exitCode == 0;
            }
            catch
            {
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
                    {
                        presetArgs = $"-preset fast -rc vbr_hq -cq 28 {bitrateArg}";
                    }
                    else if (encoder.Contains("amf"))
                    {
                        presetArgs = $"-quality speed -q 28 {bitrateArg}";
                    }
                    else if (encoder.Contains("qsv"))
                    {
                        presetArgs = $"-preset veryfast -global_quality 30 {bitrateArg}";
                    }
                    else
                    {
                        presetArgs = $"-preset ultrafast -crf 28 {bitrateArg}";
                    }
                    break;

                case QualityPreset.Medium:
                    if (encoder.Contains("nvenc"))
                    {
                        presetArgs = $"-preset medium -rc vbr_hq -cq 23 {bitrateArg}";
                    }
                    else if (encoder.Contains("amf"))
                    {
                        presetArgs = $"-quality balanced -q 23 {bitrateArg}";
                    }
                    else if (encoder.Contains("qsv"))
                    {
                        presetArgs = $"-preset medium -global_quality 25 {bitrateArg}";
                    }
                    else
                    {
                        presetArgs = $"-preset medium -crf 23 {bitrateArg}";
                    }
                    break;

                case QualityPreset.High:
                    if (encoder.Contains("nvenc"))
                    {
                        presetArgs = $"-preset slow -rc vbr_hq -cq 18 {bitrateArg}";
                    }
                    else if (encoder.Contains("amf"))
                    {
                        presetArgs = $"-quality quality -q 18 {bitrateArg}";
                    }
                    else if (encoder.Contains("qsv"))
                    {
                        presetArgs = $"-preset slow -global_quality 20 {bitrateArg}";
                    }
                    else
                    {
                        presetArgs = $"-preset slow -crf 18 {bitrateArg}";
                    }
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
