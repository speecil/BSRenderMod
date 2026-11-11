using HMUI;
using IPA.Utilities;
using RenderMod.Render;
using Zenject;

namespace RenderMod.UI
{
    internal class RenderSettingsFlow : FlowCoordinator
    {
        [Inject] private readonly RenderSettingsView _renderSettingsView = null;

#if BS_1_29
        public override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
#else
        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
#endif
        {
            if (firstActivation)
            {
                SetTitle("Render Settings");
                showBackButton = true;
                ProvideInitialViewControllers(_renderSettingsView);
            }
        }

#if BS_1_29
        public override void BackButtonWasPressed(ViewController topViewController)
#else
        protected override void BackButtonWasPressed(ViewController topViewController)
#endif
        {
            ReplayRenderSettings.SaveSettings();
            this.GetField<FlowCoordinator, FlowCoordinator>("_parentFlowCoordinator")
                .DismissFlowCoordinator(this, ViewController.AnimationDirection.Horizontal);
        }
    }
}
