using ModelContextProtocol.Server;
using System.ComponentModel;

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
    /// <summary>Get recording settings (format, quality, encoder)</summary>
    GetSettings,
    /// <summary>Set recording format (mp4, mkv, etc.)</summary>
    SetFormat,
    /// <summary>Set recording quality preset</summary>
    SetQuality
}

/// <summary>
/// OBS recording control tool
/// </summary>
[McpServerToolType]
public static class ObsRecordingTool
{
    [McpServerTool(Name = "obs_recording")]
    [Description(@"Control OBS recording.

Actions:
- Start: Start recording (ensure a capture source exists first!)
- Stop: Stop recording and save the file
- Pause: Pause the current recording
- Resume: Resume a paused recording
- GetStatus: Get recording status (active, paused, timecode)
- GetSettings: Get recording format, quality, and encoder
- SetFormat: Set recording format (mp4, mkv, flv, mov, ts)
- SetQuality: Set recording quality (Stream, Small, HQ, Lossless)

IMPORTANT: Before starting a recording, add a capture source using obs_source with action=AddWindowCapture to avoid a BLACK SCREEN.")]
    public static string Recording(
        [Description("Action to perform: Start, Stop, Pause, Resume, GetStatus, GetSettings, SetFormat, SetQuality")] RecordingAction action,
        [Description("Recording format for SetFormat action: mp4 (recommended), mkv, flv, mov, ts")] string? format = null,
        [Description("Quality preset for SetQuality action: Stream, Small, HQ (recommended), Lossless")] string? quality = null)
    {
        try
        {
            return action switch
            {
                RecordingAction.Start => DoStart(),
                RecordingAction.Stop => DoStop(),
                RecordingAction.Pause => DoPause(),
                RecordingAction.Resume => DoResume(),
                RecordingAction.GetStatus => DoGetStatus(),
                RecordingAction.GetSettings => DoGetSettings(),
                RecordingAction.SetFormat => DoSetFormat(format),
                RecordingAction.SetQuality => DoSetQuality(quality),
                _ => $"Error: Unknown action '{action}'"
            };
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static string DoStart()
    {
        var client = ObsConnectionTool.GetClient();
        client.StartRecording();
        return "Recording started. Use obs_recording with action=Stop when finished.";
    }

    private static string DoStop()
    {
        var client = ObsConnectionTool.GetClient();
        client.StopRecording();
        return "Recording stopped and saved";
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

        return $"Recording Settings:\n" +
               $"Format: {settings.Format}\n" +
               $"Quality: {settings.Quality}\n" +
               $"Encoder: {settings.Encoder}";
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
