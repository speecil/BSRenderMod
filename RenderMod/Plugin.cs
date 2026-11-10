using HarmonyLib;
using IPA;
using IPA.Utilities;
using RenderMod.Patches;
using RenderMod.Render;
using SiraUtil.Zenject;
using System;
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
            if (!Environment.GetCommandLineArgs().Contains("fpfc"))
                return;

            if (!File.Exists(Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe")))
            {
                logger.Error("ffmpeg.exe not found in the game directory! Exiting!");
                return;
            }

            var harmony = new Harmony("com.speecil.beatsaber.rendermod");
            ReplayButtonPatches.Apply(harmony, logger);
            AutoPauseStealthPatch.Apply(harmony, logger);

            RenderManager.Log = logger;
            zenjector.UseLogger(logger);
            zenjector.Install<Installers.AppInstaller>(Location.App);
            zenjector.Install<Installers.MenuInstaller>(Location.Menu);
            zenjector.Install<Installers.RenderInstaller>(Location.GameCore);

            try
            {
                UnityAudioSyncHook.Install(logger);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to install the audio sync hook, rendered audios gonna be cooked mate: {ex}");
            }
        }

        [OnExit]
        public void OnExit()
        {
            ReplayRenderSettings.SaveSettings();
        }
    }
}
