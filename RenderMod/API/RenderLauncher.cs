using RenderMod.AffinityPatches;
using RenderMod.Render;
using System;

namespace RenderMod.API
{
    public class RenderLauncher
    {
        private static bool previousState = false;

        public static void Launch(Action onRenderFinished)
        {
            BeatLeaderWarningPatch.shouldNotInterfere = true;
            ScoreSaberWarningPatch.shouldNotInterfere = true;
            previousState = ReplayRenderSettings.RenderEnabled;
            ReplayRenderSettings.RenderEnabled = true;
            onRenderFinished += OnRenderComplete;
        }

        public static void OnRenderComplete()
        {
            BeatLeaderWarningPatch.shouldNotInterfere = false;
            ScoreSaberWarningPatch.shouldNotInterfere = false;
            ReplayRenderSettings.RenderEnabled = previousState;
        }
    }
}
