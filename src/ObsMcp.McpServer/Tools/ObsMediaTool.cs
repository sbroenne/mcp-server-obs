using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Sbroenne.ObsMcp.McpServer.Tools;

/// <summary>
/// Actions available for the obs_media tool
/// </summary>
public enum MediaAction
{
    /// <summary>Save a screenshot to a file</summary>
    SaveScreenshot
}

/// <summary>
/// OBS media operations tool
/// </summary>
[McpServerToolType]
public static partial class ObsMediaTool
{
    /// <summary>
    /// Save OBS screenshots to files.
    /// 
    /// Actions:
    /// - SaveScreenshot: Save a screenshot of the current scene or a specific source to a file
    /// </summary>
    /// <param name="action">Action to perform: SaveScreenshot</param>
    /// <param name="filePath">Full file path to save the screenshot (required for SaveScreenshot, e.g., C:/Screenshots/capture.png)</param>
    /// <param name="sourceName">Source name to screenshot (optional, defaults to current scene)</param>
    /// <param name="imageFormat">Screenshot format: png or jpg (default: png)</param>
    /// <param name="width">Screenshot width in pixels (optional, defaults to source resolution)</param>
    /// <param name="height">Screenshot height in pixels (optional, defaults to source resolution)</param>
    /// <param name="quality">Image compression quality 1-100 (optional, for jpg format)</param>
    [McpServerTool(Name = "obs_media", Title = "OBS Media Operations", Destructive = true)]
    public static partial CallToolResult Media(
        MediaAction action,
        [DefaultValue(null)] string? filePath,
        [DefaultValue(null)] string? sourceName,
        [DefaultValue(null)] string? imageFormat,
        [DefaultValue(null)] int? width,
        [DefaultValue(null)] int? height,
        [DefaultValue(null)] int? quality)
    {
        try
        {
            return ObsToolResult.FromText(action switch
            {
                MediaAction.SaveScreenshot => DoSaveScreenshot(filePath, sourceName, imageFormat, width, height, quality),
                _ => $"Error: Unknown action '{action}'"
            });
        }
        catch (Exception ex)
        {
            return ObsToolResult.FromText($"Error: {ex.Message}");
        }
    }

    private static string DoSaveScreenshot(string? filePath, string? sourceName, string? imageFormat, int? width, int? height, int? quality)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return "Error: filePath is required for SaveScreenshot action. Please provide a full file path (e.g., C:/Screenshots/capture.png)";
        }

        // Determine format from file extension if not specified
        var format = imageFormat;
        if (string.IsNullOrEmpty(format))
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            format = extension switch
            {
                ".jpg" or ".jpeg" => "jpg",
                ".bmp" => "bmp",
                _ => "png"
            };
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var client = ObsConnectionTool.GetClient();
        client.SaveScreenshot(filePath, sourceName, format, width, height, quality);

        return $"Screenshot saved to: {filePath}";
    }

}
