using Zenject;

namespace RenderMod.Installers
{
    internal class MenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            //Container.BindInterfacesAndSelfTo<UIPatch>().AsSingle();
            BeatSaberMarkupLanguage.GameplaySetup.GameplaySetup.instance.AddTab("Render Mod", "RenderMod.UI.RenderModView.bsml", UIPatch.instance);
        }
    }
}
