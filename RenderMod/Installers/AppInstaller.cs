using RenderMod.Util;
using Zenject;

namespace RenderMod.Installers
{
    internal class AppInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<EffectManager>().AsSingle().NonLazy();
        }
    }
}
