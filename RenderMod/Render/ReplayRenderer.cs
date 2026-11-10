using HarmonyLib;
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

namespace RenderMod.Render
{
    // 1/fps timestep
    // UnityAudioSyncHook keeps the audio in sync
    internal class ReplayRenderer : IAffinity, ILateTickable, ILateDisposable
    {
        [Inject] private readonly BeatmapLevel _beatmapLevel = null;
        [Inject] private readonly BeatmapKey _beatmapKey = default;
        [Inject] private readonly SiraLog _log = null;
        [Inject] private readonly ReplayProgressUI _progressUI = null;
        [Inject] private readonly StandardLevelReturnToMenuController _returnToMenuController = null;
        [Inject] private readonly EffectManager _effectManager = null;

        private AudioTimeSyncController _atsc;
        private Camera _camera;
        private CameraState _originalCameraState;
        private ReplayCameraCaptureDriver _cameraDriver;
        private RenderTexture _rt;
        private FFmpegPipe _pipe;
        private byte[] _frameBuffer;
        private ReplayAudioCapture _audioCapture;
        private GameObject _createdListener;

        private string _videoPath;
        private string _audioPath;
        private int _w, _h, _fps;
        private float _oldCaptureDeltaTime;
        private int _audioResyncCount;
        private bool _ready;

