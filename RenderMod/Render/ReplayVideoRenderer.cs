using RenderMod.Render;
using RenderMod.Util;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using System;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;
using static RenderMod.Render.RenderManager;

public class ReplayVideoRenderer : ILateDisposable, IAffinity, ILateTickable
{
    [Inject] private BeatmapLevel _beatmapLevel = null;
    [Inject] private BeatmapKey _beatmapKey;
    [Inject] private SiraLog _log = null;
    [Inject] private ReplayProgressUI _progressUI = null;
    [Inject] private readonly StandardLevelReturnToMenuController _returnToMenuController = null;
    [Inject] private readonly EffectManager _effectManager = null;

    private AudioTimeSyncController _atsc;
    private Camera _replayCamera;

    private RenderTexture _rt;
    private FFmpegPipe _pipe;

    private string _unfinishedPath;

    private const int BufferCount = 8;
    private byte[][] _frameBuffers;

    private Matrix4x4 _origProj;
    private bool _hadTargetTex;
    private RenderTexture _origTarget;

    private int _w, _h, _fps;
    private int _frameIndex;
    private bool _ready;

    private float _oldCaptureDeltaTime;

    string Safe(string s) =>
    string.Concat(s.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));


    [AffinityPatch(typeof(AudioTimeSyncController), nameof(AudioTimeSyncController.StartSong))]
    [AffinityPostfix]
    public void Init()
    {
        if (currentState != RenderState.Video)
        {
            _log.Info("RenderManager state is not Video, skipping ReplayVideoRenderer init.");
            return;
        }
        _w = ReplayRenderSettings.Width;
        _h = ReplayRenderSettings.Height;
        _fps = Mathf.Max(1, ReplayRenderSettings.FPS);

        _atsc = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().FirstOrDefault();
        if (_atsc == null || _beatmapLevel == null)
        {
            _log.Error("Missing AudioTimeSyncController or BeatmapLevel");
            return;
        }

        var renderRoot = ReplayRenderSettings.RenderRoot;
        Directory.CreateDirectory(Path.Combine(renderRoot, "Unfinished"));
        
        string codec = ReplayRenderSettings.VideoCodec;
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _unfinishedPath = Path.Combine(
            renderRoot,
            "Unfinished",
            Safe($"replay_{_beatmapLevel.songName}-{_beatmapKey.difficulty}_{timestamp}_video.{codec}")
        );

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
        _log.Notice($"FFmpeg started: {ffArgs}");

        SetupCamera();
        SetupTiming();

        _progressUI.Show();
        AudioListener.volume = 0f;
        _ready = true;
    }


    private void SetupCamera()
    {
        _replayCamera = _effectManager.RenderCameraEffect.FoundCamera;

        if (_replayCamera == null && ReplayRenderSettings.CameraType == "None")
            _replayCamera = Camera.main;

        if (_replayCamera == null)
        {
            _log.Error("Replay camera not found.");
            _returnToMenuController.ReturnToMenu();
            return;
        }

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
        _origProj = _replayCamera.projectionMatrix;

        _replayCamera.targetTexture = _rt;
        _replayCamera.enabled = true;
    }

    private void SetupTiming()
    {
        _oldCaptureDeltaTime = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / _fps;
    }


    public void LateTick()
    {
        if (!_ready)
            return;

        int bufferIdx = _frameIndex % BufferCount;

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
                _log.Warn("GPU readback failed");
            }
        }).WaitForCompletion();

        _progressUI.UpdateProgress(
            Mathf.Clamp01(_atsc.songTime / Mathf.Max(0.0001f, _atsc.songLength))
        );

        _frameIndex++;

        if (_atsc.songTime + Mathf.Epsilon >= _atsc.songEndTime)
            _returnToMenuController.ReturnToMenu();
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

    public void LateDispose()
    {
        AudioListener.volume = 1f;
        AsyncGPUReadback.WaitAllRequests();
        _ready = false;

        Time.captureDeltaTime = _oldCaptureDeltaTime;

        _pipe?.Close();

        if (_replayCamera != null)
        {
            _replayCamera.projectionMatrix = _origProj;
            _replayCamera.targetTexture = _hadTargetTex ? _origTarget : null;
        }

        if (_rt != null)
        {
            _rt.Release();
            UnityEngine.Object.Destroy(_rt);
            _rt = null;
        }

        _progressUI.UpdateProgress(1f, "Video render complete");
        _progressUI.Hide();

        _log.Notice($"Finished writing {_unfinishedPath}");
    }
}
