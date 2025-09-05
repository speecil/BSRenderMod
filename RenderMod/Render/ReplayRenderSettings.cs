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
        public static int AudioBitrateKbps = 320; // unused, always 320 for now

        // paths
        public static string RenderRoot = "Renders";

        // camera
        //public static float RenderFOV = 70f;
        //public static Vector3 RenderOffset = new Vector3(0, 0f, -0.15f);

        // advanced
        public static string ExtraFFmpegArgs = ""; // additional arguments for ffmpeg
    }
}
