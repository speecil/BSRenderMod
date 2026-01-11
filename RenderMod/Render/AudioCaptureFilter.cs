using System;
using System.Linq;
using UnityEngine;

namespace RenderMod.Render
{
    public class AudioCaptureFilter : MonoBehaviour
    {
        public Action<float[], int> OnAudioData;

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (data.All(d => d == 0f))
                return;
            if (OnAudioData != null && data.Length > 0)
            {
                float[] dataCopy = new float[data.Length];
                Array.Copy(data, dataCopy, data.Length);
                Array.Clear(data, 0, data.Length);
                OnAudioData.Invoke(dataCopy, channels);
            }
        }
    }
}
