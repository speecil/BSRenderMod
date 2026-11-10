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
    // LocalLeaderboard.UI.ScoreInfoModal.silly
    [HarmonyPatch]
    internal class LocalLeaderboardWarningPatch
    {
        public static bool shouldNotInterfere = false;

        public static object instance = null;

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "LocalLeaderboard");
            if (asm == null) return null;

            var type = asm.GetType("LocalLeaderboard.UI.ScoreInfoModal");
            if (type == null) return null;
            return AccessTools.Method(
                type,
                "replayStart");
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
                    RenderManager.StartVideoRender("LocalLeaderboard");
                },
                "Render Mod",
                $"About to render a LocalLeaderboard Replay\n" +
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
                    harmony.Patch(TargetMethod(), prefix: new HarmonyMethod(typeof(LocalLeaderboardWarningPatch).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.Public)));
                    return true;
                }
                catch
                {
                    Debug.LogError("Render Mod: Failed to patch LocalLeaderboard replay button!");
                }
            }
            return false;
        }
    }
}
