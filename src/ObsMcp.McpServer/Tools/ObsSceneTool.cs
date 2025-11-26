using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Sbroenne.ObsMcp.McpServer.Tools;

/// <summary>
/// Actions available for the obs_scene tool
/// </summary>
public enum SceneAction
{
    /// <summary>List all available scenes</summary>
    List,
    /// <summary>Get current active scene</summary>
    GetCurrent,
    /// <summary>Switch to a different scene</summary>
    Set,
    /// <summary>List all sources in a scene</summary>
    ListSources
}

/// <summary>
/// OBS scene management tool
/// </summary>
[McpServerToolType]
public static class ObsSceneTool
{
    [McpServerTool(Name = "obs_scene")]
    [Description(@"Manage OBS scenes.

Actions:
- List: List all available scenes
- GetCurrent: Get the current active scene
- Set: Switch to a different scene (requires sceneName)
- ListSources: List all sources in a scene")]
    public static string Scene(
        [Description("Action to perform: List, GetCurrent, Set, ListSources")] SceneAction action,
        [Description("Scene name (required for Set, optional for ListSources - uses current scene if not provided)")] string? sceneName = null)
    {
        try
        {
            return action switch
            {
                SceneAction.List => DoList(),
                SceneAction.GetCurrent => DoGetCurrent(),
                SceneAction.Set => DoSet(sceneName),
                SceneAction.ListSources => DoListSources(sceneName),
                _ => $"Error: Unknown action '{action}'"
            };
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static string DoList()
    {
        var client = ObsConnectionTool.GetClient();
        var scenes = client.GetScenes();

        if (scenes.Count == 0)
        {
            return "No scenes found";
        }

        var lines = new List<string> { $"Available Scenes ({scenes.Count}):" };
        foreach (var scene in scenes)
        {
            var current = scene.IsCurrent ? " (current)" : "";
            lines.Add($"  - {scene.Name}{current}");
        }

        return string.Join("\n", lines);
    }

    private static string DoGetCurrent()
    {
        var client = ObsConnectionTool.GetClient();
        var sceneName = client.GetCurrentScene();
        return $"Current scene: {sceneName}";
    }

    private static string DoSet(string? sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return "Error: sceneName parameter is required for Set action";
        }

        var client = ObsConnectionTool.GetClient();
        client.SetScene(sceneName);
        return $"Switched to scene: {sceneName}";
    }

    private static string DoListSources(string? sceneName)
    {
        var client = ObsConnectionTool.GetClient();
        var sources = client.GetSources(sceneName);

        if (sources.Count == 0)
        {
            return "No sources found in scene";
        }

        var lines = new List<string> { $"Sources ({sources.Count}):" };
        foreach (var source in sources)
        {
            var enabled = source.IsEnabled ? "visible" : "hidden";
            lines.Add($"  - {source.Name} ({source.Kind}) [{enabled}]");
        }

        return string.Join("\n", lines);
    }
}
