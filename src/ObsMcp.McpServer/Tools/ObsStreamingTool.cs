using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Sbroenne.ObsMcp.McpServer.Tools;

/// <summary>
/// Actions available for the obs_streaming tool
/// </summary>
public enum StreamingAction
{
    /// <summary>Start streaming</summary>
    Start,
    /// <summary>Stop streaming</summary>
    Stop,
    /// <summary>Get streaming status</summary>
    GetStatus
}

/// <summary>
/// OBS streaming control tool
/// </summary>
[McpServerToolType]
public static class ObsStreamingTool
{
    [McpServerTool(Name = "obs_streaming")]
    [Description(@"Control OBS streaming.

Actions:
- Start: Start streaming (requires stream settings configured in OBS)
- Stop: Stop streaming
- GetStatus: Get streaming status (active, reconnecting, duration, bytes sent)")]
    public static string Streaming(
        [Description("Action to perform: Start, Stop, GetStatus")] StreamingAction action)
    {
        try
        {
            return action switch
            {
                StreamingAction.Start => DoStart(),
                StreamingAction.Stop => DoStop(),
                StreamingAction.GetStatus => DoGetStatus(),
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
        client.StartStreaming();
        return "Streaming started";
    }

    private static string DoStop()
    {
        var client = ObsConnectionTool.GetClient();
        client.StopStreaming();
        return "Streaming stopped";
    }

    private static string DoGetStatus()
    {
        var client = ObsConnectionTool.GetClient();
        var status = client.GetStreamingStatus();

        return $"Streaming Status:\n" +
               $"Active: {status.IsStreaming}\n" +
               $"Reconnecting: {status.IsReconnecting}\n" +
               $"Duration (ms): {status.DurationMs}\n" +
               $"Bytes Sent: {status.BytesSent}";
    }
}
