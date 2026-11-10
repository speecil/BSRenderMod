using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using IPA.Utilities;
using RenderMod.Render;
using SiraUtil.Affinity;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace RenderMod.UI
{
    internal class UIPatch : IInitializable, IDisposable, IAffinity
    {
        [Inject] private readonly GameplaySetup _gameplaySetup = null;
        [Inject] private readonly RenderSettingsFlow _renderSettingsFlow = null;

        private Button _playButton;

        [UIValue("renderEnabled")] private bool renderEnabled = ReplayRenderSettings.RenderEnabled;

        [UIAction("OnToggleChanged")]
        private void OnToggleChanged(bool value)
        {
            ReplayRenderSettings.RenderEnabled = value;
            if (_playButton != null)
                _playButton.interactable = !value;
        }

        [UIAction("OpenSettings")]
        private void OpenSettings()
        {
            Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().FirstOrDefault()?.PresentFlowCoordinator(_renderSettingsFlow);
        }

        public void Initialize()
        {
            _gameplaySetup.AddTab("Render Mod", "RenderMod.UI.RenderModView.bsml", this, MenuType.Solo);
        }

        public void Dispose()
        {
            _gameplaySetup?.RemoveTab("Render Mod");
        }

        [AffinityPatch(typeof(StandardLevelDetailView), "RefreshContent")]
        [AffinityPostfix]
        private void RefreshContentPostfix(StandardLevelDetailView __instance)
        {
            _playButton = __instance.GetField<Button, StandardLevelDetailView>("_actionButton");
            if (_playButton != null)
                _playButton.interactable = !ReplayRenderSettings.RenderEnabled;
        }
    }
}
