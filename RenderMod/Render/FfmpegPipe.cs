using IPA.Utilities;
using ModestTree;
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
        ffmpeg.StartInfo.FileName = Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe");
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


    public void RemuxRawH264ToMp4(string rawH264Path, string outputMp4Path)
    {
        var ffm = new Process();
        ffm.StartInfo.FileName = Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe");
        ffm.StartInfo.Arguments =
            $"-y -i \"{rawH264Path}\" " +
            "-c:v copy " +
            $"\"{outputMp4Path}\"";
        ffm.StartInfo.UseShellExecute = false;
        ffm.StartInfo.CreateNoWindow = true;
        ffm.Start();
        ffm.WaitForExit();
    }

    public void AddAudioToMp4(string videoMp4Path, string audioPath, string finalMp4Path)
    {
        if (!File.Exists(audioPath))
        {
            return;
        }

        var ffm = new Process();
        ffm.StartInfo.FileName = Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe");
        ffm.StartInfo.Arguments =
            $"-y -i \"{videoMp4Path}\" -i \"{audioPath}\" -c:v copy -c:a aac -shortest \"{finalMp4Path}\"";
        ffm.StartInfo.UseShellExecute = false;
        ffm.StartInfo.CreateNoWindow = true;
        ffm.Start();
        ffm.WaitForExit();

        File.Delete(videoMp4Path);
    }

    // currently not being binded, but leaving the zenject interface for consistency / just in case i do want to bind this later
    public void Dispose()
    {
        Close();
    }
}
