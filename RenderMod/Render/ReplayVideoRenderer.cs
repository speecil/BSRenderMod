using IPA.Utilities;
using RenderMod.Render;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;
using static CustomLevelLoader;

public class ReplayVideoRenderer : IDisposable, IAffinity
{
    [Inject] private BeatmapLevel _beatmapLevel;
    [Inject] private SiraLog _log;
    [Inject] private ReplayProgressUI _progressUI;
    [Inject] private IReturnToMenuController _returnToMenuController;

    private AudioTimeSyncController _atsc;
    private Camera _replayCamera;

    private RenderTexture _rt;
    private FFmpegPipe _pipe;

    private string _unfinishedPath;
    private string _finishedPath;
    private string _songPath;

    private volatile bool _rendering;

    private const int BufferCount = 8;
    private byte[][] _frameBuffers;
    private int _availableSlots;

    private readonly ConcurrentDictionary<int, byte[]> _frameDict = new ConcurrentDictionary<int, byte[]>();
    private int _nextWriteIndex = 0;

    private Task _writerTask;
    private CancellationTokenSource _writerCts;
    private SemaphoreSlim _queueSignal = new SemaphoreSlim(0);
    private SemaphoreSlim _availableSlotSignal;

    private Matrix4x4 _origProj;
    private bool _hadTargetTex;
    private RenderTexture _origTarget;

    private int _w, _h, _fps;
    private bool _atscPrevEnabled;

    GameObject gcDisabler;

    [AffinityPatch(typeof(AudioTimeSyncController), nameof(AudioTimeSyncController.StartSong))]
    [AffinityPostfix]
    public void a()
    {
        gcDisabler = new GameObject("ReplayVideoRenderer");
        var gcdisable = gcDisabler.AddComponent<DisableGCWhileEnabled>();
        gcdisable.enabled = true; // disable GC while this component is enabled

        _w = ReplayRenderSettings.Width;
        _h = ReplayRenderSettings.Height;
        _fps = Mathf.Max(1, ReplayRenderSettings.FPS);

        // directory creation / file naming
        var renderRoot = ReplayRenderSettings.RenderRoot;
        Directory.CreateDirectory(Path.Combine(renderRoot, "Unfinished"));
        Directory.CreateDirectory(Path.Combine(renderRoot, "Finished"));
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _unfinishedPath = Path.Combine(renderRoot, "Unfinished", $"replay_{timestamp}_raw.h264");
        _finishedPath = Path.Combine(renderRoot, "Finished", $"replay_{timestamp}.mp4");

        _atsc = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().FirstOrDefault();
        if (_atsc == null) { _log.Error("AudioTimeSyncController not found."); return; }
        if (_beatmapLevel == null) { _log.Error("BeatmapLevel is null."); return; }

        var loader = Resources.FindObjectsOfTypeAll<CustomLevelLoader>().FirstOrDefault();
        var saveDataDict = loader?.GetField<Dictionary<string, LoadedSaveData>, CustomLevelLoader>("_loadedBeatmapSaveData");
        if (saveDataDict == null || !saveDataDict.TryGetValue(_beatmapLevel.levelID, out var saveData))
        {
            _log.Error($"No save data for {_beatmapLevel.levelID}");
            return;
        }

        // i swear to god if a mapper puts in a second .ogg or .egg file in the custom level folder, i will cry
        _songPath = Directory.GetFiles(saveData.customLevelFolderInfo.folderPath)
                             .FirstOrDefault(x =>
                             {
                                 var l = x.ToLower();
                                 return l.EndsWith(".ogg") || l.EndsWith(".egg");
                             });

        _frameBuffers = new byte[BufferCount][];
        for (int i = 0; i < BufferCount; i++)
            _frameBuffers[i] = new byte[_w * _h * 3];

        _availableSlots = BufferCount;
        _availableSlotSignal = new SemaphoreSlim(BufferCount);

        var (encoder, presetArgs) = EncoderHelpers.BuildEncoderArgs();

        string ffArgs =
            $"-y " +
            $"-f rawvideo " +
            $"-pix_fmt rgb24 " +
            $"-s {_w}x{_h} " +
            $"-r {_fps} " +
            $"-i - " +
            $"-vf vflip " +
            $"{presetArgs} " +
            $"\"{_unfinishedPath}\"";



        _pipe = new FFmpegPipe(ffArgs);

        // start the writer task
        _writerCts = new CancellationTokenSource();
        _writerTask = Task.Run(() => WriterTaskProc(_writerCts.Token));

        _rendering = true;
        _atscPrevEnabled = _atsc.enabled;
        _atsc.StartCoroutine(RenderReplayCoroutine());
    }

