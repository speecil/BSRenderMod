using RenderMod.Render;
using RenderMod.Util;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;
using static CustomLevelLoader;

public class ReplayVideoRenderer : ILateDisposable, IAffinity, ILateTickable
{
    [Inject] private BeatmapLevel _beatmapLevel = null;
    [Inject] private BeatmapKey _beatmapKey;
    [Inject] private SiraLog _log = null;
    [Inject] private ReplayProgressUI _progressUI = null;
    [Inject] private readonly StandardLevelReturnToMenuController _returnToMenuController = null;
    [Inject] private readonly EffectManager _effectManager = null;

    private AudioTimeSyncController _atsc = null;
    private Camera _replayCamera = null;

    private RenderTexture _rt = null;
    private FFmpegPipe _pipe = null;

    private string _unfinishedPath = null;
    private string _finishedPath = null;
    private string _songPath = null;

    private const int BufferCount = 8;
    private byte[][] _frameBuffers = null;

    private Matrix4x4 _origProj = Matrix4x4.identity;
    private bool _hadTargetTex = false;
    private RenderTexture _origTarget = null;

    private int _w, _h, _fps = 0;

    private float _oldCaptureDeltaTime;

    private string StatusMessage = "";

    string Safe(string s) =>
    string.Concat(s.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));


    [AffinityPatch(typeof(AudioTimeSyncController), nameof(AudioTimeSyncController.StartSong))]
    [AffinityPostfix]
    public void Init()
    {
        _w = ReplayRenderSettings.Width;
        _h = ReplayRenderSettings.Height;
        _fps = Mathf.Max(1, ReplayRenderSettings.FPS);

        // directory creation / file naming
        var renderRoot = ReplayRenderSettings.RenderRoot;
        Directory.CreateDirectory(Path.Combine(renderRoot, "Unfinished"));
        Directory.CreateDirectory(Path.Combine(renderRoot, "Finished"));
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

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

        _unfinishedPath = Path.Combine(renderRoot, "Unfinished", $"replay_{_beatmapLevel.songName}-{_beatmapKey.difficulty}_{timestamp}_raw.h264");
        _finishedPath = Path.Combine(renderRoot, "Finished", $"replay_{_beatmapLevel.songName}-{_beatmapKey.difficulty}_{timestamp}.mp4");

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
    public void LateTick()
    {
        if (!_readyToRender)
        {
            return;
        }

        _progressUI.Show();
        AudioListener.volume = 0f;
        _ready = true;
    }

        }).WaitForCompletion();

        _progressUI.UpdateProgress(Mathf.Clamp01(_atsc.songTime / Mathf.Max(0.0001f, _atsc.songLength)));
        frameIndex++;

        // ensure that beatleaders feature to keep the map from exiting doesnt break rendering
        if (_atsc.songTime + Mathf.Epsilon >= _atsc.songEndTime) // song ended or within epsilon
        {
            _returnToMenuController.ReturnToMenu();
        }
    }

    float oldCaptureDeltaTime;

    private void InitInit()
    {
        _replayCamera = _effectManager.RenderCameraEffect.FoundCamera;

        if (_replayCamera == null && ReplayRenderSettings.CameraType != "None")
        {
            _log.Error("Replay camera not found, cannot render video.");
            _returnToMenuController.ReturnToMenu(); // uh oh\
            return;
        }

        if (ReplayRenderSettings.CameraType == "None")
        {
            _replayCamera = Camera.main;
        }

        if (_replayCamera == null)
        {
            _log.Error("Replay camera not found, cannot render video.");
            _returnToMenuController.ReturnToMenu(); // uh oh again
            return;
        }

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
        if (ReplayRenderSettings.CameraType == "ReeCamera")
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            if (currentDomain.GetAssemblies().Any(a => a.GetName().Name.Contains("ReeCamera")))
            {
                _replayCamera.GetComponent<ReeCamera.MainCameraController>().SetTargetTexture(_rt);
            }
        }
        _origProj = _replayCamera.projectionMatrix;

        // get the delta time setup
        oldCaptureDeltaTime = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / _fps;

        _progressUI.UpdateProgress(
            Mathf.Clamp01(_atsc.songTime / Mathf.Max(0.0001f, _atsc.songLength))
        );

        // mute audio whilst rendering video
        AudioListener.volume = 0f;

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

        _pipe.RemuxRawH264ToMp4(videoPath, videoOnlyMp4);

        // audio pass (muxed from ogg)
        _pipe.AddAudioToMp4(videoOnlyMp4, audioPath, outputPath);

        _log.Notice($"Remuxing complete. Final MP4: {outputPath}");
    }

    public void LateDispose()
    {
        AsyncGPUReadback.WaitAllRequests();
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

        Time.captureDeltaTime = oldCaptureDeltaTime;
        _progressUI.UpdateProgress(1f, "Rendering complete!");
        _progressUI.Hide();
        AudioListener.volume = 1f;
        DingPlayer.shouldPlayDing = true;
        System.Diagnostics.Process.Start("explorer.exe", Path.GetDirectoryName(_finishedPath));
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

    // this fixes flying scores spawning in the same spot twice where 2 or more notes are cut in the same frame
    [AffinityPatch(typeof(FlyingScoreSpawner), nameof(FlyingScoreSpawner.SpawnFlyingScoreNextFrame))]
    [AffinityPrefix]
    public bool DisableFlyingScoreSpawnerBug(FlyingScoreSpawner __instance, IReadonlyCutScoreBuffer cutScoreBuffer, Color color)
    {
        __instance.SpawnFlyingScore(cutScoreBuffer, color);
        return false;
    }
}