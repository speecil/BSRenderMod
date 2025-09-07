using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using RenderMod.Render;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using Zenject;


namespace RenderMod.UI
{
    [HotReload(RelativePathToLayout = @"RenderSettingsView.bsml")]
    [ViewDefinition("RenderMod.UI.RenderSettingsView.bsml")]
    internal class RenderSettingsView : BSMLAutomaticViewController
    {
        [Inject] private SiraLog _log;

        [UIValue("resolution")] private string resolution = $"{ReplayRenderSettings.Width}x{ReplayRenderSettings.Height}";
        [UIValue("fps")] private int fps = ReplayRenderSettings.FPS;
        [UIValue("cameraSpecifier")] private string cameraSpecifier = ReplayRenderSettings.SpecifiedCameraName;
        [UIValue("bitrate")] private int bitrate = ReplayRenderSettings.BitrateKbps;
        [UIValue("audioBitrate")] private int audioBitrate = ReplayRenderSettings.AudioBitrateKbps;
        [UIValue("extraFFmpegArgs")] private string extraFFmpegArgs = ReplayRenderSettings.ExtraFFmpegArgs;

        // presets
        [UIValue("preset-options")] private List<string> presetOptions = new List<string>() { "Low", "Medium", "High" };
        [UIValue("preset-option")] private string currentPreset = ReplayRenderSettings.Preset.ToString();
        [UIValue("resolution-options")] private List<string> resolutionOptions = new List<string>()
        {
            "640x360",   // 360p
            "854x480",   // 480p
            "1280x720",  // 720p
            "1920x1080", // 1080p
            "2560x1440", // 1440p
            "3840x2160"  // 4K
        };

        [UIAction("OnResolutionChanged")]
        private void OnResolutionChanged(string value)
        {
            string[] split = value.Split('x');
            if (split.Length != 2) return;
            if (int.TryParse(split[0], out int width) && int.TryParse(split[1], out int height))
            {
                ReplayRenderSettings.Width = width;
                ReplayRenderSettings.Height = height;
                resolution = value;
            }
        }

        [UIAction("OnFPSChanged")]
        private void OnFPSChanged(int value) => ReplayRenderSettings.FPS = value;

        [UIAction("OnCameraSpecifierChanged")]
        private void OnCameraSpecifierChanged(string value) => ReplayRenderSettings.SpecifiedCameraName = value;

        [UIAction("OnBitrateChanged")]
        private void OnBitrateChanged(int value) => ReplayRenderSettings.BitrateKbps = value;

        [UIAction("OnAudioBitrateChanged")]
        private void OnAudioBitrateChanged(int value) => ReplayRenderSettings.AudioBitrateKbps = value;

        [UIAction("OnExtraArgsChanged")]
        private void OnExtraArgsChanged(string value) => ReplayRenderSettings.ExtraFFmpegArgs = value;

        [UIAction("OnPresetChanged")]
        private void OnPresetChanged(string value)
        {
            if (Enum.TryParse(value, out QualityPreset preset))
            {
                ReplayRenderSettings.Preset = preset;
                currentPreset = value;
            }
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
    }
}
