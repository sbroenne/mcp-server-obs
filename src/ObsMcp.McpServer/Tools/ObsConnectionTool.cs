using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Sbroenne.ObsMcp.McpServer.Tools;

/// <summary>
/// Actions available for the obs_connection tool
/// </summary>
public enum ConnectionAction
{
    /// <summary>Connect to OBS WebSocket server</summary>
    Connect,
    /// <summary>Disconnect from OBS WebSocket server</summary>
    Disconnect,
    /// <summary>Get OBS connection status and basic info</summary>
    GetStatus,
    /// <summary>Get OBS performance statistics</summary>
    GetStats
}

/// <summary>
/// OBS connection management tool
/// </summary>
[McpServerToolType]
public static partial class ObsConnectionTool
{
    private static ObsClient? _client;

    internal static ObsClient GetClient()
    {
        if (_client == null || !_client.IsConnected)
        {
            throw new InvalidOperationException("Not connected to OBS. Use obs_connection with action=Connect first.");
        }
        return _client;
    }

    /// <summary>
    /// Manage OBS WebSocket connection.
    /// 
    /// Actions:
    /// - Connect: Connect to OBS (required before other operations)
    /// - Disconnect: Disconnect from OBS
    /// - GetStatus: Get connection status, current scene, recording/streaming state
    /// - GetStats: Get OBS performance statistics (FPS, CPU, memory)
    /// 
    /// Connection settings can be passed as parameters or via environment variables (OBS_HOST, OBS_PORT, OBS_PASSWORD).
    /// </summary>
    /// <param name="action">Action to perform: Connect, Disconnect, GetStatus, GetStats</param>
    /// <param name="host">OBS WebSocket host (default: localhost, or OBS_HOST env var)</param>
    /// <param name="port">OBS WebSocket port (default: 4455, or OBS_PORT env var)</param>
    /// <param name="password">OBS WebSocket password (or OBS_PASSWORD env var)</param>
    [McpServerTool(Name = "obs_connection")]
    public static partial string Connection(
        ConnectionAction action,
        [DefaultValue(null)] string? host,
        [DefaultValue(null)] int? port,
        [DefaultValue(null)] string? password)
    {
        try
        {
            return action switch
            {
                ConnectionAction.Connect => DoConnect(host, port, password),
                ConnectionAction.Disconnect => DoDisconnect(),
                ConnectionAction.GetStatus => DoGetStatus(),
                ConnectionAction.GetStats => DoGetStats(),
                _ => $"Error: Unknown action '{action}'"
            };
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static string DoConnect(string? host, int? port, string? password)
    {
        var h = host ?? Environment.GetEnvironmentVariable("OBS_HOST") ?? "localhost";
        var p = port ?? int.Parse(Environment.GetEnvironmentVariable("OBS_PORT") ?? "4455");
        var pw = password ?? Environment.GetEnvironmentVariable("OBS_PASSWORD");

        _client?.Dispose();
        _client = new ObsClient();
        _client.Connect(h, p, pw);

        return $"Connected to OBS at {h}:{p}";
    }

    private static string DoDisconnect()
    {
        _client?.Dispose();
        _client = null;
        return "Disconnected from OBS";
    }

    private static string DoGetStatus()
    {
        if (_client == null || !_client.IsConnected)
        {
            return "Not connected to OBS";
        }

        var recording = _client.GetRecordingStatus();
        var streaming = _client.GetStreamingStatus();
        var scene = _client.GetCurrentScene();

        return $"Connected to OBS\n" +
               $"Current Scene: {scene}\n" +
               $"Recording: {(recording.IsRecording ? "Active" : "Inactive")}" +
               $"{(recording.IsPaused ? " (Paused)" : "")}\n" +
               $"Streaming: {(streaming.IsStreaming ? "Active" : "Inactive")}";
    }

    private static string DoGetStats()
    {
        var client = GetClient();
        var stats = client.GetStats();

        return $"OBS Stats:\n" +
               $"FPS: {stats.ActiveFps:F2}\n" +
               $"CPU Usage: {stats.CpuUsage:F2}%\n" +
               $"Memory Usage: {stats.MemoryUsage:F2} MB\n" +
               $"Render Skipped: {stats.RenderSkippedFrames}/{stats.RenderTotalFrames}\n" +
               $"Output Skipped: {stats.OutputSkippedFrames}/{stats.OutputTotalFrames}";
    }
}
