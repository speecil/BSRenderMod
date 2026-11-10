using System;
using System.IO;
using UnityEngine;

namespace RenderMod.Render
{
    internal class ReplayAudioCapture : MonoBehaviour
    {
        private const int HeaderSize = 44;

        private readonly object _lock = new object();
        private BinaryWriter _writer;
        private int _sampleRate;
        private int _channels;
        private int _dataBytes;

        private float _peak = 1f;
        private float _peakDecay;
        private int _channel;

        public float CapturedSeconds => _channels == 0 ? 0f : _dataBytes / (2f * _channels * _sampleRate);

        public void OpenFile(string path)
        {
            _sampleRate = AudioSettings.outputSampleRate;
            _peakDecay = 1f / _sampleRate;
            var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            _writer = new BinaryWriter(new BufferedStream(stream, 64 * 1024));
            _writer.Write(new byte[HeaderSize]); // filled in on Close
        }

        private void Update()
        {
            UnityAudioSyncHook.UpdateGameTime();
        }

        // runs on the audio thread
        private void OnAudioFilterRead(float[] data, int channels)
        {
            lock (_lock)
            {
                if (_writer == null || channels <= 0)
                    return;
                _channels = channels;

                foreach (float sample in data)
                {
                    float level = Math.Abs(sample);
                    if (level > _peak)
                        _peak = level;
                    else if (_channel == channels - 1)
                        _peak = Math.Max(_peak - _peakDecay, 1f);
                    _channel = (_channel + 1) % channels;

                    _writer.Write((short)(sample / _peak * short.MaxValue));
                }
                _dataBytes += data.Length * sizeof(short);
            }
        }

        private void OnDestroy() => Close();

        public void Close()
        {
            lock (_lock)
            {
                if (_writer == null)
                    return;

                int channels = _channels > 0 ? _channels : 2;
                _writer.Seek(0, SeekOrigin.Begin);
                _writer.Write(0x46464952);                 // "RIFF"
                _writer.Write(_dataBytes + HeaderSize - 8);
                _writer.Write(0x45564157);                 // "WAVE"
                _writer.Write(0x20746D66);                 // "fmt "
                _writer.Write(16);
                _writer.Write((short)1);                   // PCM
                _writer.Write((short)channels);
                _writer.Write(_sampleRate);
                _writer.Write(_sampleRate * channels * 2); // byte rate
                _writer.Write((short)(channels * 2));      // block align
                _writer.Write((short)16);                  // bits per sample
                _writer.Write(0x61746164);                 // "data"
                _writer.Write(_dataBytes);
                _writer.Close();
                _writer = null;
            }
        }
    }
}
