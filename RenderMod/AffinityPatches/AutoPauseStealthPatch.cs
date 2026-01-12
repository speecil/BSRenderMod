using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RenderMod.AffinityPatches
{
    [HarmonyPatch]
    internal class AutoPauseStealthPatch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "AutoPauseStealth");
            if (asm == null) return null;

            var type = asm.GetType("AutoPauseStealth.Plugin");
            if (type == null) return null;

            return AccessTools.Method(
                type,
                "ApplyHarmonyPatches");
        }

        [HarmonyPrefix]
        public static bool Prefix(object __instance)
        {
            return false;
        }

        [HarmonyCleanup]
        public static void Cleanup()
        {

        }

        public static bool ShouldPatch(Harmony harmony)
        {
            if (TargetMethod() != null)
            {
                try
                {
                    harmony.Patch(TargetMethod(), prefix: new HarmonyMethod(typeof(AutoPauseStealthPatch).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.Public)));
                    return true;
                }
                catch
                {
                    Debug.LogError("Render Mod: Failed to patch AutoPauseStealth");
                }
            }
            return false;
        }
    }
}
