using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Sbroenne.ObsMcp.McpServer.Tools;

/// <summary>
/// Actions available for the obs_media tool
/// </summary>
public enum MediaAction
{
    /// <summary>Take a screenshot</summary>
    TakeScreenshot,
    /// <summary>Start the virtual camera</summary>
    StartVirtualCamera,
    /// <summary>Stop the virtual camera</summary>
    StopVirtualCamera
}

/// <summary>
/// OBS media operations tool
/// </summary>
[McpServerToolType]
public static partial class ObsMediaTool
{
    /// <summary>
    /// OBS media operations (screenshots, virtual camera).
    /// 
    /// Actions:
    /// - TakeScreenshot: Capture a screenshot of the current scene or a specific source
    /// - StartVirtualCamera: Start the OBS virtual camera output
    /// - StopVirtualCamera: Stop the OBS virtual camera
    /// </summary>
    /// <param name="action">Action to perform: TakeScreenshot, StartVirtualCamera, StopVirtualCamera</param>
    /// <param name="sourceName">Source name to screenshot (optional, defaults to current scene)</param>
    /// <param name="imageFormat">Screenshot format: png, jpg, or bmp (default: png)</param>
    /// <param name="width">Screenshot width (optional)</param>
    /// <param name="height">Screenshot height (optional)</param>
    [McpServerTool(Name = "obs_media")]
    public static partial string Media(
        MediaAction action,
        [DefaultValue(null)] string? sourceName,
        [DefaultValue(null)] string? imageFormat,
        [DefaultValue(null)] int? width,
        [DefaultValue(null)] int? height)
    {
        try
        {
            return action switch
            {
                MediaAction.TakeScreenshot => DoTakeScreenshot(sourceName, imageFormat, width, height),
                MediaAction.StartVirtualCamera => DoStartVirtualCamera(),
                MediaAction.StopVirtualCamera => DoStopVirtualCamera(),
                _ => $"Error: Unknown action '{action}'"
            };
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static string DoTakeScreenshot(string? sourceName, string? imageFormat, int? width, int? height)
    {
        var format = imageFormat ?? "png";
        var client = ObsConnectionTool.GetClient();
        var result = client.TakeScreenshot(sourceName, format, width, height);

        if (string.IsNullOrEmpty(result))
        {
            return "Error: Screenshot returned empty result";
        }

        return $"Screenshot captured:\n" +
               $"Format: {format}\n" +
               $"Data length: {result.Length} characters (base64)";
    }

    private static string DoStartVirtualCamera()
    {
        var client = ObsConnectionTool.GetClient();
        client.StartVirtualCamera();
        return "Virtual camera started";
    }

    private static string DoStopVirtualCamera()
    {
        var client = ObsConnectionTool.GetClient();
        client.StopVirtualCamera();
        return "Virtual camera stopped";
    }
}
