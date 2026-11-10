using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using RenderMod.Render;
using RenderMod.Util;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Zenject;

namespace RenderMod.UI
{
    [HotReload(RelativePathToLayout = @"RenderSettingsView.bsml")]
    [ViewDefinition("RenderMod.UI.RenderSettingsView.bsml")]
    internal class RenderSettingsView : BSMLAutomaticViewController
    {
        [Inject] private readonly SiraLog _log = null;


        private static readonly string[] ResolutionLabels = { "360p", "480p", "720p", "1080p", "1440p", "4K", "8K" };
        private static readonly int[] Heights = { 360, 480, 720, 1080, 1440, 2160, 4320 };

        private static readonly Dictionary<string, int[]> Widths = new Dictionary<string, int[]>
        {
            ["16:9"] = new[] { 640, 854, 1280, 1920, 2560, 3840, 7680 },
            ["20:9"] = new[] { 800, 1066, 1600, 2400, 3200, 4800, 9600 },
            ["21:9"] = new[] { 840, 1120, 1680, 2560, 3440, 5120, 10080 },
            ["32:9"] = new[] { 1280, 1706, 2560, 3840, 5120, 7680, 15360 },
            ["3:2"] = new[] { 540, 720, 1080, 1620, 2160, 3240, 6480 },
            ["4:3"] = new[] { 480, 640, 960, 1440, 1920, 2880, 5760 },
        };

        private static void ApplyResolution(string aspect, bool portrait, string label)
        {
            int i = Math.Max(0, Array.IndexOf(ResolutionLabels, label));
            int w = Widths[aspect][i], h = Heights[i];
            ReplayRenderSettings.AspectRatio = aspect;
            ReplayRenderSettings.PortraitToggle = portrait;
            ReplayRenderSettings.Width = portrait ? h : w;
            ReplayRenderSettings.Height = portrait ? w : h;
            ReplayRenderSettings.SaveSettings();
        }

        [UIValue("resolution-options")] private List<object> resolutionOptions = ResolutionLabels.Cast<object>().ToList();
        [UIValue("aspectRatio-options")] private List<object> aspectRatioOptions = Widths.Keys.Cast<object>().ToList();
        [UIValue("portraitToggle")] private bool portraitToggle = ReplayRenderSettings.PortraitToggle;

        [UIValue("aspectRatio")]
        private string aspectRatio
        {
            get => Widths.ContainsKey(ReplayRenderSettings.AspectRatio) ? ReplayRenderSettings.AspectRatio : "16:9";
            set => ApplyResolution(value, ReplayRenderSettings.PortraitToggle, resolution);
        }

        [UIValue("resolution")]
        private string resolution
        {
            get
            {
                bool portrait = ReplayRenderSettings.PortraitToggle;
                int landscapeW = portrait ? ReplayRenderSettings.Height : ReplayRenderSettings.Width;
                int landscapeH = portrait ? ReplayRenderSettings.Width : ReplayRenderSettings.Height;
                int[] widths = Widths[aspectRatio];
                for (int i = 0; i < ResolutionLabels.Length; i++)
                {
                    if (widths[i] == landscapeW && Heights[i] == landscapeH)
                        return ResolutionLabels[i];
                }

                _log.Warn($"Unknown resolution setting: {ReplayRenderSettings.Width}x{ReplayRenderSettings.Height}, defaulting to 1080p");
                return "1080p";
            }
            set => ApplyResolution(aspectRatio, ReplayRenderSettings.PortraitToggle, value);
        }

        [UIAction("OnResolutionChanged")]
        private void OnResolutionChanged(string value)
        {
            resolution = value;
            UpdateWarnings();
        }

        [UIAction("OnAspectRatioChanged")]
        private void OnAspectRatioChanged(string value)
        {
            aspectRatio = value;
            UpdateWarnings();
        }

        [UIAction("OnPortraitToggleChanged")]
        private void OnPortraitToggleChanged(bool value)
        {
            ApplyResolution(aspectRatio, value, resolution);
            UpdateWarnings();
        }

        [UIValue("fps")] private int fps = ReplayRenderSettings.FPS;
        [UIValue("bitrate")] private int bitrate = ReplayRenderSettings.BitrateKbps;
        [UIValue("audioBitrate")] private int audioBitrate = ReplayRenderSettings.AudioBitrateKbps;
        [UIValue("extraFFmpegArgs")] private string extraFFmpegArgs = ReplayRenderSettings.ExtraFFmpegArgs;
        [UIValue("video-codec-options")] private List<object> videoCodecs = new List<object> { "h264", "hevc", "av1" };
        [UIValue("video-codec")] private string videoCodec = ReplayRenderSettings.VideoCodec;
        [UIValue("preset-options")] private List<object> presetOptions = Enum.GetNames(typeof(QualityPreset)).Cast<object>().ToList();
        [UIValue("preset-option")] private string currentPreset = ReplayRenderSettings.Preset.ToString();

        [UIComponent("encoder-test-text")] private TextMeshProUGUI encoderTestText = null;

        [UIAction("OnFPSChanged")]
        private void OnFPSChanged(int value)
        {
            ReplayRenderSettings.FPS = value;
            UpdateWarnings();
        }

        [UIAction("OnBitrateChanged")]
        private void OnBitrateChanged(int value)
        {
            ReplayRenderSettings.BitrateKbps = value;
            UpdateWarnings();
        }

        [UIAction("OnAudioBitrateChanged")]
        private void OnAudioBitrateChanged(int value)
        {
            ReplayRenderSettings.AudioBitrateKbps = value;
        }

