using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using IPA.Utilities;
using RenderMod.Render;
using RenderMod.UI;
using SiraUtil.Affinity;
using System;
using System.Collections.Generic;
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

        [UIValue("renderEnabled")] private bool renderEnabled = ReplayRenderSettings.RenderEnabled;

        [UIAction("OnToggleChanged")]
        private void OnToggleChanged(bool value)
        {
            ReplayRenderSettings.RenderEnabled = value;
            daPatch(Resources.FindObjectsOfTypeAll<StandardLevelDetailView>().FirstOrDefault());
        }

        [UIAction("OpenSettings")]
        private void OpenSettings()
        {
            Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().FirstOrDefault().PresentFlowCoordinator(renderSettingsVFlow);
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

        [AffinityPatch(typeof(StandardLevelDetailView), "RefreshContent")]
        [AffinityPostfix]
        public void daPatch(StandardLevelDetailView __instance)
        {
            var button = __instance.GetField<Button, StandardLevelDetailView>("_actionButton");
            if (button == null) return;
            if (__instance.beatmapKey.levelId.Contains("WIP"))
            {
                button.interactable = false;
                return; // dont fw it broski
            }

            button.interactable = !ReplayRenderSettings.RenderEnabled;
        }
    }
}
