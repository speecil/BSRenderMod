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
                "h264_nvenc", // Nvidia
                "h264_amf", // AMD
                "h264_qsv", // QSV
                "libx264" // CPU (Fallback)
            };

            foreach (var encoder in possibleEncoders)
            {
                if (TestEncoder(encoder))
                    return encoder;
            }

            return "libx264"; // Only if the others fail
        }

        public static bool TestEncoder(string encoder)
        {
            try
            {
                string ffmpegPath = Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe");
                
                if (!File.Exists(ffmpegPath))
                    ffmpegPath = "ffmpeg";

                using (var proc = new Process())
                {
                    proc.StartInfo.FileName = ffmpegPath;
                    proc.StartInfo.Arguments = $"-hide_banner -f lavfi -i testsrc=size=1280x720:rate=30 -c:v {encoder} -t 1 -f null -"; // Actually test encoders
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
                Debug.WriteLine($"FFmpeg encoder test failed for {encoder}: {ex.Message}"); // Error message if it fails
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
