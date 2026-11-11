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
        public static IPALogger Log;

        public static bool RenderRequested { get; private set; }

        public static void StartRender(string leaderboard)
        {
            Log.Notice($"Starting render for {leaderboard} replay");
            EncoderHelpers.PrepareEncoder();
            RenderRequested = true;
        }

        public static void CancelRender() => RenderRequested = false;

        public static void FinishRender(string videoPath, string audioPath)
        {
            RenderRequested = false;
            Task.Run(() => Mux(videoPath, audioPath));
        }

        private static void Mux(string videoPath, string audioPath)
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
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to mux video and audio: {ex}");
            }
        }
    }
}
