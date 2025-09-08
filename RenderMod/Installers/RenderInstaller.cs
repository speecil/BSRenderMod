using RenderMod.Render;
using SiraUtil.Tools.FPFC;
using Zenject;

namespace RenderMod.Installers
{
    internal class RenderInstaller : Installer
    {
        public override void InstallBindings()
        {
            if (!Container.HasBinding<IFPFCSettings>())
            {
                return;
            }
            if (!ReplayRenderSettings.RenderEnabled)
            {
                return;
            }
            Container.BindInterfacesAndSelfTo<ReplayVideoRenderer>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ReplayProgressUI>().AsSingle().NonLazy();
            //Container.BindInterfacesAndSelfTo<SettingsApplicator>().AsSingle().NonLazy();
        }
    }
}
