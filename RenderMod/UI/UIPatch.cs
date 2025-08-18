using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using RenderMod.Render;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace RenderMod
{
    internal class UIPatch : IInitializable, IDisposable
    {
        [Inject] private GameplaySetup gameplaySetup;

        [UIValue("renderEnabled")] private bool renderEnabled = ReplayRenderSettings.RenderEnabled;
        [UIValue("width")] private int width = ReplayRenderSettings.Width;
        [UIValue("height")] private int height = ReplayRenderSettings.Height;
        [UIValue("fps")] private int fps = ReplayRenderSettings.FPS;
        [UIValue("bitrate")] private int bitrate = ReplayRenderSettings.BitrateKbps;
        [UIValue("extraFFmpegArgs")] private string extraFFmpegArgs = ReplayRenderSettings.ExtraFFmpegArgs;

        // presets
        [UIValue("preset-options")] private List<string> presetOptions = new List<string>() { "Low", "Medium", "High" };
        [UIValue("preset-option")] private string currentPreset = ReplayRenderSettings.Preset.ToString();

        // camera
        [UIValue("replayCameraFOV")] private float replayCameraFOV = ReplayRenderSettings.RenderFOV;

        [UIValue("currentXValueOffset")] private float currentXValueOffset = ReplayRenderSettings.RenderOffset.x;
        [UIValue("currentYValueOffset")] private float currentYValueOffset = ReplayRenderSettings.RenderOffset.y;
        [UIValue("currentZValueOffset")] private float currentZValueOffset = ReplayRenderSettings.RenderOffset.z;

        // actions

        [UIAction("OnToggleChanged")]
        private void OnToggleChanged(bool value) => ReplayRenderSettings.RenderEnabled = value;

        [UIAction("OnWidthChanged")]
        private void OnWidthChanged(int value) => ReplayRenderSettings.Width = value;

        [UIAction("OnHeightChanged")]
        private void OnHeightChanged(int value) => ReplayRenderSettings.Height = value;

        [UIAction("OnFPSChanged")]
        private void OnFPSChanged(int value) => ReplayRenderSettings.FPS = value;

        [UIAction("OnBitrateChanged")]
        private void OnBitrateChanged(int value) => ReplayRenderSettings.BitrateKbps = value;

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

        [UIAction("OnReplayCameraFOVChanged")]
        private void OnReplayCameraFOVChanged(float value) => ReplayRenderSettings.RenderFOV = value;

        [UIAction("OnXValueOffsetChanged")]
        private void OnXValueOffsetChanged(float value) => ReplayRenderSettings.RenderOffset = new Vector3(value, ReplayRenderSettings.RenderOffset.y, ReplayRenderSettings.RenderOffset.z);
        [UIAction("OnYValueOffsetChanged")]
        private void OnYValueOffsetChanged(float value) => ReplayRenderSettings.RenderOffset = new Vector3(ReplayRenderSettings.RenderOffset.x, value, ReplayRenderSettings.RenderOffset.z);
        [UIAction("OnZValueOffsetChanged")]
        private void OnZValueOffsetChanged(float value) => ReplayRenderSettings.RenderOffset = new Vector3(ReplayRenderSettings.RenderOffset.x, ReplayRenderSettings.RenderOffset.y, value);

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
                Debug.LogError($"Failed to build encoder args: {ex}");
            }
        }

        // lifecycle methods

        public void Initialize()
        {
            gameplaySetup.AddTab("Render Mod", "RenderMod.UI.RenderModView.bsml", this, MenuType.Solo);
        }

        public void Dispose()
        {
            gameplaySetup?.RemoveTab("Render Mod");
        }
    }
}
