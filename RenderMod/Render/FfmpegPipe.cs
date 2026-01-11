using IPA.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

public class FFmpegPipe
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
            if (ffmpegInput != null)
            {
                ffmpegInput.Flush();
                ffmpegInput.Close();
                ffmpegInput.Dispose();
                ffmpegInput = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error closing FFmpeg input: {ex.Message}");
        }

        try
        {
            if (ffmpeg != null && !ffmpeg.HasExited)
            {
                ffmpeg.WaitForExit();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error waiting for FFmpeg to exit: {ex.Message}");
        }

        if (ffmpeg != null)
        {
            ffmpeg.Dispose();
            ffmpeg = null;
        }
    }


    public static void RemuxRawH264ToMp4(string rawH264Path, string outputMp4Path, int fps)
    {
        if (!File.Exists(rawH264Path))
        {
            Debug.LogError($"Raw H.264 file not found: {rawH264Path}");
            return;
        }

        var ffm = new Process();
        ffm.StartInfo.FileName = Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe");

        ffm.StartInfo.Arguments =
            $"-y -r {fps} -f h264 -i \"{rawH264Path}\" " +
            $"-c:v copy \"{outputMp4Path}\"";

        ffm.StartInfo.UseShellExecute = false;
        ffm.StartInfo.CreateNoWindow = true;
        ffm.StartInfo.RedirectStandardError = true;

        ffm.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Debug.Log($"[FFmpeg] {e.Data}");
        };

        ffm.Start();
        ffm.BeginErrorReadLine();
        ffm.WaitForExit();
    }

    public static void AddAudioToMp4(string videoMp4Path, string audioPath, string finalMp4Path, int audioBitrateKbps)
    {
        if (!File.Exists(videoMp4Path))
        {
            Debug.LogError($"Video MP4 not found: {videoMp4Path}");
            return;
        }

        if (!File.Exists(audioPath))
        {
            Debug.LogError($"Audio WAV not found: {audioPath}");
            return;
        }

        var ffm = new Process();
        ffm.StartInfo.FileName = Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe");

        ffm.StartInfo.Arguments =
            $"-y -i \"{videoMp4Path}\" -i \"{audioPath}\" " +
            $"-c:v copy -c:a aac -b:a {audioBitrateKbps}k -shortest \"{finalMp4Path}\"";

        ffm.StartInfo.UseShellExecute = false;
        ffm.StartInfo.CreateNoWindow = true;
        ffm.StartInfo.RedirectStandardError = true;

        ffm.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Debug.Log($"[FFmpeg] {e.Data}");
        };

        ffm.Start();
        ffm.BeginErrorReadLine();
        ffm.WaitForExit();

        try
        {
            File.Delete(videoMp4Path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to delete intermediate MP4: {ex}");
        }
    }
}
