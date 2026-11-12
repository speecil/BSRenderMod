using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using RenderMod.Render;
using SiraUtil.Affinity;
using System;
using System.Linq;
using UnityEngine;
using Zenject;

namespace RenderMod.UI
{
    internal class UIPatch : IInitializable, IDisposable, IAffinity
    {
        [Inject] private readonly RenderSettingsFlow _renderSettingsFlow = null;
        [Inject] private readonly GameScenesManager _gameScenesManager = null;
#if BS_1_29
        private GameplaySetup _gameplaySetup => GameplaySetup.instance;
#else
        [Inject] private readonly GameplaySetup _gameplaySetup = null;
#endif

        [UIValue("renderEnabled")] private bool renderEnabled = ReplayRenderSettings.RenderEnabled;

        [UIAction("OnToggleChanged")]
        private void OnToggleChanged(bool value)
        {
            ReplayRenderSettings.RenderEnabled = value;
        }

        [UIAction("OpenSettings")]
        private void OpenSettings()
        {
            Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().FirstOrDefault()?.PresentFlowCoordinator(_renderSettingsFlow);
        }

        public void Initialize()
        {
            _gameplaySetup?.AddTab("Render Mod", "RenderMod.UI.RenderModView.bsml", this, MenuType.Solo);
        }

        public void Dispose()
        {
            _gameplaySetup?.RemoveTab("Render Mod");
        }

        [AffinityPatch(typeof(SinglePlayerLevelSelectionFlowCoordinator), "ActionButtonWasPressed")]
        [AffinityPostfix]
        private void ActionButtonWasPressedPostfix()
        {
            if (!_gameScenesManager.isInTransition)
                return;
            if (ReplayRenderSettings.RenderEnabled)
                RenderManager.StartRender(RenderManager.ManualId);
            else
                RenderManager.CancelRender();
        }
    }
}
