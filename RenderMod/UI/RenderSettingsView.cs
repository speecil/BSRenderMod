using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using IPA.Utilities;
using RenderMod.Render;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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

        [UIValue("resolution")] private string resolution
        {
            get
            {
                switch (ReplayRenderSettings.Width)
                {
                    case (640):
                        return "360p";
                    case (854):
                        return "480p";
                    case (1280):
                        return "720p";
                    case (1920):
                        return "1080p";
                    case (2560):
                        return "1440p";
                    case (3840):
                        return "4K";
                    default:
                        return $"1080p";

                }
            }
        }
        [UIValue("fps")] private int fps = ReplayRenderSettings.FPS;
        [UIValue("camera-option")] private string cameraSpecifier = ReplayRenderSettings.SpecifiedCameraName;
        [UIValue("bitrate")] private int bitrate = ReplayRenderSettings.BitrateKbps;
        [UIValue("audioBitrate")] private int audioBitrate = ReplayRenderSettings.AudioBitrateKbps;
        [UIValue("extraFFmpegArgs")] private string extraFFmpegArgs = ReplayRenderSettings.ExtraFFmpegArgs;

        // presets
        [UIValue("preset-options")] private List<string> presetOptions = new List<string>() { "Low", "Medium", "High" };
        [UIValue("preset-option")] private string currentPreset = ReplayRenderSettings.Preset.ToString();
        [UIValue("resolution-options")] private List<string> resolutionOptions = new List<string>()
        {
            "360p",
            "480p",
            "720p",
            "1080p",
            "1440p",
            "4K"
        };

        List<string> _cameraOptions = new List<string>();

        [UIValue("camera-options")]
        private List<string> cameraOptions
        {
            get
            {
                return _cameraOptions;
            }
            set
            {
                _cameraOptions = value;
                NotifyPropertyChanged();
            }
        }


        private string FourKWarning = "4K Renders require a lot of processing power and disk space. Ensure your system is capable of handling it.";
        [UIAction("OnResolutionChanged")]
        private void OnResolutionChanged(string value)
        {
            VideoWarnings.Remove(FourKWarning);
            switch (value)
            {
                case "360p":
                    ReplayRenderSettings.Width = 640;
                    ReplayRenderSettings.Height = 360;
                    break;
                case "480p":
                    ReplayRenderSettings.Width = 854;
                    ReplayRenderSettings.Height = 480;
                    break;
                case "720p":
                    ReplayRenderSettings.Width = 1280;
                    ReplayRenderSettings.Height = 720;
                    break;
                case "1080p":
                    ReplayRenderSettings.Width = 1920;
                    ReplayRenderSettings.Height = 1080;
                    break;
                case "1440p":
                    ReplayRenderSettings.Width = 2560;
                    ReplayRenderSettings.Height = 1440;
                    break;
                case "4K":
                    ReplayRenderSettings.Width = 3840;
                    ReplayRenderSettings.Height = 2160;
                    if (!VideoWarnings.Contains(FourKWarning))
                        VideoWarnings.Add(FourKWarning);
                    break;
                default:
                    _log.Warn($"Unknown resolution option: {value}, defaulting to 1080p");
                    ReplayRenderSettings.Width = 1920;
                    ReplayRenderSettings.Height = 1080;
                    break;
            }
            UpdateWarnings();
        }

        private string FPSWarning = "Higher FPS values require more processing power and will result in larger file sizes. Ensure your system is capable of handling it.";
        [UIAction("OnFPSChanged")]
        private void OnFPSChanged(int value)
        {
            VideoWarnings.Remove(FPSWarning);
            ReplayRenderSettings.FPS = value;
            if(value > 60)
            {
                VideoWarnings.Add(FPSWarning);
            }
            UpdateWarnings();
        }

        [UIAction("OnCameraSpecifierChanged")]
        private void OnCameraSpecifierChanged(string value) => ReplayRenderSettings.SpecifiedCameraName = value;

        private string BitrateWarning = "Bitrate values higher than 10000kbps require more processing power. Ensure your system is capable of handling it.";
        [UIAction("OnBitrateChanged")]
        private void OnBitrateChanged(int value)
        {
            VideoWarnings.Remove(BitrateWarning);
            if (value > 10000)
            {
                VideoWarnings.Add(BitrateWarning);
            }
            ReplayRenderSettings.BitrateKbps = value;
            UpdateWarnings();
        }

        [UIAction("OnAudioBitrateChanged")]
        private void OnAudioBitrateChanged(int value) => ReplayRenderSettings.AudioBitrateKbps = value;

        private string ExtraArgsWarning = "Using certain FFmpeg arguments can cause instability or crashes. Ensure you know what you're doing.";
        [UIAction("OnExtraArgsChanged")]
        private void OnExtraArgsChanged(string value)
        {
            OtherWarnings.Remove(ExtraArgsWarning);
            if(!string.IsNullOrEmpty(value))
            {
                OtherWarnings.Add(ExtraArgsWarning);
            }
            ReplayRenderSettings.ExtraFFmpegArgs = value;
            UpdateWarnings();
        }

        private string PresetWarning = "High Quality will use a lot of storage space and processing. Ensure your system can handle it.";
        [UIAction("OnPresetChanged")]
        private void OnPresetChanged(string value)
        {
            QualityWarnings.Remove(PresetWarning);
            if (Enum.TryParse(value, out QualityPreset preset))
            {
                if(preset == QualityPreset.High)
                {
                    QualityWarnings.Add(PresetWarning);
                }
                ReplayRenderSettings.Preset = preset;
                currentPreset = value;
            }
            UpdateWarnings();
        }

        [UIComponent("encoder-test-text")]
        private TMPro.TextMeshProUGUI encoderTestText;

        // encoder tester
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


        private List<string> QualityWarnings = new List<string>();
        private List<string> VideoWarnings = new List<string>();
        private List<string> CameraWarnings = new List<string>();
        private List<string> OtherWarnings = new List<string>();
        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            cameraOptions.Clear();
            List<string> cameras = new List<string>();
            foreach (var item in Directory.GetFiles(Path.Combine(UnityGame.UserDataPath, "Camera2", "Cameras")))
            {
                _log.Notice($"Found camera: {Path.GetFileNameWithoutExtension(item)}");
                cameras.Add(Path.GetFileNameWithoutExtension(item));
            }
            cameraOptions = cameras;
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            if (firstActivation)
            {
                tabSelector.TextSegmentedControl.didSelectCellEvent += (segmentedControl, cell) =>
                {
                    switch (cell)
                    {
                        case 0:
                            qualityWarningText.text = string.Join("\n", QualityWarnings);
                            break;
                        case 1:
                            videoWarningText.text = string.Join("\n", VideoWarnings);
                            break;
                        case 2:
                            cameraWarningText.text = string.Join("\n", CameraWarnings);
                            break;
                        case 3:
                            otherWarningText.text = string.Join("\n", OtherWarnings);
                            break;
                    }
                };
                tabSelector.TextSegmentedControl.SelectCellWithNumber(0);
                UpdateWarnings();
            }
        }

        public void UpdateWarnings()
        {
            qualityWarningText.text = string.Join("\n", QualityWarnings);
            videoWarningText.text = string.Join("\n", VideoWarnings);
            cameraWarningText.text = string.Join("\n", CameraWarnings);
            otherWarningText.text = string.Join("\n", OtherWarnings);
        }
    }
}
