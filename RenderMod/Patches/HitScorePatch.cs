#if !BS_1_29
using SiraUtil.Affinity;
using UnityEngine;

namespace RenderMod.Patches
{
    internal class HitScorePatch : IAffinity
    {
        [AffinityPatch(typeof(FlyingScoreSpawner), nameof(FlyingScoreSpawner.SpawnFlyingScoreNextFrame))]
        [AffinityPrefix]
        private bool Prefix(FlyingScoreSpawner __instance, IReadonlyCutScoreBuffer cutScoreBuffer, Color color)
        {
            __instance.SpawnFlyingScore(cutScoreBuffer, color);
            return false;
        }
    }
}
#endif
