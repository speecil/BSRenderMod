using Zenject;

namespace RenderMod.Installers
{
    internal class MenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<UIPatch>().AsSingle();
        }
    }
}
