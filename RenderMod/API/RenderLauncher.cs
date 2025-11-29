using RenderMod.AffinityPatches;
using RenderMod.Render;
using System;

namespace RenderMod.API
{
    public class RenderLauncher
    {
        public static bool previousState = false;

        public static void Launch(Action onComplete)
        {
            BeatLeaderWarningPatch.shouldNotInterfere = true;
            previousState = ReplayRenderSettings.RenderEnabled;
            ReplayRenderSettings.RenderEnabled = true;
            onComplete += OnRenderComplete;
        }

        public static void OnRenderComplete()
        {
            BeatLeaderWarningPatch.shouldNotInterfere = false;
            ReplayRenderSettings.RenderEnabled = previousState;
        }
    }
}
