using System;
using System.IO;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace RenderMod.Render
{
    public static class EncoderHelpers
    {
        // these messages should hopefully be enough to help users
        // ffmpeg should be the latest release build
        private static readonly (string error, string hint)[] FailureHints =
        {
            ("required nvenc API version", "the NVIDIA driver is too old for this FFmpeg build. Update the driver or use an older FFmpeg"),
            ("minimum required Nvidia driver", "the NVIDIA driver is too old for this FFmpeg build. Update the driver or use an older FFmpeg"),
            ("Cannot get the preset configuration", "the FFmpeg build in Beat Saber/Libs is too old for this NVIDIA driver. Replace Libs/ffmpeg.exe with a newer build"),
            ("Cannot load nvcuda.dll", "the NVIDIA driver could not be loaded"),
            ("No NVENC capable devices found", "no NVENC capable NVIDIA GPU was found"),
            ("OpenEncodeSessionEx failed", "an NVENC session could not be opened. Update the driver or close other recording software"),
            ("amfrt64.dll", "the AMD encoder runtime is not installed or no supported AMD GPU was found"),
            ("Failed to create", "an AMD encoder device could not be created"),
            ("MFX session", "Intel Quick Sync is not supported or is disabled"),
            ("mfx implementation", "Intel Quick Sync is not supported or is disabled"),
        };

        public static string SelectBestEncoder()
        {
            string codec = ReplayRenderSettings.VideoCodec ?? "h264";
            string software = codec == "hevc" ? "libx265" : codec == "av1" ? "libaom-av1" : "libx264";

            FFmpegPipe.Run("-hide_banner -encoders", out string encoders);
            if (string.IsNullOrWhiteSpace(encoders))
            {
                Debug.LogWarning("FFmpeg -encoders returned no output. Falling back to software encoding.");
                return software;
            }

            foreach (string hw in new[] { "nvenc", "amf", "qsv" })
            {
                string encoder = $"{codec}_{hw}";
                if (IsListed(encoder, encoders) && TestEncoder(encoder))
                {
                    Debug.Log("Selected usable encoder: " + encoder);
                    return encoder;
                }
            }

            Debug.LogWarning($"No usable hardware {codec} encoder found. Falling back to {software}.");
            return software;
        }

        private static bool IsListed(string encoder, string encoders)
        {
            using (var reader = new StringReader(encoders))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && parts[1] == encoder)
                        return true;
                }
            }
            return false;
        }

        public static bool TestEncoder(string encoder)
        {
            bool hardware = encoder.Contains("nvenc") || encoder.Contains("amf") || encoder.Contains("qsv");
            string args =
                "-hide_banner -loglevel error -f lavfi -i testsrc=size=1280x720:rate=30 -frames:v 1 " +
                $"-vf format={(hardware ? "nv12" : "yuv420p")} -c:v {encoder} " +
                (encoder.Contains("nvenc") ? "-preset medium " : "") +
                "-f null -";

            if (FFmpegPipe.Run(args, out string error))
                return true;

            string hint = FailureHints.FirstOrDefault(h => error.IndexOf(h.error, StringComparison.OrdinalIgnoreCase) >= 0).hint;
            Debug.LogWarning($"{encoder} is unavailable{(hint != null ? ": " + hint : "")}.\nFFmpeg error:\n{error}");
            return false;
        }

        private static string _preparedEncoder, _preparedCodec;

        public static string PrepareEncoder()
        {
            _preparedCodec = ReplayRenderSettings.VideoCodec;
            return _preparedEncoder = SelectBestEncoder();
        }

        public static (string encoder, string args) BuildEncoderArgs()
        {
            bool prepared = _preparedEncoder != null && _preparedCodec == ReplayRenderSettings.VideoCodec;
            return BuildEncoderArgs(prepared ? _preparedEncoder : PrepareEncoder());
        }

        public static (string encoder, string args) BuildEncoderArgs(string encoder)
        {
            string bitrate = $"-b:v {ReplayRenderSettings.BitrateKbps}k";
            var p = Presets[(int)ReplayRenderSettings.Preset];

            string presetArgs;
            if (encoder.Contains("av1_nvenc"))
                presetArgs = $"-preset {p.nvenc} -rc vbr {(ReplayRenderSettings.Preset == QualityPreset.High ? "-tune hq " : "")}{bitrate}";
            else if (encoder.Contains("nvenc"))
                presetArgs = $"-preset {p.nvenc} -rc vbr -cq {p.crf} {bitrate}";
            else if (encoder.Contains("amf"))
                presetArgs = $"-quality {p.amf} -q {p.crf} {bitrate}";
            else if (encoder.Contains("qsv"))
                presetArgs = $"-preset {p.qsv} -global_quality {p.qsvQuality} {bitrate}";
            else if (encoder.Contains("aom"))
                presetArgs = $"-cpu-used 6 -crf {p.crf} {bitrate}";
            else
                presetArgs = $"-preset {p.software} -crf {p.crf} {bitrate}";

            string args = $"-c:v {encoder} {presetArgs} -pix_fmt {ReplayRenderSettings.PixelFormat} {ReplayRenderSettings.ExtraFFmpegArgs}";
            return (encoder, args);
        }

        // i love c#!
        private static readonly (string nvenc, int crf, string amf, string qsv, int qsvQuality, string software)[] Presets =
        {
            ("fast", 28, "speed", "veryfast", 30, "ultrafast"),
            ("medium", 23, "balanced", "medium", 25, "medium"),
            ("slow", 18, "quality", "slow", 20, "slow"),
        };
    }
}
