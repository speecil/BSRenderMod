using BeatSaberMarkupLanguage.MenuButtons;
using System;
using Zenject;

namespace RenderMod.UI
{
    internal class MenuButtonView : IInitializable, IDisposable
    {
        private readonly MenuButton _menuButton;
        [Inject] private readonly MainFlowCoordinator _mainFlowCoordinator;
        [Inject] private readonly RenderSettingsFlow _renderModFlow;
        [Inject] private readonly MenuButtons _menuButtons;

        public MenuButtonView(RenderSettingsFlow renderSettingsFlow)
        {
            _renderModFlow = renderSettingsFlow;
            _menuButton = new MenuButton("Render Mod", "Adjust settings for renders", new Action(this.PresentTheFlow), true);
        }

        public void Initialize()
        {
            _menuButtons.RegisterButton(_menuButton);
            _menuButton.Interactable = true;
        }

        public void PresentTheFlow()
        {
            _mainFlowCoordinator.PresentFlowCoordinator(_renderModFlow);
        }

        public void Dispose()
        {
            MenuButtons.Instance.UnregisterButton(this._menuButton);
        }
    }
}
