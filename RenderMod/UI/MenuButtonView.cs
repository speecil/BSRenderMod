using BeatSaberMarkupLanguage.MenuButtons;
using System;
using Zenject;

namespace RenderMod.UI
{
    internal class MenuButtonView : IInitializable, IDisposable
    {
        [Inject] private readonly MainFlowCoordinator _mainFlowCoordinator = null;
        [Inject] private readonly RenderSettingsFlow _renderSettingsFlow = null;
#if BS_1_29
        private MenuButtons _menuButtons => MenuButtons.instance;
#else
        [Inject] private readonly MenuButtons _menuButtons = null;
#endif

        private MenuButton _menuButton;

        public void Initialize()
        {
            _menuButton = new MenuButton("Render Mod", "Adjust settings for renders",
                () => _mainFlowCoordinator.PresentFlowCoordinator(_renderSettingsFlow), true);
            _menuButtons.RegisterButton(_menuButton);
        }

        public void Dispose()
        {
            _menuButtons.UnregisterButton(_menuButton);
        }
    }
}
