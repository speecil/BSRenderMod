using RenderMod.Render;
using SiraUtil.Tools.FPFC;
using Zenject;

namespace RenderMod.Installers
{
    internal class RenderInstaller : Installer
    {
        public override void InstallBindings()
        {
            if (!Container.HasBinding<IFPFCSettings>() || !RenderManager.RenderRequested)
                return;
            if (RenderManager.RequestExpired)
            {
                RenderManager.CancelRender();
                return;
            }

            Container.BindInterfacesAndSelfTo<ReplayRenderer>().AsSingle().NonLazy();
            Container.Bind<ReplayProgressUI>().AsSingle();
        }
    }
}
