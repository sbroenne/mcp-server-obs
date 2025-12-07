using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Sbroenne.ObsMcp.McpServer.Tools;

/// <summary>
/// Actions available for the obs_recording tool
/// </summary>
public enum RecordingAction
{
    /// <summary>Start recording</summary>
    Start,
    /// <summary>Stop recording and save file</summary>
    Stop,
    /// <summary>Pause recording</summary>
    Pause,
    /// <summary>Resume paused recording</summary>
    Resume,
    /// <summary>Get recording status</summary>
    GetStatus,
    /// <summary>Get recording settings (format, quality, encoder, path)</summary>
    GetSettings,
    /// <summary>Set recording format (mp4, mkv, etc.)</summary>
    SetFormat,
    /// <summary>Set recording quality preset</summary>
    SetQuality,
    /// <summary>Set recording output directory</summary>
    SetPath,
    /// <summary>Get current recording output directory</summary>
    GetPath
}

/// <summary>
/// OBS recording control tool
/// </summary>
[McpServerToolType]
public static partial class ObsRecordingTool
{
    /// <summary>
    /// Control OBS recording.
    /// 
    /// Actions:
    /// - Start: Start recording. Use 'path' parameter to set output directory (e.g., path='C:/Videos')
    /// - Stop: Stop recording and save the file
    /// - Pause: Pause the current recording
    /// - Resume: Resume a paused recording
    /// - GetStatus: Get recording status (active, paused, timecode)
    /// - GetSettings: Get recording format, quality, encoder, and output path
    /// - SetFormat: Set recording format (mp4, mkv, flv, mov, ts)
    /// - SetQuality: Set recording quality (Stream, Small, HQ, Lossless)
    /// - SetPath: Set recording output directory (e.g., path='C:/Videos')
    /// - GetPath: Get current recording output directory
    /// 
    /// OUTPUT PATH: Use 'path' parameter with Start or SetPath to control where recordings are saved.
    /// Example: obs_recording(action: Start, path: 'D:/MyRecordings')
    /// 
    /// AUDIO: By default, audio is MUTED when starting. Set muteAudio=false to include audio.
    /// 
    /// IMPORTANT: Add a capture source first using obs_source to avoid BLACK SCREEN recordings.
    /// </summary>
    /// <param name="action">Action to perform: Start, Stop, Pause, Resume, GetStatus, GetSettings, SetFormat, SetQuality, SetPath, GetPath</param>
    /// <param name="format">Recording format for SetFormat action: mp4 (recommended), mkv, flv, mov, ts</param>
    /// <param name="quality">Quality preset for SetQuality action: Stream, Small, HQ (recommended), Lossless</param>
    /// <param name="path">Output directory path for Start or SetPath actions. Example: 'C:/Videos' or 'D:/Recordings'</param>
    /// <param name="muteAudio">Mute audio when starting recording. Default: true (audio muted). Set to false to record with audio.</param>
    [McpServerTool(Name = "obs_recording")]
    public static partial string Recording(
        RecordingAction action,
        [DefaultValue(null)] string? format,
        [DefaultValue(null)] string? quality,
        [DefaultValue(null)] string? path,
        [DefaultValue(true)] bool muteAudio)
    {
        try
        {
            return action switch
            {
                RecordingAction.Start => DoStart(muteAudio, path),
                RecordingAction.Stop => DoStop(),
                RecordingAction.Pause => DoPause(),
                RecordingAction.Resume => DoResume(),
                RecordingAction.GetStatus => DoGetStatus(),
                RecordingAction.GetSettings => DoGetSettings(),
                RecordingAction.SetFormat => DoSetFormat(format),
                RecordingAction.SetQuality => DoSetQuality(quality),
                RecordingAction.SetPath => DoSetPath(path),
                RecordingAction.GetPath => DoGetPath(),
                _ => $"Error: Unknown action '{action}'"
            };
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static string DoStart(bool muteAudio, string? path)
    {
        var client = ObsConnectionTool.GetClient();
        var audioStatus = "";
        var pathStatus = "";

        // Set recording path if specified
        if (!string.IsNullOrEmpty(path))
        {
            client.SetRecordingDirectory(path);
            pathStatus = $" Output: {path}";
        }

        if (muteAudio)
        {
            // Mute all audio inputs before recording
            var inputs = client.GetSpecialInputs();
            foreach (var input in inputs)
            {
                client.SetInputMute(input.Name, true);
            }
            audioStatus = inputs.Count > 0 ? " Audio muted." : "";
        }

        client.StartRecording();
        return $"Recording started.{audioStatus}{pathStatus} Use obs_recording with action=Stop when finished.";
    }

    private static string DoStop()
    {
        var client = ObsConnectionTool.GetClient();
        var outputPath = client.StopRecording();
        return $"Recording stopped and saved to: {outputPath}";
    }

    private static string DoPause()
    {
        var client = ObsConnectionTool.GetClient();
        client.PauseRecording();
        return "Recording paused. Use obs_recording with action=Resume to continue.";
    }

    private static string DoResume()
    {
        var client = ObsConnectionTool.GetClient();
        client.ResumeRecording();
        return "Recording resumed";
    }

    private static string DoGetStatus()
    {
        var client = ObsConnectionTool.GetClient();
        var status = client.GetRecordingStatus();

        return $"Recording Status:\n" +
               $"Active: {status.IsRecording}\n" +
               $"Paused: {status.IsPaused}\n" +
               $"Timecode: {status.Timecode}";
    }

    private static string DoGetSettings()
    {
        var client = ObsConnectionTool.GetClient();
        var settings = client.GetRecordingSettings();
        var directory = client.GetRecordingDirectory();

        return $"Recording Settings:\n" +
               $"Format: {settings.Format}\n" +
               $"Quality: {settings.Quality}\n" +
               $"Encoder: {settings.Encoder}\n" +
               $"Path: {directory}";
    }

    private static string DoSetPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "Error: path parameter is required for SetPath action";
        }

        var client = ObsConnectionTool.GetClient();
        client.SetRecordingDirectory(path);
        return $"Recording output directory set to: {path}";
    }

    private static string DoGetPath()
    {
        var client = ObsConnectionTool.GetClient();
        var directory = client.GetRecordingDirectory();
        return $"Recording output directory: {directory}";
    }

    private static string DoSetFormat(string? format)
    {
        if (string.IsNullOrEmpty(format))
        {
            return "Error: format parameter is required for SetFormat action";
        }

        var validFormats = new[] { "mp4", "mkv", "flv", "mov", "ts" };
        if (!validFormats.Contains(format.ToLower()))
        {
            return $"Error: Invalid format. Valid formats: {string.Join(", ", validFormats)}";
        }

        var client = ObsConnectionTool.GetClient();
        client.SetRecordingFormat(format.ToLower());
        return $"Recording format set to {format}";
    }

    private static string DoSetQuality(string? quality)
    {
        if (string.IsNullOrEmpty(quality))
        {
            return "Error: quality parameter is required for SetQuality action";
        }

        var validQualities = new[] { "Stream", "Small", "HQ", "Lossless" };
        if (!validQualities.Contains(quality, StringComparer.OrdinalIgnoreCase))
        {
            return $"Error: Invalid quality. Valid options: {string.Join(", ", validQualities)}";
        }

        var client = ObsConnectionTool.GetClient();
        client.SetRecordingQuality(quality);
        return $"Recording quality set to {quality}";
    }
}