        [AffinityPatch(typeof(AudioTimeSyncController), nameof(AudioTimeSyncController.StartSong))]
        [AffinityPostfix]
        private void Init()
        {
            if (!RenderManager.RenderRequested || _ready)
                return;

            _atsc = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().FirstOrDefault();
            _camera = ReplayRenderSettings.CameraType == "None"
                ? Camera.main
                : _effectManager.RenderCameraEffect.FoundCamera;

            if (_atsc == null || _camera == null)
            {
                _log.Error(_camera == null ? "Replay camera not found." : "AudioTimeSyncController not found.");
                RenderManager.CancelRender();
                _returnToMenuController.ReturnToMenu();
                return;
            }

            _w = ReplayRenderSettings.Width;
            _h = ReplayRenderSettings.Height;
            _fps = Mathf.Max(1, ReplayRenderSettings.FPS);

            string dir = Path.Combine(ReplayRenderSettings.RenderRoot, "Unfinished");
            Directory.CreateDirectory(dir);
            string baseName = string.Concat(
                $"replay_{_beatmapLevel.songName}-{_beatmapKey.difficulty}_{DateTime.Now:yyyyMMdd_HHmmss}"
                    .Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
            _videoPath = Path.Combine(dir, $"{baseName}_video.{FFmpegPipe.RawFormat}");
            _audioPath = Path.Combine(dir, $"{baseName}_audio.wav");

            var (_, encoderArgs) = EncoderHelpers.BuildEncoderArgs();
            string ffArgs = $"-y -f rawvideo -pix_fmt rgb24 -s {_w}x{_h} -r {_fps} -i - {encoderArgs} \"{_videoPath}\"";
            _pipe = new FFmpegPipe(ffArgs);
            _frameBuffer = new byte[_w * _h * 3];
            _log.Notice($"FFmpeg started: {ffArgs}");

            SetupCamera();

            _oldCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / _fps;

            SetupAudio();
            Application.quitting += LateDispose;
            _progressUI.Show();
            _ready = true;
        }

        private void SetupCamera()
        {
            _rt = new RenderTexture(_w, _h, 24, RenderTextureFormat.ARGB32)
            {
                useMipMap = false,
                autoGenerateMips = false,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            _rt.Create();

            _originalCameraState = new CameraState(_camera);
            _camera.targetTexture = _rt;
            _camera.stereoTargetEye = StereoTargetEyeMask.None;
            _camera.aspect = _w / (float)_h;
            _camera.pixelRect = new Rect(0f, 0f, _w, _h);
            _camera.rect = new Rect(0f, 0f, 1f, 1f);

            // disable frustum culling
            _camera.cullingMatrix =
                Matrix4x4.Ortho(-99999f, 99999f, -99999f, 99999f, 0.001f, 99999f) *
                Matrix4x4.Translate(Vector3.forward * -49999.5f) *
                _camera.worldToCameraMatrix;

            _cameraDriver = _camera.gameObject.AddComponent<ReplayCameraCaptureDriver>();
            _cameraDriver.FrameRendered += OnFrameRendered;
            _cameraDriver.Begin(_camera, _fps);
        }

        private void SetupAudio()
        {
            var listener = Resources.FindObjectsOfTypeAll<AudioListener>().FirstOrDefault(l => l != null && l.isActiveAndEnabled);
            if (listener == null)
            {
                _log.Warn("No active AudioListener was found. Created a fallback listener for audio capture.");
                _createdListener = new GameObject("ReplayAudioCapture");
                listener = _createdListener.AddComponent<AudioListener>();
            }

            _audioCapture = listener.gameObject.AddComponent<ReplayAudioCapture>();
            _audioCapture.OpenFile(_audioPath);

            try
            {
                UnityAudioSyncHook.SetSyncTimes(true);
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to start audio sync, audio will be captured unsynced: {ex}");
            }
        }

        private void OnFrameRendered(int frameCount)
        {
            if (!_ready)
                return;

            AsyncGPUReadback.Request(_rt, 0, TextureFormat.RGB24, req =>
            {
                if (req.hasError)
                {
                    _log.Warn("GPU readback failed");
                    return;
                }
                FlipFrame(req.GetData<byte>(), _frameBuffer, _w, _h);
                _pipe.WriteFrame(_frameBuffer);
            }).WaitForCompletion();
        }

        private static unsafe void FlipFrame(NativeArray<byte> src, byte[] dst, int width, int height)
        {
            byte* srcPtr = (byte*)src.GetUnsafeReadOnlyPtr();
            int stride = width * 3;
            fixed (byte* dstPtr = dst)
            {
                for (int y = 0; y < height; y++)
                    Buffer.MemoryCopy(srcPtr + y * stride, dstPtr + (height - 1 - y) * stride, stride, stride);
            }
        }

        public void LateTick()
        {
            if (!_ready)
                return;

            UnityAudioSyncHook.UpdateGameTime();
            _progressUI.UpdateProgress(_atsc.songTime / Mathf.Max(0.0001f, _atsc.songLength));

            if (_atsc.songTime + Mathf.Epsilon >= _atsc.songEndTime)
                _returnToMenuController.ReturnToMenu();
        }

        public void LateDispose()
        {
            if (!_ready)
            {
                RenderManager.CancelRender();
                return;
            }
            _ready = false;
            Application.quitting -= LateDispose;

            if (UnityAudioSyncHook.TimeoutCount > 0)
                _log.Warn($"Audio thread timed out waiting for the main thread {UnityAudioSyncHook.TimeoutCount} time(s), audio may have run ahead of video there.");
            if (_audioResyncCount > 0)
                _log.Warn($"Song audio drifted and was re-seeked {_audioResyncCount} time(s).");
            UnityAudioSyncHook.SetSyncTimes(false);

            AsyncGPUReadback.WaitAllRequests();
            Time.captureDeltaTime = _oldCaptureDeltaTime;
            _pipe.Close();

            _log.Notice($"Captured {_audioCapture.CapturedSeconds:0.000}s of audio");
            _audioCapture.Close();

            RenderManager.FinishRender(_videoPath, _audioPath);

            if (_audioCapture != null)
                UnityEngine.Object.Destroy(_audioCapture);
            if (_createdListener != null)
                UnityEngine.Object.Destroy(_createdListener);

            _cameraDriver.FrameRendered -= OnFrameRendered;
            if (_cameraDriver != null)
            {
                _cameraDriver.End();
                UnityEngine.Object.Destroy(_cameraDriver);
            }
            if (_camera != null)
                _originalCameraState.Restore(_camera);

            _rt.Release();
            UnityEngine.Object.Destroy(_rt);
            _progressUI.Hide();
        }

        private const float AudioResyncThreshold = 0.1f;

        private static readonly AccessTools.FieldRef<AudioTimeSyncController, AudioTimeSyncController.State> AtscState =
            AccessTools.FieldRefAccess<AudioTimeSyncController, AudioTimeSyncController.State>("_state");
        private static readonly AccessTools.FieldRef<AudioTimeSyncController, float> AtscSongTime =
            AccessTools.FieldRefAccess<AudioTimeSyncController, float>("_songTime");
        private static readonly AccessTools.FieldRef<AudioTimeSyncController, float> AtscTimeScale =
            AccessTools.FieldRefAccess<AudioTimeSyncController, float>("_timeScale");
        private static readonly AccessTools.FieldRef<AudioTimeSyncController, float> AtscLastFrameDelta =
            AccessTools.FieldRefAccess<AudioTimeSyncController, float>("_lastFrameDeltaSongTime");
        private static readonly AccessTools.FieldRef<AudioTimeSyncController, AudioSource> AtscAudioSource =
            AccessTools.FieldRefAccess<AudioTimeSyncController, AudioSource>("_audioSource");
        private static readonly AccessTools.FieldRef<AudioTimeSyncController, double> AtscDspTimeOffset =
            AccessTools.FieldRefAccess<AudioTimeSyncController, double>("_dspTimeOffset");
        private static readonly AccessTools.FieldRef<AudioTimeSyncController, bool> AtscIsReady =
            AccessTools.FieldRefAccess<AudioTimeSyncController, bool>("_isReady");

        // stops base game capture mode seeking the song every frame (UnityAudioSyncHook keeps it in sync)
        [AffinityPatch(typeof(AudioTimeSyncController), "Update")]
        [AffinityPrefix]
        private bool AudioTimeSyncControllerUpdatePrefix(AudioTimeSyncController __instance)
        {
            if (!_ready || !UnityAudioSyncHook.SyncTimes || Time.captureFramerate == 0)
                return true;

            var audioSource = AtscAudioSource(__instance);
            if (AtscState(__instance) != AudioTimeSyncController.State.Playing || audioSource == null || audioSource.clip == null)
                return true;

            float delta = Time.deltaTime * AtscTimeScale(__instance);
            AtscLastFrameDelta(__instance) = delta;

            ref float songTime = ref AtscSongTime(__instance);
            songTime += delta;

            float clipLength = audioSource.clip.length;
            if (!audioSource.loop)
                songTime = Mathf.Min(songTime, clipLength - 0.01f);

            float target = audioSource.loop ? songTime % clipLength : songTime;
            if (Mathf.Abs(audioSource.time - target) > AudioResyncThreshold)
            {
                audioSource.time = target;
                _audioResyncCount++;
            }

            AtscDspTimeOffset(__instance) = AudioSettings.dspTime - songTime;
            AtscIsReady(__instance) = true;
            return false;
        }

        private readonly struct CameraState
        {
            private readonly RenderTexture _target;
            private readonly StereoTargetEyeMask _stereoTargetEye;
            private readonly Rect _pixelRect, _rect;
            private readonly bool _enabled;

            public CameraState(Camera camera)
            {
                _target = camera.targetTexture;
                _stereoTargetEye = camera.stereoTargetEye;
                _pixelRect = camera.pixelRect;
                _rect = camera.rect;
                _enabled = camera.enabled;
            }

            public void Restore(Camera camera)
            {
                camera.targetTexture = _target;
                camera.stereoTargetEye = _stereoTargetEye;
                camera.rect = _rect;
                camera.pixelRect = _pixelRect;
                camera.ResetAspect();
                camera.ResetCullingMatrix();
                camera.enabled = _enabled;
            }
        }
    }
}
