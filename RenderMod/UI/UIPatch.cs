using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using IPA.Config.Data;
using IPA.Utilities;
using RenderMod.Render;
using RenderMod.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
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
        [UIValue("cameraSpecifier")] private string cameraSpecifier = ReplayRenderSettings.SpecifiedCameraName;
        [UIValue("bitrate")] private int bitrate = ReplayRenderSettings.BitrateKbps;
        [UIValue("extraFFmpegArgs")] private string extraFFmpegArgs = ReplayRenderSettings.ExtraFFmpegArgs;

        // presets
        [UIValue("preset-options")] private List<string> presetOptions = new List<string>() { "Low", "Medium", "High" };
        [UIValue("preset-option")] private string currentPreset = ReplayRenderSettings.Preset.ToString();

        // actions

        [UIAction("OnToggleChanged")]
        private void OnToggleChanged(bool value) => ReplayRenderSettings.RenderEnabled = value;

        [UIAction("OnWidthChanged")]
        private void OnWidthChanged(int value) => ReplayRenderSettings.Width = value;

        [UIAction("OnHeightChanged")]
        private void OnHeightChanged(int value) => ReplayRenderSettings.Height = value;

        [UIAction("OnFPSChanged")]
        private void OnFPSChanged(int value) => ReplayRenderSettings.FPS = value;

        [UIAction("OnCameraSpecifierChanged")]
        private void OnCameraSpecifierChanged(string value) => ReplayRenderSettings.SpecifiedCameraName = value;

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
            var button = Resources.FindObjectsOfTypeAll<StandardLevelDetailView>().FirstOrDefault().GetField<Button, StandardLevelDetailView>("_actionButton");
            if (button == null) return;
            if (button.gameObject.GetComponent<DependantInteractable>() != null) return;
            var x = button.gameObject.AddComponent<DependantInteractable>();
            x.interactableCheck = () => ReplayRenderSettings.RenderEnabled;
            x.Dependant = button;
        }

        public void Dispose()
        {
            gameplaySetup?.RemoveTab("Render Mod");
        }
    }
}
