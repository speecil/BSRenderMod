using HMUI;
using IPA.Utilities;
using RenderMod.Render;
using Zenject;

namespace RenderMod.UI
{
    internal class RenderSettingsFlow : FlowCoordinator
    {
        [Inject] private readonly RenderSettingsView _renderSettingsView = null;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (firstActivation)
            {
                SetTitle("Render Settings");
                showBackButton = true;
                ProvideInitialViewControllers(_renderSettingsView);
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            ReplayRenderSettings.SaveSettings();
            this.GetField<FlowCoordinator, FlowCoordinator>("_parentFlowCoordinator")
                .DismissFlowCoordinator(this, ViewController.AnimationDirection.Horizontal);
        }
    }
}
