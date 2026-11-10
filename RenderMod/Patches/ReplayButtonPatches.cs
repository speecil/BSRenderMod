using HarmonyLib;
using HMUI;
using IPA.Utilities;
using RenderMod.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;

namespace RenderMod.Patches
{
    // intercepts each leaderboard's "watch replay" button to ask whether to render it.
    internal static class ReplayButtonPatches
    {
        private class Target
        {
            public string Name;
            public MethodBase Method;
            public object Instance;
            public bool PassThrough;
        }

        private static readonly List<Target> Targets = new List<Target>();

        public static void Apply(Harmony harmony, IPALogger log)
        {
            TryPatch(harmony, log, "BeatLeader", "BeatLeader", "OnPlayButtonClicked",
                "BeatLeader.Components.ReplayPanel");
            TryPatch(harmony, log, "ScoreSaber", "ScoreSaber", "StartReplay",
                "ScoreSaber.UI.Elements.Leaderboard.ScoreDetailView",
                "ScoreSaber.Features.Leaderboards.UI.ScoreDetails.ScoreDetailView");
            TryPatch(harmony, log, "LocalLeaderboard", "LocalLeaderboard", "replayStart",
                "LocalLeaderboard.UI.ScoreInfoModal");
        }

        private static void TryPatch(Harmony harmony, IPALogger log, string name, string assembly, string method, params string[] typeNames)
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assembly);
            var type = typeNames.Select(t => asm?.GetType(t)).FirstOrDefault(t => t != null);
            var target = type == null ? null : AccessTools.Method(type, method);
            if (target == null)
            {
                log.Warn($"{name} not found, skipping its replay button patch.");
                return;
            }

            try
            {
                harmony.Patch(target, prefix: new HarmonyMethod(typeof(ReplayButtonPatches), nameof(Prefix)));
                Targets.Add(new Target { Name = name, Method = target });
            }
            catch (Exception ex)
            {
                log.Error($"Failed to patch {name} replay button: {ex}");
            }
        }

        private static bool Prefix(object __instance, MethodBase __originalMethod)
        {
            var target = Targets.FirstOrDefault(t => t.Method.Equals(__originalMethod));
            if (target == null)
                return true;

            target.Instance = __instance;
            if (!ReplayRenderSettings.RenderEnabled || target.PassThrough)
            {
                target.PassThrough = false;
                return true;
            }

            var flow = Resources.FindObjectsOfTypeAll<SinglePlayerLevelSelectionFlowCoordinator>()
                .FirstOrDefault(f => f.isActiveAndEnabled);
            if (flow == null)
                return true;

            ShowPrompt(flow, $"About to render a {target.Name} Replay\n\nContinue?", () =>
            {
                RenderManager.StartRender(target.Name);
                target.PassThrough = true;
                target.Method.Invoke(target.Instance, null);
            });
            return false;
        }

        private static void ShowPrompt(SinglePlayerLevelSelectionFlowCoordinator flow, string message, Action onYes)
        {
            var view = flow.GetField<SafeAreaFocusedSimpleDialogPromptViewController, SinglePlayerLevelSelectionFlowCoordinator>(
                "_safeAreaFocusedSimpleDialogPromptViewController");

            view.Init("Render Mod", message, "Yes", "No", button =>
            {
                flow.InvokeMethod<object, SinglePlayerLevelSelectionFlowCoordinator>(
                    "DismissViewController", view, ViewController.AnimationDirection.Horizontal, null, false);
                if (button == 0)
                    onYes();
            });

            flow.InvokeMethod<object, SinglePlayerLevelSelectionFlowCoordinator>(
                "PresentViewController", view, null, ViewController.AnimationDirection.Horizontal, false);
        }
    }
}
