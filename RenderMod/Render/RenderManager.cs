using IPA.Logging;
using RenderMod.AffinityPatches;
using RenderMod.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace RenderMod.Render
{
    internal class RenderManager
    {
        public static Logger _log;
        public enum RenderState
        {
            None,
            Video,
            Audio
        }

        public static RenderState currentState = RenderState.None;

        private static bool beatleaderRender = false;

        public static void StartVideoRender(bool beatleader)
        {
            currentState = RenderState.Video;
            beatleaderRender = beatleader;
            if (beatleaderRender)
            {
                _log.Notice("Starting video render for BeatLeader replay");
                BeatLeaderWarningPatch.TargetMethod()?.Invoke(BeatLeaderWarningPatch.instance, null);
                BeatLeaderWarningPatch.shouldNotInterfere = true;
            }
            else
            {
                _log.Notice("Starting video render for ScoreSaber replay");
                ScoreSaberWarningPatch.TargetMethod()?.Invoke(ScoreSaberWarningPatch.instance, null);
                ScoreSaberWarningPatch.shouldNotInterfere = true;
            }
        }

        public static void StartAudioCapture()
        {
            currentState = RenderState.Audio;
            if (beatleaderRender)
            {
                _log.Notice("Starting audio render for BeatLeader replay");
                BeatLeaderWarningPatch.TargetMethod()?.Invoke(BeatLeaderWarningPatch.instance, null);
            }
            else
            {
                _log.Notice("Starting audio render for ScoreSaber replay");
                ScoreSaberWarningPatch.TargetMethod()?.Invoke(ScoreSaberWarningPatch.instance, null);
            }
        }

        public static void StopRender()
        {
            if (currentState == RenderState.None)
            {
                _log.Error("No render in progress to stop.");
                return;
            }

            currentState = RenderState.None;
            BeatLeaderWarningPatch.shouldNotInterfere = false;
            ScoreSaberWarningPatch.shouldNotInterfere = false;

            var renderRoot = ReplayRenderSettings.RenderRoot;
            var unfinishedDir = Path.Combine(renderRoot, "Unfinished");
            var finishedDir = Path.Combine(renderRoot, "Finished");

            Directory.CreateDirectory(unfinishedDir);
            Directory.CreateDirectory(finishedDir);

            var codec = ReplayRenderSettings.VideoCodec;
            if (codec == "av1")
            {
                codec = "ivf";
            }
            var latestVideoFile = Directory.GetFiles(unfinishedDir, "*." + codec)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault()?.FullName;

            var latestAudioFile = Directory.GetFiles(unfinishedDir, "*.wav")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault()?.FullName;

            if (latestVideoFile == null || latestAudioFile == null)
            {
                _log.Error("No video or audio files found to mux.");
                return;
            }

            _log.Notice($"Muxing video '{latestVideoFile}' with audio '{latestAudioFile}'");

            string videoBaseName = Path.GetFileNameWithoutExtension(latestVideoFile);
            string tempVideoMp4 = Path.Combine(finishedDir, videoBaseName + "_temp.mp4");
            string finalMp4 = Path.Combine(finishedDir, videoBaseName + ".mp4");

            try
            {
                _log.Notice($"Muxing raw video stream to mp4...");
                FFmpegPipe.RemuxRawStreamToMp4(latestVideoFile, tempVideoMp4, ReplayRenderSettings.FPS);
                _log.Notice($"Raw video stream remuxed to MP4: {tempVideoMp4}");
                
                _log.Notice($"Muxing audio and video into a final mp4 file...");
                FFmpegPipe.AddAudioToMp4(tempVideoMp4, latestAudioFile, finalMp4, ReplayRenderSettings.AudioBitrateKbps);
                _log.Notice($"Final muxed MP4 created: {finalMp4}");

                Process.Start("explorer.exe", $"/select,\"{finalMp4}\"");
                DingPlayer.shouldPlayDing = true;
                //deleting temp files
                File.Delete(latestVideoFile);
                File.Delete(latestAudioFile);
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to mux video and audio: {ex}");
            }
        }

        internal static string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = fileName;

            sanitized = sanitized.Replace(' ', '_');

            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }

            for (int i = 0; i < sanitized.Length; i++)
            {
                if (sanitized[i] > 127)
                {
                    sanitized = sanitized.Remove(i, 1).Insert(i, "_");
                }
            }

            return sanitized;
        }

        internal static void SceneChange(Scene scene1, Scene scene2)
        {
            _log.Debug($"Scene changed from {scene1.name} to {scene2.name}");
            switch (currentState)
            {
                case RenderState.None:
                    break;
                case RenderState.Video:
                    
                    if (scene2.name.ToLower() != "gamecore")
                    {
                        _log.Notice("Exiting gameplay scene, starting audio capture in 2 seconds");
                        // leaving gameplay (render end)
                        var codec = ReplayRenderSettings.VideoCodec;
                        if (codec == "av1") //have to delay audio capture a bit more when using av1, ffmpeg takes longer to finish doing its av1 shit
                        {
                            _log.Notice("AV1 detected, waiting 15 seconds instead of 2...");
                            Task.Delay(15000).ContinueWith(t => StartAudioCapture()); //ideally wait for ffmpeg exit and then delay by 2 seconds, fml
                        }
                        else
                        {
                            Task.Delay(2000).ContinueWith(t => StartAudioCapture());
                        }
                        
                    }
                    break;
                case RenderState.Audio:
                    if (scene2.name.ToLower() == "gamecore")
                    {
                        // going to gameplay for audio pass
                        // do nothing, render has already been started
                        _log.Notice("Entering gameplay scene for audio capture");
                    }
                    else
                    {
                        // leaving gameplay (render end)
                        _log.Notice("Exiting gameplay scene, stopping render management in 2 seconds");
                        Task.Delay(2000).ContinueWith(t => StopRender());
                    }
                    break;
                default:
                    break;
            }
        }
    }
}