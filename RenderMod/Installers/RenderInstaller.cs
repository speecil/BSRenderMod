using RenderMod.Render;
using Zenject;

namespace RenderMod.Installers
{
    internal class RenderInstaller : Installer
    {
        public override void InstallBindings()
        {
            if (!ReplayRenderSettings.RenderEnabled)
            {
                return;
            }
            Container.BindInterfacesAndSelfTo<ReplayVideoRenderer>().AsSingle();
            Container.BindInterfacesAndSelfTo<ReplayProgressUI>().AsSingle();
            Container.BindInterfacesAndSelfTo<SettingsApplicator>().AsSingle().NonLazy();
        }
    }
}
