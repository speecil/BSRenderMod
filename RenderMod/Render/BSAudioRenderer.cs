using System;
using System.IO;
using System.Threading;
using UnityEngine;

public class BSAudioRenderer : MonoBehaviour
{
    private Thread writeThread;
    private FileStream fileStream;
    private BinaryWriter writer;
    private bool capturing;
    private int sampleRate;
    private int channels;
    private int totalSamples;
    private float[] mixBuffer;

    private void Awake()
    {
        AudioSettings.GetDSPBufferSize(out int dspBufferLength, out _);
        sampleRate = AudioSettings.outputSampleRate;
        channels = 2;
        mixBuffer = new float[dspBufferLength * channels];
    }

    public void StartCapture(string path)
    {
        if (capturing) return;

        fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        writer = new BinaryWriter(fileStream);
        WriteWavHeader();

        capturing = true;
        totalSamples = 0;

        writeThread = new Thread(CaptureLoop) { IsBackground = true };
        writeThread.Start();

        Debug.Log($"[BSAudioRenderer] Started capturing to {path}");
    }

    public void StopCapture()
    {
        capturing = false;
        writeThread?.Join();

        fileStream.Seek(0, SeekOrigin.Begin);
        WriteWavHeader();
        writer?.Close();
        fileStream?.Close();

        Debug.Log("[BSAudioRenderer] Capture stopped and file finalized.");
    }

    private void CaptureLoop()
    {
        float[] samplesL = new float[1024];
        float[] samplesR = new float[1024];
        short[] pcmBuffer = new short[2048];

        while (capturing)
        {
            AudioListener.GetOutputData(samplesL, 0);
            AudioListener.GetOutputData(samplesR, 1);

            for (int i = 0; i < samplesL.Length; i++)
            {
                float left = Mathf.Clamp(samplesL[i], -1f, 1f);
                float right = Mathf.Clamp(samplesR[i], -1f, 1f);
                pcmBuffer[i * 2] = (short)(left * short.MaxValue);
                pcmBuffer[i * 2 + 1] = (short)(right * short.MaxValue);
            }

            lock (writer)
            {
                byte[] bytes = new byte[pcmBuffer.Length * 2];
                Buffer.BlockCopy(pcmBuffer, 0, bytes, 0, bytes.Length);
                writer.Write(bytes);
                totalSamples += pcmBuffer.Length;
            }

            Thread.Yield();
        }
    }

    private void WriteWavHeader()
    {
        if (writer == null) return;

        int byteRate = sampleRate * channels * 2;
        int subChunk2Size = totalSamples * 2;
        int chunkSize = 36 + subChunk2Size;

        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(chunkSize);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)(channels * 2));
        writer.Write((short)16); // bits per sample
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(subChunk2Size);
    }
}
