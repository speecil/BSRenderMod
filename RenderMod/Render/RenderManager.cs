using RenderMod.Api;
using RenderMod.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using IPALogger = IPA.Logging.Logger;

namespace RenderMod.Render
{
    internal static class RenderManager
    {
        public const string ManualId = "Manual";
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

        public static IPALogger Log;

        public static bool RenderRequested { get; private set; }
        public static string Leaderboard { get; private set; }
        private static DateTime _requestTime;
        private static volatile bool _finished;
        private static string _finishedLeaderboard, _finishedPath;

        public static bool RequestExpired => DateTime.UtcNow - _requestTime > RequestTimeout;

        public static void StartRender(string leaderboard)
        {
            Log.Notice($"Starting {leaderboard} render");
            EncoderHelpers.PrepareEncoder();
            Leaderboard = leaderboard;
            _requestTime = DateTime.UtcNow;
            RenderRequested = true;
        }

        public static void CancelRender() => RenderRequested = false;

        public static void FailRender()
        {
            RenderRequested = false;
            _finishedLeaderboard = Leaderboard;
            _finishedPath = null;
            _finished = true;
        }

        public static void FinishRender(string videoPath, string audioPath)
        {
            RenderRequested = false;
            _finishedLeaderboard = Leaderboard;
            Task.Run(() =>
            {
                _finishedPath = Mux(videoPath, audioPath);
                _finished = true;
            });
        }

        public static void RaiseFinished()
        {
            if (!_finished)
                return;
            _finished = false;
            RenderModApi.RaiseRenderFinished(_finishedLeaderboard, _finishedPath, _finishedPath != null);
        }

        private static string Mux(string videoPath, string audioPath)
        {
            string finishedDir = Path.Combine(ReplayRenderSettings.RenderRoot, "Finished");
            Directory.CreateDirectory(finishedDir);

            string baseName = Path.GetFileNameWithoutExtension(videoPath);
            string tempMp4 = Path.Combine(finishedDir, baseName + "_temp.mp4");
            string finalMp4 = Path.GetFullPath(Path.Combine(finishedDir, baseName + ".mp4"));

            try
            {
                if (!FFmpegPipe.RemuxRawStreamToMp4(videoPath, tempMp4, ReplayRenderSettings.FPS))
                    throw new InvalidOperationException("Video remux failed.");
                if (!FFmpegPipe.AddAudioToMp4(tempMp4, audioPath, finalMp4, ReplayRenderSettings.AudioBitrateKbps))
                    throw new InvalidOperationException("Audio mux failed.");

                Log.Notice($"Render finished: {finalMp4}");
                File.Delete(videoPath);
                File.Delete(audioPath);
                Process.Start("explorer.exe", $"/select,\"{finalMp4}\"");
                DingPlayer.shouldPlayDing = true;
                return finalMp4;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to mux video and audio: {ex}");
                return null;
            }
        }
    }
}
