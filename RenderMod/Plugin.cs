using HarmonyLib;
using IPA;
using IPA.Utilities;
using RenderMod.AffinityPatches;
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
            if (!File.Exists(Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe")))
            {
                logger.Error("ffmpeg.exe not found in the game directory! Exiting!");
                return;
            }
            var _harmony = new Harmony("com.speecil.beatsaber.rendermod");
            if (!ScoreSaberWarningPatch.ShouldPatch(_harmony))
            {
                logger.Warn("ScoreSaber not found, skipping ScoreSaber replay warning patch.");
            }
            if (!BeatLeaderWarningPatch.ShouldPatch(_harmony))
            {
                logger.Warn("BeatLeader not found, skipping BeatLeader replay warning patch.");
            }
            if (!AutoPauseStealthPatch.ShouldPatch(_harmony))
            {
                logger.Warn("AutoPauseStealth not found, skipping AutoPauseStealth patch.");
            }
            RenderManager._log = logger;
            zenjector.UseLogger(logger);
            zenjector.Install<Installers.AppInstaller>(Location.App);
            zenjector.Install<Installers.MenuInstaller>(Location.Menu);
            zenjector.Install<Installers.RenderInstaller>(Location.GameCore);

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += RenderManager.SceneChange;
        }

        [OnExit]
        public void OnExit()
        {
            ReplayRenderSettings.SaveSettings();
        }
    }
}
