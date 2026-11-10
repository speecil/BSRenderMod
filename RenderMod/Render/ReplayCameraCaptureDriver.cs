using System;
using System.Collections;
using UnityEngine;

namespace RenderMod.Render
{
    internal sealed class ReplayCameraCaptureDriver : MonoBehaviour
    {
        private static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();

        private Camera _camera;
        private double _frameDelta;
        private float _startTime;
        private bool _active;
        private bool _capturedThisFrame;
        private int _frameCount;
        private Coroutine _endOfFrameLoop;

        public event Action<int> FrameRendered;

        public void Begin(Camera camera, int fps)
        {
            _camera = camera;
            _frameDelta = fps > 0 ? 1d / fps : 0d;
            _startTime = Time.time - (float)(_frameDelta / 2d);
            _frameCount = 0;
            _active = true;
            _camera.enabled = false;
            _endOfFrameLoop = StartCoroutine(EndOfFrameLoop());
        }

        public void End()
        {
            _active = false;
            _capturedThisFrame = false;
            if (_endOfFrameLoop != null)
                StopCoroutine(_endOfFrameLoop);
            if (_camera != null)
                _camera.enabled = false;
        }

        private void Update()
        {
            if (!_active || _camera == null)
                return;

            _capturedThisFrame = Time.time - _startTime >= _frameDelta * _frameCount;
            _camera.enabled = _capturedThisFrame;
            if (_capturedThisFrame)
                _frameCount++;

            UnityAudioSyncHook.WaitForAudio();
        }

        private IEnumerator EndOfFrameLoop()
        {
            while (_active)
            {
                yield return EndOfFrame;
                if (_active && _capturedThisFrame)
                    FrameRendered?.Invoke(_frameCount);
            }
        }
    }
}
