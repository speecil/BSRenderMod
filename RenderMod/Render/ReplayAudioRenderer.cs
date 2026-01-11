using RenderMod.Util;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using UnityEngine;
using Zenject;
using static RenderMod.Render.RenderManager;

namespace RenderMod.Render
{
    public class ReplayAudioRenderer : IAffinity, ILateDisposable
    {
        [Inject] private readonly SiraLog _log = null;
        [Inject] private readonly EffectManager _effectManager = null;
        [Inject] private readonly ReplayProgressUI _progressUI = null;

        private AudioCaptureFilter _filter;
        private AudioListener _listener;

        private FileStream _fileStream;
        private BinaryWriter _writer;

        private readonly ConcurrentQueue<float[]> _audioQueue = new ConcurrentQueue<float[]>();
        private bool _recording;

        private int _channels;
        private int _sampleRate;
        private long _totalSamples;

        private long _dataChunkSizePos;

        private string _audioPath;

        [AffinityPatch(typeof(AudioTimeSyncController), nameof(AudioTimeSyncController.StartSong))]
        public void Init()
        {
            if (currentState != RenderState.Audio)
                return;

            _sampleRate = AudioSettings.outputSampleRate;

            var root = ReplayRenderSettings.RenderRoot;
            Directory.CreateDirectory(Path.Combine(root, "Unfinished"));

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _audioPath = Path.Combine(
                root,
                "Unfinished",
                $"replay_audio_{timestamp}.wav"
            );

            OpenWav();
            AttachListener();

            _recording = true;
            Application.quitting += OnQuit;

            _progressUI.Show();

            _progressUI.UpdateProgress(1f, "Rendering Audio...");

            _log.Notice($"Recording audio to {_audioPath}");
        }

        private void AttachListener()
        {
            var listenerObj = new GameObject("ReplayAudioListener");
            _listener = listenerObj.AddComponent<AudioListener>();
            _filter = listenerObj.AddComponent<AudioCaptureFilter>();
            _filter.OnAudioData += OnAudioData;
        }

        private void OnAudioData(float[] data, int channels)
        {
            if (!_recording || data == null || data.Length == 0)
                return;

            if (_channels == 0)
                _channels = channels;

            _audioQueue.Enqueue((float[])data.Clone());
        }

        private void Update()
        {
            DrainQueue();
        }

        private void DrainQueue()
        {
            if (_writer == null)
                return;

            while (_audioQueue.TryDequeue(out var block))
            {
                foreach (var sample in block)
                {
                    float clamped = Mathf.Clamp(sample, -1f, 1f);
                    short pcm = (short)(clamped * short.MaxValue);
                    _writer.Write(pcm);
                }

                _totalSamples += block.Length;
            }
        }

        private void OpenWav()
        {
            _fileStream = new FileStream(_audioPath, FileMode.Create, FileAccess.Write);
            _writer = new BinaryWriter(_fileStream);

            _writer.Write(new[] { 'R', 'I', 'F', 'F' });
            _writer.Write(0);
            _writer.Write(new[] { 'W', 'A', 'V', 'E' });

            _writer.Write(new[] { 'f', 'm', 't', ' ' });
            _writer.Write(16);
            _writer.Write((short)1);
            _writer.Write((short)2);
            _writer.Write(_sampleRate);
            _writer.Write(_sampleRate * 2 * 2);
            _writer.Write((short)(2 * 2));
            _writer.Write((short)16);

            _writer.Write(new[] { 'd', 'a', 't', 'a' });
            _dataChunkSizePos = _fileStream.Position;
            _writer.Write(0);
        }

        private void FinalizeWav()
        {
            DrainQueue();

            long fileSize = _fileStream.Position;

            _fileStream.Seek(_dataChunkSizePos, SeekOrigin.Begin);
            _writer.Write((int)(_totalSamples * sizeof(short)));

            _fileStream.Seek(22, SeekOrigin.Begin);
            _writer.Write((short)_channels);

            _fileStream.Seek(4, SeekOrigin.Begin);
            _writer.Write((int)(fileSize - 8));

            _fileStream.Seek(fileSize, SeekOrigin.Begin);
        }

        private void OnQuit()
        {
            LateDispose();
        }

        public void LateDispose()
        {
            if (!_recording)
                return;

            _progressUI.Hide();

            _recording = false;

            try
            {
                FinalizeWav();
                _writer?.Close();
                _fileStream?.Close();
            }
            catch (Exception ex)
            {
                _log.Error($"[AudioOnly] Failed to finalize WAV: {ex}");
            }

            if (_filter != null)
                _filter.OnAudioData -= OnAudioData;

            if (_listener != null)
                UnityEngine.Object.Destroy(_listener.gameObject);

            Application.quitting -= OnQuit;

            _log.Notice("[AudioOnly] Audio capture complete.");
        }
    }
}
