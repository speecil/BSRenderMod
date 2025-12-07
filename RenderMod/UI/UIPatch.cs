using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using IPA.Utilities;
using RenderMod.Render;
using RenderMod.UI;
using SiraUtil.Affinity;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace RenderMod
{
    internal class UIPatch : IInitializable, IDisposable, IAffinity
    {
        [Inject] private GameplaySetup gameplaySetup;
        [Inject] private readonly RenderSettingsFlow renderSettingsVFlow;

        private Button _actionButton;

        [UIValue("renderEnabled")] private bool renderEnabled = ReplayRenderSettings.RenderEnabled;

        [UIAction("OnToggleChanged")]
        private void OnToggleChanged(bool value)
        {
            ReplayRenderSettings.RenderEnabled = value;
            if (_actionButton != null)
            {
                _actionButton.interactable = !value;
            }
        }

        [UIAction("OpenSettings")]
        private void OpenSettings()
        {
            Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().FirstOrDefault().PresentFlowCoordinator(renderSettingsVFlow);
        }

        public void Initialize()
        {
            gameplaySetup.AddTab("Render Mod", "RenderMod.UI.RenderModView.bsml", this, MenuType.Solo);
        }

        public void Dispose()
        {
            gameplaySetup?.RemoveTab("Render Mod");
        }

        [AffinityPatch(typeof(StandardLevelDetailView), "RefreshContent")]
        [AffinityPostfix]
        public void daPatch(StandardLevelDetailView __instance)
        {
            var button = __instance.GetField<Button, StandardLevelDetailView>("_actionButton");
            _actionButton = button;
            if (_actionButton == null) return;
            _actionButton.interactable = !ReplayRenderSettings.RenderEnabled;
        }
    }
}
