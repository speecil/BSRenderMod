using IPA.Utilities;
using System.IO;

namespace RenderMod.Render
{
    public enum QualityPreset
    {
        Low,
        Medium,
        High
    }

    public static class ReplayRenderSettings
    {
        public static bool RenderEnabled = false;

        public static string CameraType = "None";

        // general
        public static int Width = 1920;
        public static int Height = 1080; // TODO: add preset support for 4K, 1440p, instead of increment settings
        public static int FPS = 60; // default FPS for rendering, can be changed to 120 or whatever

        public static string SpecifiedCameraName = "Main"; // Camera2 default camera name is "Main"

        // quality
        public static QualityPreset Preset = QualityPreset.Medium; // default preset
        public static int BitrateKbps = 8000; // always respected, regardless of preset

        // encoding
        public static string VideoCodec = "auto"; // unused (i doubt people have different encoders available)
        public static string PixelFormat = "yuv420p";
        public static bool IncludeAudio = true; // unused, always true for now
        public static string AudioCodec = "aac"; // unused, always aac for now
        public static int AudioBitrateKbps = 320;

        // paths
        public static string RenderRoot = "Renders";

        // advanced
        public static string ExtraFFmpegArgs = ""; // additional arguments for ffmpeg

        static ReplayRenderSettings()
        {
            LoadSettings();
        }

        public static void LoadSettings()
        {
            if (!File.Exists(UnityGame.UserDataPath + "/RenderModSettings.json"))
            {
                SaveSettings();
                return;
            }
            string jsonText = File.ReadAllText(UnityGame.UserDataPath + "/RenderModSettings.json");
            Newtonsoft.Json.Linq.JObject settings = Newtonsoft.Json.Linq.JObject.Parse(jsonText);
            RenderEnabled = settings.Value<bool?>("RenderEnabled") ?? RenderEnabled;
            CameraType = settings.Value<string>("CameraType") ?? CameraType;
            Width = settings.Value<int?>("Width") ?? Width;
            Height = settings.Value<int?>("Height") ?? Height;
            FPS = settings.Value<int?>("FPS") ?? FPS;
            SpecifiedCameraName = settings.Value<string>("SpecifiedCameraName") ?? SpecifiedCameraName;
            string presetStr = settings.Value<string>("Preset") ?? Preset.ToString();
            if (System.Enum.TryParse<QualityPreset>(presetStr, out QualityPreset parsedPreset))
            {
                Preset = parsedPreset;
            }
            BitrateKbps = settings.Value<int?>("BitrateKbps") ?? BitrateKbps;
            VideoCodec = settings.Value<string>("VideoCodec") ?? VideoCodec;
            PixelFormat = settings.Value<string>("PixelFormat") ?? PixelFormat;
            IncludeAudio = settings.Value<bool?>("IncludeAudio") ?? IncludeAudio;
            AudioCodec = settings.Value<string>("AudioCodec") ?? AudioCodec;
            AudioBitrateKbps = settings.Value<int?>("AudioBitrateKbps") ?? AudioBitrateKbps;
            RenderRoot = settings.Value<string>("RenderRoot") ?? RenderRoot;
            ExtraFFmpegArgs = settings.Value<string>("ExtraFFmpegArgs") ?? ExtraFFmpegArgs;
        }

        public static void SaveSettings()
        {
            var settings = new Newtonsoft.Json.Linq.JObject
            {
                ["RenderEnabled"] = RenderEnabled,
                ["CameraType"] = CameraType,
                ["Width"] = Width,
                ["Height"] = Height,
                ["FPS"] = FPS,
                ["SpecifiedCameraName"] = SpecifiedCameraName,
                ["Preset"] = Preset.ToString(),
                ["BitrateKbps"] = BitrateKbps,
                ["VideoCodec"] = VideoCodec,
                ["PixelFormat"] = PixelFormat,
                ["IncludeAudio"] = IncludeAudio,
                ["AudioCodec"] = AudioCodec,
                ["AudioBitrateKbps"] = AudioBitrateKbps,
                ["RenderRoot"] = RenderRoot,
                ["ExtraFFmpegArgs"] = ExtraFFmpegArgs
            };
            string jsonText = settings.ToString();
            File.WriteAllText(UnityGame.UserDataPath + "/RenderModSettings.json", jsonText);
        }
    }
}