    float oldCaptureDeltaTime;

    private Quaternion _lastCameraRot;
    private IEnumerator RenderReplayCoroutine()
    {
        _progressUI.Show();
        while (!_atsc.isReady || !_atsc.isAudioLoaded)
        {
            yield return null;
        }
        _atsc.StopSong();

        // get the replay camera (bl names theirs, ss doesnt and uses main)
        int tries = 0;
        while (_replayCamera == null || !_replayCamera.gameObject.activeInHierarchy)
        {
            if (tries > 10)
            {
                _log.Error("Replay camera not found after 10 tries, giving up.");
                yield break;
            }

            _replayCamera = Resources.FindObjectsOfTypeAll<Camera>().Where(c => c.name == "ReplayerViewCamera").FirstOrDefault();
            if (_replayCamera != null && _replayCamera.gameObject.activeInHierarchy)
                break;

            tries++;
            yield return new WaitForEndOfFrame();
        }

        if (_replayCamera == null || !_replayCamera.gameObject.activeInHierarchy)
        {
            _log.Error("Replay camera not found or not active.");
            _replayCamera = Camera.main; // fallback to main camera
        }

        if (_replayCamera == null)
        { _log.Error("Replay camera not found."); yield break; }

        _replayCamera.enabled = true;

        // make the render texture (this is where the magic happens)
        _rt = new RenderTexture(_w, _h, 24, RenderTextureFormat.ARGB32)
        {
            useMipMap = false,
            autoGenerateMips = false,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        _rt.Create();

        _hadTargetTex = _replayCamera.targetTexture != null;
        _origTarget = _replayCamera.targetTexture;
        _replayCamera.targetTexture = _rt;
        _origProj = _replayCamera.projectionMatrix;

        _lastCameraRot = _replayCamera.transform.rotation;

        int warmupFrames = Mathf.RoundToInt(_fps * 2f);

        try
        {
            // init
            _atsc.StopSong();
            _atsc.SeekTo(0);

            // get the delta time setup
            oldCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / _fps;

            float songLen = _atsc.songLength;
            float dt = 1f / _fps;
            int frameIndex = 0;

            for (float t = 0f; t < songLen; t += dt)
            {
                SetSongTime(t);

                Quaternion targetRot = _replayCamera.transform.rotation;
                _replayCamera.transform.rotation = Quaternion.Slerp(_lastCameraRot, targetRot, 0.2f);
                _lastCameraRot = _replayCamera.transform.rotation;

                yield return WaitForSlotAsync();

                int bufferIdx = frameIndex % BufferCount;

                int capturedIndex = frameIndex;
                AsyncGPUReadback.Request(_rt, 0, TextureFormat.RGB24, req =>
                {
                    if (!req.hasError)
                    {
                        var data = req.GetData<byte>();
                        data.CopyTo(_frameBuffers[bufferIdx]);

                        _frameDict[capturedIndex] = _frameBuffers[bufferIdx];
                        _queueSignal.Release();
                    }
                    else
                    {
                        _log.Warn("GPU readback failed for a frame.");
                    }

                    Interlocked.Increment(ref _availableSlots);
                    _availableSlotSignal.Release();
                });

                _availableSlots--;
                frameIndex++;

                _progressUI.UpdateProgress(Mathf.Clamp01(t / Mathf.Max(0.0001f, songLen)));
            }

            // wait for all frames to be processed
            while (!_frameDict.IsEmpty || _availableSlots < BufferCount)
                yield return null;
        }
        finally
        {
            _rendering = false;
            _log.Notice("Stopping replay rendering...");
            _writerCts?.Cancel();
            try { _writerTask?.Wait(5000); } catch { }
            _pipe?.Close();
            _log.Notice("Replay rendering stopped.");

            if (_replayCamera != null)
            {
                _replayCamera.projectionMatrix = _origProj;
                _replayCamera.targetTexture = _hadTargetTex ? _origTarget : null;
            }

            if (_rt != null)
            {
                try { _rt.Release(); } catch { }
                try { UnityEngine.Object.Destroy(_rt); } catch { }
                _rt = null;
            }

            if (_atsc != null) _atsc.enabled = _atscPrevEnabled;

            _progressUI.Hide();
            Time.captureDeltaTime = oldCaptureDeltaTime;
            _returnToMenuController.ReturnToMenu();
        }
    }

    private IEnumerator WaitForSlotAsync()
    {
        bool acquired = false;
        while (!acquired)
        {
            if (_availableSlotSignal.Wait(0))
            {
                acquired = true;
            }
            else
            {
                yield return null;
            }
        }
    }

    private async Task WriterTaskProc(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_frameDict.TryRemove(_nextWriteIndex, out var frame))
            {
                try { _pipe.WriteFrame(frame); } catch (Exception e) { _log.Warn($"Failed to write frame: {e}"); }

                _nextWriteIndex++;
            }
            else
            {
                try { await _queueSignal.WaitAsync(token); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private void RemuxRawH264ToMp4(string rawH264Path, string outputMp4Path)
    {
        _log.Notice($"Wrapping raw H264 {rawH264Path} into MP4 {outputMp4Path}...");

        var ffm = new Process();
        ffm.StartInfo.FileName = "ffmpeg";
        ffm.StartInfo.Arguments =
            $"-y -i \"{rawH264Path}\" " +
            "-c:v copy " +
            $"\"{outputMp4Path}\"";
        ffm.StartInfo.UseShellExecute = false;
        ffm.StartInfo.CreateNoWindow = true;
        ffm.Start();
        ffm.WaitForExit();

        _log.Notice($"MP4 without audio created: {outputMp4Path}");
    }

    private void AddAudioToMp4(string videoMp4Path, string audioPath, string finalMp4Path)
    {
        if (!File.Exists(audioPath))
        {
            _log.Error($"Audio file not found: {audioPath}, skipping audio mux.");
            return;
        }

        _log.Notice($"Adding audio {audioPath} to {videoMp4Path} -> {finalMp4Path}...");

        var ffm = new Process();
        ffm.StartInfo.FileName = "ffmpeg";
        ffm.StartInfo.Arguments =
            $"-y -i \"{videoMp4Path}\" -i \"{audioPath}\" -c:v copy -c:a aac -shortest \"{finalMp4Path}\"";
        ffm.StartInfo.UseShellExecute = false;
        ffm.StartInfo.CreateNoWindow = true;
        ffm.Start();
        ffm.WaitForExit();

        File.Delete(videoMp4Path); // clean up the intermediate video file

        _log.Notice($"Final MP4 with audio created: {finalMp4Path}");
    }

    private void RemuxAsync(string videoPath, string audioPath, string outputPath, int captureFps)
    {
        _log.Notice($"Remuxing video from {videoPath} and audio from {audioPath} to {outputPath}...");

        if (!File.Exists(videoPath))
        {
            _log.Error($"Raw video file not found: {videoPath}, skipping remux.");
            return;
        }

        // make it an mp4 without audio to begin with
        string videoOnlyMp4 = Path.Combine(Path.GetDirectoryName(outputPath),
                                           Path.GetFileNameWithoutExtension(outputPath) + "_video.mp4");

        RemuxRawH264ToMp4(videoPath, videoOnlyMp4);

        // audio pass (muxed from ogg)
        AddAudioToMp4(videoOnlyMp4, audioPath, outputPath);

        _log.Notice($"Remuxing complete. Final MP4: {outputPath}");
    }



    public void Dispose()
    {
        _rendering = false;
        _writerCts?.Cancel();
        try { _writerTask?.Wait(5000); } catch { }

        _pipe?.Close();

        if (_replayCamera != null)
        {
            _replayCamera.projectionMatrix = _origProj;
            _replayCamera.targetTexture = _hadTargetTex ? _origTarget : null;
        }

        if (_rt != null)
        {
            try { _rt.Release(); } catch { }
            try { UnityEngine.Object.Destroy(_rt); } catch { }
            _rt = null;
        }

        // fail safe to mux just in case we exit early
        RemuxAsync(_unfinishedPath, _songPath, _finishedPath, _fps);
        ClearUnfinishedDirectory();
        GameObject.Destroy(gcDisabler);
    }

    private void ClearUnfinishedDirectory()
    {
        var unfinishedDir = Path.Combine(ReplayRenderSettings.RenderRoot, "Unfinished");
        if (Directory.Exists(unfinishedDir))
        {
            try
            {
                foreach (var item in Directory.GetFiles(unfinishedDir))
                {
                    File.Delete(item);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to clear unfinished directory: {ex}");
            }
        }
    }

    private void SetSongTime(float songTime)
    {
        _atsc.SetField("_songTime", songTime);
    }
}