using IPA;
using IPA.Utilities;
using RenderMod.Render;
using SiraUtil.Zenject;
using System.IO;
using System.Linq;
using IPALogger = IPA.Logging.Logger;

namespace RenderMod
{
    [Plugin(RuntimeOptions.SingleStartInit), NoEnableDisable]
    public class Plugin
    {
        [Init]
        public Plugin(IPALogger logger, Zenjector zenjector)
        {
            if (!System.Environment.GetCommandLineArgs().Contains("fpfc"))
            {
                return; // this mod is only usable in fpfc (do you really want to kill your gpu that badly?)
            }
            if(!File.Exists(Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe")))
            {
                logger.Error("ffmpeg.exe not found in the game directory! Exiting!");
                return;
            }
            zenjector.UseLogger(logger);
            zenjector.Install<Installers.MenuInstaller>(Location.Menu);
            zenjector.Install<Installers.RenderInstaller>(Location.GameCore);
        }

        [OnExit]
        public void OnExit()
        {
            ReplayRenderSettings.SaveSettings();
        }
    }
}