        [UIAction("OnExtraArgsChanged")]
        private void OnExtraArgsChanged(string value)
        {
            ReplayRenderSettings.ExtraFFmpegArgs = value;
            UpdateWarnings();
        }

        [UIAction("OnPresetChanged")]
        private void OnPresetChanged(string value)
        {
            if (Enum.TryParse(value, out QualityPreset preset))
                ReplayRenderSettings.Preset = preset;
            UpdateWarnings();
        }

        [UIAction("OnVideoCodecChanged")]
        private void OnVideoCodecChanged(string value)
        {
            ReplayRenderSettings.VideoCodec = value;
            UpdateWarnings();
        }

        [UIAction("OnEncoderTestClicked")]
        private void OnEncoderTestClicked()
        {
            encoderTestText.text = $"RESULT: {EncoderHelpers.SelectBestEncoder()} encoder";
        }

        [UIValue("camera-options")] private List<object> cameraOptions = new List<object>();
        [UIValue("camera-option")] private string cameraSpecifier = ReplayRenderSettings.SpecifiedCameraName;
        [UIValue("cameraType-options")] private List<object> cameraTypeOptions = new List<object> { "Camera2", "ReeCamera", "None" };
        [UIValue("cameraType-option")] private string cameraTypeSpecifier = ReplayRenderSettings.CameraType;

        [UIComponent("camera-specifier")] private DropDownListSetting cameraSpecifierDropDown = null;

        [UIAction("OnCameraSpecifierChanged")]
        private void OnCameraSpecifierChanged(string value)
        {
            ReplayRenderSettings.SpecifiedCameraName = value;
            UpdateWarnings();
        }

        [UIAction("OnCameraTypeSpecifierChanged")]
        private void OnCameraTypeSpecifierChanged(string value)
        {
            ReplayRenderSettings.CameraType = value;
            if (cameraSpecifierDropDown != null)
                cameraSpecifierDropDown.Interactable = value == "Camera2";
            UpdateWarnings();
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            cameraSpecifierDropDown.Values = CameraUtils.Core.CamerasManager.GetRegisteredCameras()
                .Where(c => c.CameraFlags != CameraUtils.Core.CameraFlags.Mirror)
                .Select(c => c.Camera.transform.GetObjectPath(2))
                .Where(path => !path.Contains("/ReeLayout") && !path.Contains("/Origin/"))
                .Cast<object>()
                .ToList();
            cameraSpecifierDropDown.Value = ReplayRenderSettings.SpecifiedCameraName;
            cameraSpecifierDropDown.UpdateChoices();

            OnCameraTypeSpecifierChanged(ReplayRenderSettings.CameraType);

            if (firstActivation)
                tabSelector.TextSegmentedControl.SelectCellWithNumber(0);
        }

        [UIComponent("tabSelector")] private TabSelector tabSelector = null;
        [UIComponent("qualitywarningText")] private TextMeshProUGUI qualityWarningText = null;
        [UIComponent("resolutionwarningText")] private TextMeshProUGUI resolutionWarningText = null;
        [UIComponent("bitratewarningText")] private TextMeshProUGUI bitrateWarningText = null;
        [UIComponent("camerawarningText")] private TextMeshProUGUI cameraWarningText = null;
        [UIComponent("otherwarningText")] private TextMeshProUGUI otherWarningText = null;

        [UIAction("#post-parse")]
        private void UpdateWarnings()
        {
            if (qualityWarningText == null)
                return;

            string res = resolution;
            bool reeCamera = ReplayRenderSettings.CameraType == "ReeCamera";
            bool not169 = aspectRatio != "16:9" || ReplayRenderSettings.PortraitToggle;

            SetWarnings(qualityWarningText, "quality",
                (ReplayRenderSettings.Preset == QualityPreset.High, "High Quality preset uses substantial storage and processing."),
                (ReplayRenderSettings.VideoCodec == "av1", "AV1 is chosen as the current codec! You need either an RTX 40 series GPU or an AMD RX 7000 series GPU."));

            SetWarnings(resolutionWarningText, "resolution",
                (res == "4K" || res == "8K", "4K/8K renders require significant processing power and disk space."),
                (res == "8K", "8K renders require the use of a HEVC encoder to render properly."),
                (not169 && reeCamera, "You are currently running a non-16:9 aspect ratio with ReeCamera! Make sure to enable Spout\nand set the correct resolution in your ReeCamera preset!\n You can view your current resolution in UserData > RenderModSettings.json"));

            SetWarnings(bitrateWarningText, "bitrate",
                (ReplayRenderSettings.FPS > 60, "High FPS values increase file size and CPU usage."),
                (ReplayRenderSettings.BitrateKbps > 10000, "Bitrates over 10,000 kbps may cause instability or lag."));

            SetWarnings(cameraWarningText, "camera",
                (ReplayRenderSettings.CameraType == "Camera2" && !ReplayRenderSettings.SpecifiedCameraName.ToLower().Contains("main"), "Camera is not called \"Main\". Ensure this is the correct camera."),
                (ReplayRenderSettings.CameraType == "None", "No camera mod installed, main camera will be used."));

            SetWarnings(otherWarningText, "other",
                (!string.IsNullOrEmpty(ReplayRenderSettings.ExtraFFmpegArgs), "Extra FFmpeg arguments can cause instability or crashes."));
        }

        private static void SetWarnings(TextMeshProUGUI text, string category, params (bool active, string message)[] warnings)
        {
            // wait for text init
            if (text == null)
                return;
            var active = warnings.Where(w => w.active).Select(w => $"<color=#ffcc00>• {w.message}</color>").ToList();
            text.text = active.Count == 0 ? $"<color=#888888>No {category} warnings.</color>" : string.Join("\n", active);
        }
    }
}
