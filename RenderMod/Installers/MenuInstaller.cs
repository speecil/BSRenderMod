using RenderMod.UI;
using RenderMod.Util;
using Zenject;

namespace RenderMod.Installers
{
    internal class MenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<RenderSettingsFlow>().FromNewComponentOnNewGameObject().AsSingle();
            Container.Bind<RenderSettingsView>().FromNewComponentAsViewController().AsSingle();
            Container.BindInterfacesAndSelfTo<MenuButtonView>().AsSingle();
            Container.BindInterfacesAndSelfTo<UIPatch>().AsSingle();
            Container.Bind<DingPlayer>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
        }
    }
}
