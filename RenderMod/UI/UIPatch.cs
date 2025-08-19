using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using RenderMod.Render;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace RenderMod
{
    internal class UIPatch : PersistentSingleton<UIPatch>
    {
        [UIValue("renderEnabled")] public bool renderEnabled = ReplayRenderSettings.RenderEnabled;
        [UIValue("width")] public int width = ReplayRenderSettings.Width;
        [UIValue("height")] public int height = ReplayRenderSettings.Height;
        [UIValue("fps")] public int fps = ReplayRenderSettings.FPS;
        [UIValue("bitrate")] public int bitrate = ReplayRenderSettings.BitrateKbps;
        [UIValue("extraFFmpegArgs")] public string extraFFmpegArgs = ReplayRenderSettings.ExtraFFmpegArgs;

        // presets
        [UIValue("preset-options")] public List<object> presetOptions = new List<object>() { "Low", "Medium", "High" };
        [UIValue("preset-option")] public string currentPreset = ReplayRenderSettings.Preset.ToString();

        // actions

        [UIAction("OnToggleChanged")]
        public void OnToggleChanged(bool value) => ReplayRenderSettings.RenderEnabled = value;

        [UIAction("OnWidthChanged")]
        public void OnWidthChanged(int value) => ReplayRenderSettings.Width = value;

        [UIAction("OnHeightChanged")]
        public void OnHeightChanged(int value) => ReplayRenderSettings.Height = value;

        [UIAction("OnFPSChanged")]
        public void OnFPSChanged(int value) => ReplayRenderSettings.FPS = value;

        [UIAction("OnBitrateChanged")]
        public void OnBitrateChanged(int value) => ReplayRenderSettings.BitrateKbps = value;

        [UIAction("OnExtraArgsChanged")]
        public void OnExtraArgsChanged(string value) => ReplayRenderSettings.ExtraFFmpegArgs = value;

        [UIAction("OnPresetChanged")]
        public void OnPresetChanged(string value)
        {
            if (Enum.TryParse(value, out QualityPreset preset))
            {
                ReplayRenderSettings.Preset = preset;
                currentPreset = value;
            }
        }

        [UIComponent("encoder-test-text")]
        public TMPro.TextMeshProUGUI encoderTestText;

        // encoder tester
        [UIAction("OnEncoderTestClicked")]
        public void OnTestEncoder()
        {
            try
            {
                var (encoder, args) = EncoderHelpers.BuildEncoderArgs();
                encoderTestText.text = $"RESULT: {encoder} encoder";
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to build encoder args: {ex}");
            }
        }
    }
}
