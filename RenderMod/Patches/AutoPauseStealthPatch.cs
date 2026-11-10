using HarmonyLib;
using System;
using System.Linq;
using IPALogger = IPA.Logging.Logger;

namespace RenderMod.Patches
{
    // Stops AutoPauseStealth from installing its own patches, so it can't pause a render.
    internal static class AutoPauseStealthPatch
    {
        public static void Apply(Harmony harmony, IPALogger log)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "AutoPauseStealth")?
                .GetType("AutoPauseStealth.Plugin");
            var target = type == null ? null : AccessTools.Method(type, "ApplyHarmonyPatches");
            if (target == null)
                return;

            try
            {
                harmony.Patch(target, prefix: new HarmonyMethod(typeof(AutoPauseStealthPatch), nameof(Prefix)));
            }
            catch (Exception ex)
            {
                log.Error($"Failed to patch AutoPauseStealth: {ex}");
            }
        }

        private static bool Prefix() => false;
    }
}
