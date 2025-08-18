using System;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

public class FFmpegPipe : IDisposable
{
    private Process ffmpeg;
    private Stream ffmpegInput;
    private bool isClosed = false;

    public FFmpegPipe(string args)
    {
        ffmpeg = new Process();
        ffmpeg.StartInfo.FileName = "ffmpeg"; // TODO: make this configurable OR find ffmpeg manually (i should just make it a requirement)
        ffmpeg.StartInfo.Arguments = args;
        ffmpeg.StartInfo.UseShellExecute = false;
        ffmpeg.StartInfo.RedirectStandardInput = true;
        ffmpeg.StartInfo.RedirectStandardError = true;
        ffmpeg.StartInfo.RedirectStandardOutput = true;
        ffmpeg.StartInfo.CreateNoWindow = true;

        //ffmpeg.ErrorDataReceived += (sender, e) =>
        //{
        //    if (!string.IsNullOrEmpty(e.Data))
        //        Debug.LogWarning($"[FFmpeg] {e.Data}");
        //};

        //ffmpeg.OutputDataReceived += (sender, e) =>
        //{
        //    if (!string.IsNullOrEmpty(e.Data))
        //        Debug.Log($"[FFmpeg] {e.Data}");
        //};

        try
        {
            ffmpeg.Start();
            ffmpeg.BeginErrorReadLine();
            ffmpeg.BeginOutputReadLine();
            ffmpegInput = ffmpeg.StandardInput.BaseStream;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to start FFmpeg: {ex}");
        }
    }

    public void WriteFrame(byte[] frame)
    {
        if (isClosed) return;

        try
        {
            ffmpegInput?.Write(frame, 0, frame.Length);
        }
        catch (ObjectDisposedException)
        {
            // kill me now
        }
        catch (Exception ex)
        {
            Debug.LogError($"FFmpeg WriteFrame failed: {ex.Message}");
            Close();
        }
    }


    public void Close()
    {
        if (isClosed) return;
        isClosed = true;

        try
        {
            ffmpegInput?.Flush();
            ffmpegInput?.Close();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error closing FFmpeg input: {ex.Message}");
        }

        try
        {
            if (!ffmpeg.HasExited)
                ffmpeg.WaitForExit();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error waiting for FFmpeg to exit: {ex.Message}");
        }

        ffmpeg.Dispose();
    }

    // currently not being binded, but leaving the zenject interface for consistency / just in case i do want to bind this later
    public void Dispose()
    {
        Close();
    }
}
