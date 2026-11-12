using RenderMod.Render;
using System;
using System.Collections.Generic;
using System.IO;

namespace RenderMod.Api
{
    public static class RenderModApi
    {
        public const int Version = 1;

        private static readonly HashSet<string> Registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static event Action<string, string, bool> RenderFinished;

        public static bool IsRenderRequested => RenderManager.RenderRequested;

        public static void Register(string leaderboardId) => Registered.Add(leaderboardId);

        public static void Unregister(string leaderboardId) => Registered.Remove(leaderboardId);

        public static bool IsRegistered(string leaderboardId) => Registered.Contains(leaderboardId);

        public static bool RequestRender(string leaderboardId)
        {
            if (RenderManager.RenderRequested && !RenderManager.RequestExpired)
                return false;
            if (!File.Exists(FFmpegPipe.FFmpegPath))
                return false;

            RenderManager.StartRender(leaderboardId);
            return true;
        }

        public static void CancelRequest() => RenderManager.CancelRender();

        internal static void RaiseRenderFinished(string leaderboardId, string path, bool success) =>
            RenderFinished?.Invoke(leaderboardId, path, success);
    }
}
