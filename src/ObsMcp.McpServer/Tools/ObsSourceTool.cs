using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Sbroenne.ObsMcp.McpServer.Tools;

/// <summary>
/// Actions available for the obs_source tool
/// </summary>
public enum SourceAction
{
    /// <summary>Add a window capture source</summary>
    AddWindowCapture,
    /// <summary>List available windows for capture</summary>
    ListWindows,
    /// <summary>Set which window to capture</summary>
    SetWindowCapture,
    /// <summary>Remove a source from a scene</summary>
    Remove,
    /// <summary>Show or hide a source</summary>
    SetEnabled
}

/// <summary>
/// OBS source management tool
/// </summary>
[McpServerToolType]
public static class ObsSourceTool
{
    [McpServerTool(Name = "obs_source")]
    [Description(@"Manage OBS capture sources.

Actions:
- AddWindowCapture: Add a window capture source (requires sourceName)
- ListWindows: List available windows for capture (requires sourceName of existing window capture)
- SetWindowCapture: Set which window to capture (requires sourceName and windowValue)
- Remove: Remove a source from a scene (requires sourceName)
- SetEnabled: Show or hide a source (requires sourceName and enabled)

WORKFLOW for recording a window:
1. AddWindowCapture: Create a window capture source
2. ListWindows: See available windows (ask user which one to record)
3. SetWindowCapture: Select the window using the value from ListWindows
4. Use obs_recording with action=Start to begin recording")]
    public static string Source(
        [Description("Action to perform: AddWindowCapture, ListWindows, SetWindowCapture, Remove, SetEnabled")] SourceAction action,
        [Description("Name of the source (required for most actions)")] string? sourceName = null,
        [Description("Scene name (optional, uses current scene if not provided)")] string? sceneName = null,
        [Description("Window value from ListWindows (required for SetWindowCapture)")] string? windowValue = null,
        [Description("Whether the source should be visible (required for SetEnabled)")] bool? enabled = null)
    {
        try
        {
            return action switch
            {
                SourceAction.AddWindowCapture => DoAddWindowCapture(sourceName, sceneName),
                SourceAction.ListWindows => DoListWindows(sourceName),
                SourceAction.SetWindowCapture => DoSetWindowCapture(sourceName, windowValue),
                SourceAction.Remove => DoRemove(sourceName, sceneName),
                SourceAction.SetEnabled => DoSetEnabled(sourceName, enabled, sceneName),
                _ => $"Error: Unknown action '{action}'"
            };
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static string DoAddWindowCapture(string? sourceName, string? sceneName)
    {
        if (string.IsNullOrEmpty(sourceName))
        {
            return "Error: sourceName parameter is required for AddWindowCapture action";
        }

        var client = ObsConnectionTool.GetClient();
        client.AddWindowCapture(sourceName, sceneName);
        return $"Added window capture source '{sourceName}'. NEXT STEP: Use obs_source with action=ListWindows and sourceName='{sourceName}' to see available windows, then use action=SetWindowCapture to select a window.";
    }

    private static string DoListWindows(string? sourceName)
    {
        if (string.IsNullOrEmpty(sourceName))
        {
            return "Error: sourceName parameter is required for ListWindows action";
        }

        var client = ObsConnectionTool.GetClient();
        var windows = client.ListWindows(sourceName);

        if (windows.Count == 0)
        {
            return "No windows available for capture. Make sure the application you want to capture is running.";
        }

        var lines = new List<string> { $"Available Windows ({windows.Count}):", "", "Ask the user which window they want to record, then use obs_source with action=SetWindowCapture.", "" };
        foreach (var window in windows)
        {
            lines.Add($"  - {window.Name}");
            lines.Add($"    Value: {window.Value}");
        }

        return string.Join("\n", lines);
    }

    private static string DoSetWindowCapture(string? sourceName, string? windowValue)
    {
        if (string.IsNullOrEmpty(sourceName))
        {
            return "Error: sourceName parameter is required for SetWindowCapture action";
        }
        if (string.IsNullOrEmpty(windowValue))
        {
            return "Error: windowValue parameter is required for SetWindowCapture action. Use action=ListWindows first to get available window values.";
        }

        var client = ObsConnectionTool.GetClient();
        client.SetWindowCapture(sourceName, windowValue);
        return $"Window capture source '{sourceName}' is now configured to capture: {windowValue}. You can now start recording with obs_recording action=Start.";
    }

    private static string DoRemove(string? sourceName, string? sceneName)
    {
        if (string.IsNullOrEmpty(sourceName))
        {
            return "Error: sourceName parameter is required for Remove action";
        }

        var client = ObsConnectionTool.GetClient();
        client.RemoveSource(sourceName, sceneName);
        return $"Removed source '{sourceName}'";
    }

    private static string DoSetEnabled(string? sourceName, bool? enabled, string? sceneName)
    {
        if (string.IsNullOrEmpty(sourceName))
        {
            return "Error: sourceName parameter is required for SetEnabled action";
        }
        if (!enabled.HasValue)
        {
            return "Error: enabled parameter is required for SetEnabled action";
        }

        var client = ObsConnectionTool.GetClient();
        client.SetSourceEnabled(sourceName, enabled.Value, sceneName);
        var state = enabled.Value ? "visible" : "hidden";
        return $"Source '{sourceName}' is now {state}";
    }
}
