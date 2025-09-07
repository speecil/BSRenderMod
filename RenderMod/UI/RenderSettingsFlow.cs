using BeatSaberMarkupLanguage;
using HMUI;
using IPA.Utilities;
using RenderMod.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace RenderMod.UI
{
    internal class RenderSettingsFlow : FlowCoordinator
    {
        [Inject] private readonly RenderSettingsView _renderSettingsView;

        private FlowCoordinator _lastFlowCoordinator;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (firstActivation)
            {
                SetTitle("Render Settings");
                showBackButton = true;
                ProvideInitialViewControllers(_renderSettingsView);
            }
            _lastFlowCoordinator = this.GetField<FlowCoordinator, FlowCoordinator>("_parentFlowCoordinator");
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            base.BackButtonWasPressed(topViewController);
            _lastFlowCoordinator.DismissFlowCoordinator(this, ViewController.AnimationDirection.Horizontal);
            ReplayRenderSettings.SaveSettings();
        }
    }
}
