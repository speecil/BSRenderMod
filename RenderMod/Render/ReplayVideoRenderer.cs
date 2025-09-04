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
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;
using static CustomLevelLoader;

public class ReplayVideoRenderer : IDisposable, IAffinity, ITickable
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

    private const int BufferCount = 8;
    private byte[][] _frameBuffers;

    private Matrix4x4 _origProj;
    private bool _hadTargetTex;
    private RenderTexture _origTarget;

    private int _w, _h, _fps;
    private bool _atscPrevEnabled;

    GameObject gcDisabler;

    [AffinityPatch(typeof(AudioTimeSyncController), nameof(AudioTimeSyncController.StartSong))]
    [AffinityPostfix]
    public void Init()
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

        var (encoder, presetArgs) = EncoderHelpers.BuildEncoderArgs();

        string ffArgs =
            $"-y " +
            $"-f rawvideo " +
            $"-pix_fmt rgb24 " +
            $"-s {_w}x{_h} " +
            $"-r {_fps} " +
            $"-i - " +
            $"{presetArgs} " +
            $"\"{_unfinishedPath}\"";

        _pipe = new FFmpegPipe(ffArgs);
        InitInit();
    }

    int frameIndex = 0;
    private bool _readyToRender = false;
    public void Tick()
    {
        if (!_readyToRender)
        {
            return;
        }
        //Quaternion smoothedReplayRot = Quaternion.Slerp(
        //            _lastCameraRot,
        //            _replayCamera.transform.rotation,
        //            Time.deltaTime * 5f
        //        );

        //Quaternion targetForward = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        //Quaternion finalRot = Quaternion.Slerp(
        //    smoothedReplayRot,
        //    targetForward,
        //    Time.deltaTime * 0.05f
        //);

        //_replayCamera.transform.rotation = finalRot;
        //_lastCameraRot = finalRot;

        int bufferIdx = frameIndex % BufferCount;

        int capturedIndex = frameIndex;
        AsyncGPUReadback.Request(_rt, 0, TextureFormat.RGB24, req =>
        {
            if (!req.hasError)
            {
                var data = req.GetData<byte>();
                FlipFrame(data, _frameBuffers[bufferIdx], _w, _h);
                _pipe.WriteFrame(_frameBuffers[bufferIdx]);
            }
            else
            {
                _log.Warn("GPU readback failed for a frame.");
            }

        }).WaitForCompletion();

        _progressUI.UpdateProgress(Mathf.Clamp01(_atsc.songTime / Mathf.Max(0.0001f, _atsc.songLength)));
        frameIndex++;
    }

    float oldCaptureDeltaTime;

    private Quaternion _lastCameraRot;
    private void InitInit()
    {
        _replayCamera = Resources.FindObjectsOfTypeAll<Camera>().FirstOrDefault(c => c.name == "ReplayerViewCamera" || c.name == "RecorderCamera");
        // get the replay camera (bl names theirs, ss doesnt and uses main)

        if (_replayCamera == null || !_replayCamera.gameObject.activeInHierarchy)
        {
            _log.Error("Replay camera not found or not active.");
            _replayCamera = Camera.main; // fallback to main camera
        }

        if (_replayCamera == null)
        { _log.Error("Replay camera not found."); return; }

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

        // get the delta time setup
        oldCaptureDeltaTime = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / _fps;

        _progressUI.Show();

        _readyToRender = true;
    }

    private unsafe void FlipFrame(NativeArray<byte> src, byte[] dst, int width, int height)
    {
        byte* srcPtr = (byte*)src.GetUnsafeReadOnlyPtr();

        for (int y = 0; y < height; y++)
        {
            int srcRow = y * width * 3;
            int dstRow = (height - 1 - y) * width * 3;

            fixed (byte* dstPtr = &dst[dstRow])
            {
                Buffer.MemoryCopy(srcPtr + srcRow, dstPtr, width * 3, width * 3);
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
        _readyToRender = false;
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

        Time.captureDeltaTime = oldCaptureDeltaTime;
        _progressUI.UpdateProgress(1f, "Rendering complete!");
        _progressUI.Hide();
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

    [AffinityPatch(typeof(FlyingScoreSpawner), nameof(FlyingScoreSpawner.SpawnFlyingScoreNextFrame))]
    [AffinityPrefix]
    public bool DisableFlyingScoreSpawning(FlyingScoreSpawner __instance, IReadonlyCutScoreBuffer cutScoreBuffer, Color color)
    {
        __instance.SpawnFlyingScore(cutScoreBuffer, color);
        return false;
    }
}