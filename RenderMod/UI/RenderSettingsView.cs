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
using Zenject;

namespace RenderMod.UI
{
    [HotReload(RelativePathToLayout = @"RenderSettingsView.bsml")]
    [ViewDefinition("RenderMod.UI.RenderSettingsView.bsml")]
    internal class RenderSettingsView : BSMLAutomaticViewController
    {
        [Inject] private SiraLog _log;

        [UIComponent("tabSelector")] private TabSelector tabSelector;
        [UIComponent("qualitywarningText")] private TMPro.TextMeshProUGUI qualityWarningText;
        [UIComponent("videowarningText")] private TMPro.TextMeshProUGUI videoWarningText;
        [UIComponent("camerawarningText")] private TMPro.TextMeshProUGUI cameraWarningText;
        [UIComponent("otherwarningText")] private TMPro.TextMeshProUGUI otherWarningText;

        [UIValue("resolution")]
        private string resolution
        {
            get
            {
                switch (ReplayRenderSettings.Width)
                {
                    case 640 when ReplayRenderSettings.Height == 360: return "360p";
                    case 854 when ReplayRenderSettings.Height == 480: return "480p";
                    case 1280 when ReplayRenderSettings.Height == 720: return "720p";
                    case 1920 when ReplayRenderSettings.Height == 1080: return "1080p";
                    case 2560 when ReplayRenderSettings.Height == 1440: return "1440p";
                    case 3840 when ReplayRenderSettings.Height == 2160: return "4K";
                    default:
                        _log.Warn($"Unknown resolution setting: {ReplayRenderSettings.Width}x{ReplayRenderSettings.Height}, defaulting to 1080p");
                        return "1080p";
                };
            }
            set
            {
                switch (value)
                {
                    case "360p": ReplayRenderSettings.Width = 640; ReplayRenderSettings.Height = 360; break;
                    case "480p": ReplayRenderSettings.Width = 854; ReplayRenderSettings.Height = 480; break;
                    case "720p": ReplayRenderSettings.Width = 1280; ReplayRenderSettings.Height = 720; break;
                    case "1080p": ReplayRenderSettings.Width = 1920; ReplayRenderSettings.Height = 1080; break;
                    case "1440p": ReplayRenderSettings.Width = 2560; ReplayRenderSettings.Height = 1440; break;
                    case "4K": ReplayRenderSettings.Width = 3840; ReplayRenderSettings.Height = 2160; break;
                    default:
                        _log.Warn($"Unknown resolution option: {value}, defaulting to 1080p");
                        ReplayRenderSettings.Width = 1920;
                        ReplayRenderSettings.Height = 1080;
                        break;
                }
                ReplayRenderSettings.SaveSettings();
                UpdateWarnings();
            }
        }

        [UIComponent("specifier-vert")]
        private UnityEngine.UI.VerticalLayoutGroup specifierVert;

        [UIValue("fps")] private int fps = ReplayRenderSettings.FPS;
        [UIValue("camera-option")] private string cameraSpecifier = ReplayRenderSettings.SpecifiedCameraName;
        [UIValue("cameraType-option")] private string cameraTypeSpecifier = ReplayRenderSettings.CameraType;
        [UIValue("bitrate")] private int bitrate = ReplayRenderSettings.BitrateKbps;
        [UIValue("audioBitrate")] private int audioBitrate = ReplayRenderSettings.AudioBitrateKbps;
        [UIValue("extraFFmpegArgs")] private string extraFFmpegArgs = ReplayRenderSettings.ExtraFFmpegArgs;

        [UIValue("preset-options")] private List<string> presetOptions = new List<string>() { "Low", "Medium", "High" };
        [UIValue("preset-option")] private string currentPreset = ReplayRenderSettings.Preset.ToString();
        [UIValue("resolution-options")]
        private List<string> resolutionOptions = new List<string>()
        {
            "360p", "480p", "720p", "1080p", "1440p", "4K"
        };

        private List<object> _cameraOptions = new List<object>();

        [UIValue("camera-options")]
        private List<object> cameraOptions
        {
            get => _cameraOptions.ToList();
            set { _cameraOptions = value; NotifyPropertyChanged(); }
        }

        private List<object> _cameraTypeOptions = new List<object>()
        {
            "Camera2", "ReeCamera", "None"
        };

        [UIValue("cameraType-options")]
        private List<object> cameraTypeOptions
        {
            get => _cameraTypeOptions.ToList();
            set { _cameraTypeOptions = value; NotifyPropertyChanged(); }
        }

        [UIComponent("camera-specifier")] private DropDownListSetting cameraSpecifierDropDown;

        [UIComponent("cameraType-specifier")] private DropDownListSetting cameraTypeDropDown;

        private readonly string FourKWarning = "4K renders require significant processing power and disk space.";
        private readonly string FPSWarning = "High FPS values increase file size and CPU usage.";
        private readonly string BitrateWarning = "Bitrates over 10,000 kbps may cause instability or lag.";
        private readonly string ExtraArgsWarning = "Extra FFmpeg arguments can cause instability or crashes.";
        private readonly string PresetWarning = "High Quality preset uses substantial storage and processing.";
        private readonly string NonMainCameraWarning = "Camera is not called \"Main\". Ensure this is the correct camera.";
        private readonly string NoneCameraTypeWarning = "No camera mod installed, main camera will be used.";

        [UIAction("OnResolutionChanged")]
        private void OnResolutionChanged(string value)
        {
            resolution = value;
        }

