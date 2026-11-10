using RenderMod.Render;
using SiraUtil.Tools.FPFC;
using Zenject;

namespace RenderMod.Installers
{
    internal class RenderInstaller : Installer
    {
        public override void InstallBindings()
        {
            if (!Container.HasBinding<IFPFCSettings>() || !ReplayRenderSettings.RenderEnabled)
                return;

            Container.BindInterfacesAndSelfTo<ReplayRenderer>().AsSingle().NonLazy();
            Container.Bind<ReplayProgressUI>().AsSingle();
        }
    }
}
