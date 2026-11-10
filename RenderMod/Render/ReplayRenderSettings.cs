using IPA.Utilities;
using Newtonsoft.Json.Linq;
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
        private static readonly string SettingsPath = Path.Combine(UnityGame.UserDataPath, "RenderModSettings.json");

        public static bool RenderEnabled = false;
        public static string CameraType = "None";
        public static string SpecifiedCameraName = "Main"; // Camera2's default camera name

        public static int Width = 1920;
        public static int Height = 1080;
        public static string AspectRatio = "16:9";
        public static bool PortraitToggle = false;
        public static int FPS = 60;

        public static QualityPreset Preset = QualityPreset.Medium;
        public static int BitrateKbps = 10000;
        public static string VideoCodec = "h264";
        public static string PixelFormat = "yuv420p";
        public static int AudioBitrateKbps = 320;

        public static string RenderRoot = "Renders";
        public static string ExtraFFmpegArgs = "";

        static ReplayRenderSettings()
        {
            LoadSettings();
        }

        public static void LoadSettings()
        {
            if (!File.Exists(SettingsPath))
            {
                SaveSettings();
                return;
            }

            var s = JObject.Parse(File.ReadAllText(SettingsPath));
            RenderEnabled = s.Value<bool?>("RenderEnabled") ?? RenderEnabled;
            CameraType = s.Value<string>("CameraType") ?? CameraType;
            SpecifiedCameraName = s.Value<string>("SpecifiedCameraName") ?? SpecifiedCameraName;
            Width = s.Value<int?>("Width") ?? Width;
            Height = s.Value<int?>("Height") ?? Height;
            AspectRatio = s.Value<string>("AspectRatio") ?? AspectRatio;
            PortraitToggle = s.Value<bool?>("PortraitToggle") ?? PortraitToggle;
            FPS = s.Value<int?>("FPS") ?? FPS;
            if (System.Enum.TryParse(s.Value<string>("Preset"), out QualityPreset preset))
                Preset = preset;
            BitrateKbps = s.Value<int?>("BitrateKbps") ?? BitrateKbps;
            VideoCodec = s.Value<string>("VideoCodec") ?? VideoCodec;
            PixelFormat = s.Value<string>("PixelFormat") ?? PixelFormat;
            AudioBitrateKbps = s.Value<int?>("AudioBitrateKbps") ?? AudioBitrateKbps;
            RenderRoot = s.Value<string>("RenderRoot") ?? RenderRoot;
            ExtraFFmpegArgs = s.Value<string>("ExtraFFmpegArgs") ?? ExtraFFmpegArgs;
        }

        public static void SaveSettings()
        {
            var s = new JObject
            {
                ["RenderEnabled"] = RenderEnabled,
                ["CameraType"] = CameraType,
                ["SpecifiedCameraName"] = SpecifiedCameraName,
                ["Width"] = Width,
                ["Height"] = Height,
                ["AspectRatio"] = AspectRatio,
                ["PortraitToggle"] = PortraitToggle,
                ["FPS"] = FPS,
                ["Preset"] = Preset.ToString(),
                ["BitrateKbps"] = BitrateKbps,
                ["VideoCodec"] = VideoCodec,
                ["PixelFormat"] = PixelFormat,
                ["AudioBitrateKbps"] = AudioBitrateKbps,
                ["RenderRoot"] = RenderRoot,
                ["ExtraFFmpegArgs"] = ExtraFFmpegArgs
            };
            File.WriteAllText(SettingsPath, s.ToString());
        }
    }
}