        [UIAction("OnFPSChanged")]
        private void OnFPSChanged(int value)
        {
            ReplayRenderSettings.FPS = value;
            fps = value;
            UpdateWarnings();
        }

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
            cameraSpecifierDropDown.Interactable = true;
            switch (value)
            {
                case "Camera2":
                    break;
                case "None":
                case "ReeCamera":
                    cameraSpecifierDropDown.Interactable = false;
                    break;
            }
            UpdateWarnings();
        }

        [UIAction("OnBitrateChanged")]
        private void OnBitrateChanged(int value)
        {
            ReplayRenderSettings.BitrateKbps = value;
            bitrate = value;
            UpdateWarnings();
        }

        [UIAction("OnAudioBitrateChanged")]
        private void OnAudioBitrateChanged(int value)
        {
            ReplayRenderSettings.AudioBitrateKbps = value;
            UpdateWarnings();
        }

        [UIAction("OnExtraArgsChanged")]
        private void OnExtraArgsChanged(string value)
        {
            ReplayRenderSettings.ExtraFFmpegArgs = value;
            extraFFmpegArgs = value;
            UpdateWarnings();
        }

        [UIAction("OnPresetChanged")]
        private void OnPresetChanged(string value)
        {
            if (Enum.TryParse(value, out QualityPreset preset))
            {
                ReplayRenderSettings.Preset = preset;
                currentPreset = value;
            }
            UpdateWarnings();
        }

        [UIComponent("encoder-test-text")] private TMPro.TextMeshProUGUI encoderTestText;

        [UIAction("OnEncoderTestClicked")]
        private void OnTestEncoder()
        {
            try
            {
                var (encoder, args) = EncoderHelpers.BuildEncoderArgs();
                encoderTestText.text = $"RESULT: {encoder} encoder";
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to build encoder args: {ex}");
            }
        }

        private readonly List<string> QualityWarnings = new List<string>();
        private readonly List<string> VideoWarnings = new List<string>();
        private readonly List<string> CameraWarnings = new List<string>();
        private readonly List<string> OtherWarnings = new List<string>();

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            cameraOptions.Clear();
            var cameras = CameraUtils.Core.CamerasManager.GetRegisteredCameras().Where(x => x.CameraFlags != CameraUtils.Core.CameraFlags.Mirror 
                                                                                            && (!x.Camera.transform.GetObjectPath(2).Contains("/ReeLayout") && !x.Camera.transform.GetObjectPath(2).Contains("/Origin/")))
                .Select(x => (object)x.Camera.transform.GetObjectPath(2))
                .ToList();

            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            OnCameraTypeSpecifierChanged(ReplayRenderSettings.CameraType);

            cameraOptions = cameras;
            cameraSpecifierDropDown.Values = cameraOptions;
            cameraSpecifierDropDown.Value = ReplayRenderSettings.SpecifiedCameraName;
            cameraSpecifierDropDown.UpdateChoices();

            if (firstActivation)
            {
                tabSelector.TextSegmentedControl.didSelectCellEvent += (_, cell) =>
                {
                    RefreshTab(cell);
                };

                tabSelector.TextSegmentedControl.SelectCellWithNumber(0);
                UpdateWarnings();
            }
        }

        [UIAction("#post-parse")]
        public void PostParse()
        {
            UpdateWarnings();
        }

        private void RefreshTab(int tabIndex)
        {
            switch (tabIndex)
            {
                case 0: qualityWarningText.text = BuildWarningText(QualityWarnings, "quality"); break;
                case 1: videoWarningText.text = BuildWarningText(VideoWarnings, "video"); break;
                case 2: cameraWarningText.text = BuildWarningText(CameraWarnings, "camera"); break;
                case 3: otherWarningText.text = BuildWarningText(OtherWarnings, "other"); break;
            }
        }

        public void UpdateWarnings()
        {
            if (qualityWarningText == null || videoWarningText == null || cameraWarningText == null || otherWarningText == null)
                return;

            QualityWarnings.Clear();
            VideoWarnings.Clear();
            CameraWarnings.Clear();
            OtherWarnings.Clear();

            string currentResolution = resolution;
            int currentFps = ReplayRenderSettings.FPS;
            int currentBitrate = ReplayRenderSettings.BitrateKbps;
            string currentExtraArgs = ReplayRenderSettings.ExtraFFmpegArgs;
            string currentPresetString = ReplayRenderSettings.Preset.ToString();
            string cameraName = ReplayRenderSettings.SpecifiedCameraName;
            string cameraType = ReplayRenderSettings.CameraType;

            if (currentResolution == "4K")
                VideoWarnings.Add(FourKWarning);
            if (currentPresetString == QualityPreset.High.ToString())
                QualityWarnings.Add(PresetWarning);

            if (currentFps > 60)
                VideoWarnings.Add(FPSWarning);
            if (currentBitrate > 10000)
                VideoWarnings.Add(BitrateWarning);

            if (!string.IsNullOrEmpty(currentExtraArgs))
                OtherWarnings.Add(ExtraArgsWarning);

            if (!cameraName.ToLower().Contains("main"))
                CameraWarnings.Add(NonMainCameraWarning);

            if(cameraType == "None")
                CameraWarnings.Add(NoneCameraTypeWarning);

            qualityWarningText.text = BuildWarningText(QualityWarnings, "quality");
            videoWarningText.text = BuildWarningText(VideoWarnings, "video");
            cameraWarningText.text = BuildWarningText(CameraWarnings, "camera");
            otherWarningText.text = BuildWarningText(OtherWarnings, "other");

            RefreshTab(tabSelector.TextSegmentedControl.selectedCellNumber);
        }

        private string BuildWarningText(List<string> warnings, string category)
        {
            if (warnings.Count == 0)
                return $"<color=#888888>No {category} warnings.</color>";

            return string.Join("\n", warnings.Select(w => $"<color=#ffcc00>• {w}</color>"));
        }
    }
}
