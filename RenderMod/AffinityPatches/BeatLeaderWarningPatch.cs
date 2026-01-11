using HarmonyLib;
using HMUI;
using IPA.Utilities;
using RenderMod.Render;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RenderMod.AffinityPatches
{
    [HarmonyPatch]
    internal class BeatLeaderWarningPatch
    {
        public static bool shouldNotInterfere = false;

        public static object instance = null;

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "BeatLeader");
            if (asm == null) return null;

            var type = asm.GetType("BeatLeader.Components.ReplayPanel");
            if (type == null) return null;

            return AccessTools.Method(
                type,
                "OnPlayButtonClicked");
        }

        [HarmonyPrefix]
        public static bool Prefix(
            object __instance)
        {
            instance = __instance;
            if (!ReplayRenderSettings.RenderEnabled || shouldNotInterfere)
            {
                shouldNotInterfere = false;
                return true;
            }

            var flow = Resources.FindObjectsOfTypeAll<SinglePlayerLevelSelectionFlowCoordinator>()
                .FirstOrDefault(f => f.isActiveAndEnabled);
            if (flow == null)
                return true;

            ShowWarningPopup(
                flow,
                () =>
                {
                    shouldNotInterfere = true;
                    RenderManager.StartVideoRender(true);
                },
                "Render Mod",
                $"About to render a BeatLeader Replay\n" +
                $"\nContinue?"
            );

            return false;
        }

        [HarmonyCleanup]
        public static void Cleanup()
        {
            shouldNotInterfere = false;
        }

        private static void ShowWarningPopup(
            SinglePlayerLevelSelectionFlowCoordinator flow,
            Action yesCallback,
            string title,
            string message)
        {
            var view = ReflectionUtil.GetField
                <SafeAreaFocusedSimpleDialogPromptViewController, SinglePlayerLevelSelectionFlowCoordinator>(
                    flow, "_safeAreaFocusedSimpleDialogPromptViewController");

            view.Init(
                title,
                message,
                "Yes",
                "No",
                button =>
                {
                    ReflectionUtil.InvokeMethod<object, SinglePlayerLevelSelectionFlowCoordinator>(
                        flow,
                        "DismissViewController",
                        new object[] {
                            view,
                            ViewController.AnimationDirection.Horizontal,
                            null,
                            false
                        });

                    if (button == 0)
                        yesCallback?.Invoke();
                    if (button == 1)
                    {
                        // fuh no bruh
                    }
                });

            ReflectionUtil.InvokeMethod<object, SinglePlayerLevelSelectionFlowCoordinator>(
                flow,
                "PresentViewController",
                new object[] {
                    view,
                    null,
                    ViewController.AnimationDirection.Horizontal,
                    false
                });
        }

        public static bool ShouldPatch(Harmony harmony)
        {
            if (TargetMethod() != null)
            {
                try
                {
                    harmony.Patch(TargetMethod(), prefix: new HarmonyMethod(typeof(BeatLeaderWarningPatch).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.Public)));
                    return true;
                }
                catch
                {
                    Debug.LogError("Render Mod: Failed to patch BeatLeader replay button!");
                }
            }
            return false;
        }
    }
}
