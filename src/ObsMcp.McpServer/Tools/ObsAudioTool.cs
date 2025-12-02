using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Sbroenne.ObsMcp.McpServer.Tools;

/// <summary>
/// Actions available for the obs_audio tool
/// </summary>
public enum AudioAction
{
    /// <summary>List all audio inputs (desktop audio, mic, etc.)</summary>
    GetInputs,
    /// <summary>Mute an audio input</summary>
    Mute,
    /// <summary>Unmute an audio input</summary>
    Unmute,
    /// <summary>Get the mute state of an audio input</summary>
    GetMuteState,
    /// <summary>Set the volume of an audio input</summary>
    SetVolume,
    /// <summary>Get the volume of an audio input</summary>
    GetVolume,
    /// <summary>Mute all audio inputs (desktop audio + mic)</summary>
    MuteAll,
    /// <summary>Unmute all audio inputs (desktop audio + mic)</summary>
    UnmuteAll
}

/// <summary>
/// OBS audio control tool
/// </summary>
[McpServerToolType]
public static partial class ObsAudioTool
{
    /// <summary>
    /// Control OBS audio inputs.
    /// 
    /// Actions:
    /// - GetInputs: List all audio inputs (Desktop Audio, Mic/Aux, etc.)
    /// - Mute: Mute a specific audio input
    /// - Unmute: Unmute a specific audio input
    /// - GetMuteState: Check if an audio input is muted
    /// - SetVolume: Set volume level (0.0-1.0) for an audio input
    /// - GetVolume: Get current volume level of an audio input
    /// - MuteAll: Mute all audio inputs (desktop audio and microphone)
    /// - UnmuteAll: Unmute all audio inputs
    /// 
    /// Common input names: 'Desktop Audio', 'Mic/Aux'
    /// 
    /// TIP: Use MuteAll before recording to capture video without sound.
    /// </summary>
    /// <param name="action">Action to perform: GetInputs, Mute, Unmute, GetMuteState, SetVolume, GetVolume, MuteAll, UnmuteAll</param>
    /// <param name="inputName">Audio input name for Mute/Unmute/GetMuteState/SetVolume/GetVolume actions (e.g., 'Desktop Audio', 'Mic/Aux')</param>
    /// <param name="volume">Volume level for SetVolume action (0.0 = silent, 1.0 = full volume)</param>
    [McpServerTool(Name = "obs_audio")]
    public static partial string Audio(
        AudioAction action,
        [DefaultValue(null)] string? inputName,
        [DefaultValue(null)] double? volume)
    {
        try
        {
            return action switch
            {
                AudioAction.GetInputs => DoGetInputs(),
                AudioAction.Mute => DoMute(inputName),
                AudioAction.Unmute => DoUnmute(inputName),
                AudioAction.GetMuteState => DoGetMuteState(inputName),
                AudioAction.SetVolume => DoSetVolume(inputName, volume),
                AudioAction.GetVolume => DoGetVolume(inputName),
                AudioAction.MuteAll => DoMuteAll(),
                AudioAction.UnmuteAll => DoUnmuteAll(),
                _ => $"Error: Unknown action '{action}'"
            };
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static string DoGetInputs()
    {
        var client = ObsConnectionTool.GetClient();
        var inputs = client.GetSpecialInputs();

        if (inputs.Count == 0)
        {
            return "No audio inputs found";
        }

        var result = "Audio Inputs:\n";
        foreach (var input in inputs)
        {
            var muteStatus = client.GetInputMute(input.Name) ? " (muted)" : "";
            result += $"- {input.Name}: {input.Kind}{muteStatus}\n";
        }
        return result.TrimEnd();
    }

    private static string DoMute(string? inputName)
    {
        if (string.IsNullOrEmpty(inputName))
        {
            return "Error: inputName parameter is required for Mute action";
        }

        var client = ObsConnectionTool.GetClient();
        client.SetInputMute(inputName, true);
        return $"Muted '{inputName}'";
    }

    private static string DoUnmute(string? inputName)
    {
        if (string.IsNullOrEmpty(inputName))
        {
            return "Error: inputName parameter is required for Unmute action";
        }

        var client = ObsConnectionTool.GetClient();
        client.SetInputMute(inputName, false);
        return $"Unmuted '{inputName}'";
    }

    private static string DoGetMuteState(string? inputName)
    {
        if (string.IsNullOrEmpty(inputName))
        {
            return "Error: inputName parameter is required for GetMuteState action";
        }

        var client = ObsConnectionTool.GetClient();
        var isMuted = client.GetInputMute(inputName);
        return $"'{inputName}' is {(isMuted ? "muted" : "unmuted")}";
    }

    private static string DoSetVolume(string? inputName, double? volume)
    {
        if (string.IsNullOrEmpty(inputName))
        {
            return "Error: inputName parameter is required for SetVolume action";
        }

        if (!volume.HasValue)
        {
            return "Error: volume parameter is required for SetVolume action";
        }

        if (volume < 0.0 || volume > 1.0)
        {
            return "Error: volume must be between 0.0 and 1.0";
        }

        var client = ObsConnectionTool.GetClient();
        client.SetInputVolume(inputName, volume.Value);
        return $"Set volume of '{inputName}' to {volume:P0}";
    }

    private static string DoGetVolume(string? inputName)
    {
        if (string.IsNullOrEmpty(inputName))
        {
            return "Error: inputName parameter is required for GetVolume action";
        }

        var client = ObsConnectionTool.GetClient();
        var (mul, db) = client.GetInputVolume(inputName);
        return $"Volume of '{inputName}': {mul:P0} ({db:F1} dB)";
    }

    private static string DoMuteAll()
    {
        var client = ObsConnectionTool.GetClient();
        var inputs = client.GetSpecialInputs();
        var muted = new List<string>();

        foreach (var input in inputs)
        {
            client.SetInputMute(input.Name, true);
            muted.Add(input.Name);
        }

        if (muted.Count == 0)
        {
            return "No audio inputs found to mute";
        }

        return $"Muted all audio inputs: {string.Join(", ", muted)}";
    }

    private static string DoUnmuteAll()
    {
        var client = ObsConnectionTool.GetClient();
        var inputs = client.GetSpecialInputs();
        var unmuted = new List<string>();

        foreach (var input in inputs)
        {
            client.SetInputMute(input.Name, false);
            unmuted.Add(input.Name);
        }

        if (unmuted.Count == 0)
        {
            return "No audio inputs found to unmute";
        }

        return $"Unmuted all audio inputs: {string.Join(", ", unmuted)}";
    }
}
