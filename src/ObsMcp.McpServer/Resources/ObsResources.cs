using ModelContextProtocol.Server;

namespace Sbroenne.ObsMcp.McpServer.Resources;

/// <summary>
/// Static resources for OBS best practices and guidance
/// </summary>
[McpServerResourceType]
public static class ObsResources
{
    /// <summary>
    /// Best practices for OBS recording workflows
    /// </summary>
    [McpServerResource(
        UriTemplate = "obs://guides/recording-best-practices",
        Name = "Recording Best Practices",
        MimeType = "text/markdown")]
    public static string GetRecordingBestPractices()
    {
        return """
            # OBS Recording Best Practices

            ## Before Recording

            1. **Always Add a Capture Source**
               - Without a capture source, your recording will show a BLACK SCREEN
               - Use Window Capture for specific applications
               - Use Display Capture for entire monitors

            2. **Verify Audio Configuration**
               - By default, audio is MUTED when starting recordings
               - Use `muteAudio: false` if you need system audio
               - Check Desktop Audio and Mic/Aux inputs

            3. **Choose the Right Capture Method**
               - **Window Capture**: Best for specific apps (VS Code, Chrome, etc.)
               - **Display Capture**: Best for tutorials showing multiple windows
               - **Game Capture**: Best for games (uses hardware hooks)

            ## Recording Settings

            ### Format Recommendations
            - **MP4**: Best compatibility, but can corrupt if OBS crashes
            - **MKV**: Safer (survives crashes), can remux to MP4 after
            - **MOV**: Good for macOS/Final Cut workflows

            ### Quality Presets
            - **High Quality**: Larger files, best for final output
            - **Indistinguishable**: Good balance of quality and size
            - **Lossless**: Huge files, only for heavy editing

            ## Common Issues

            ### Black Screen
            - No capture source in scene
            - Window capture targeting minimized window
            - Display capture needs admin rights on some systems

            ### No Audio
            - Audio inputs are muted (default behavior)
            - Wrong audio device selected
            - Application audio routing issues

            ### Connection Failed
            - OBS is not running
            - WebSocket Server is not enabled in OBS
            - Wrong port or password

            ## Workflow Tips

            1. Always connect and check status first
            2. List windows before selecting (names can change)
            3. Verify capture is working before long recordings
            4. Use pause/resume for breaks instead of stop/start
            5. Check output path before recording
            """;
    }

    /// <summary>
    /// Quick reference for all OBS MCP commands
    /// </summary>
    [McpServerResource(
        UriTemplate = "obs://guides/command-reference",
        Name = "Command Quick Reference",
        MimeType = "text/markdown")]
    public static string GetCommandReference()
    {
        return """
            # OBS MCP Command Quick Reference

            ## Connection (obs_connection)
            | Action | Description |
            |--------|-------------|
            | Connect | Connect to OBS WebSocket |
            | Disconnect | Disconnect from OBS |
            | GetStatus | Check connection status |
            | GetStats | Get OBS performance stats |

            ## Recording (obs_recording)
            | Action | Description |
            |--------|-------------|
            | Start | Start recording (muteAudio=true by default) |
            | Stop | Stop and save recording |
            | Pause | Pause recording |
            | Resume | Resume paused recording |
            | GetStatus | Get recording status |
            | GetSettings | View all recording settings |
            | SetFormat | Change format (mp4, mkv, mov, flv, ts) |
            | SetQuality | Change quality preset |
            | SetPath | Set output directory |
            | GetPath | Get current output directory |

            ## Streaming (obs_streaming)
            | Action | Description |
            |--------|-------------|
            | Start | Start streaming |
            | Stop | Stop streaming |
            | GetStatus | Get streaming status |

            ## Scenes (obs_scene)
            | Action | Description |
            |--------|-------------|
            | List | List all scenes |
            | GetCurrent | Get active scene |
            | Set | Switch to scene |
            | ListSources | List sources in scene |

            ## Sources (obs_source)
            | Action | Description |
            |--------|-------------|
            | AddWindowCapture | Create window capture source |
            | ListWindows | List capturable windows |
            | SetWindowCapture | Select window to capture |
            | Remove | Remove source from scene |
            | SetEnabled | Show/hide source |

            ## Audio (obs_audio)
            | Action | Description |
            |--------|-------------|
            | GetInputs | List audio inputs |
            | Mute | Mute an input |
            | Unmute | Unmute an input |
            | GetMuteState | Check mute status |
            | SetVolume | Set volume (0.0-1.0) |
            | GetVolume | Get current volume |
            | MuteAll | Mute all inputs |
            | UnmuteAll | Unmute all inputs |

            ## Media (obs_media)
            | Action | Description |
            |--------|-------------|
            | SaveScreenshot | Save screenshot to file |
            | StartVirtualCamera | Start virtual camera |
            | StopVirtualCamera | Stop virtual camera |
            """;
    }

    /// <summary>
    /// Error recovery guide for common OBS issues
    /// </summary>
    [McpServerResource(
        UriTemplate = "obs://guides/error-recovery",
        Name = "Error Recovery Guide",
        MimeType = "text/markdown")]
    public static string GetErrorRecoveryGuide()
    {
        return """
            # OBS Error Recovery Guide

            ## Connection Errors

            ### "Not connected to OBS"
            1. Ensure OBS is running
            2. Enable WebSocket Server: Tools → obs-websocket Settings
            3. Note the port (default: 4455) and password
            4. Use: `obs_connection(action: Connect, host: "localhost", port: 4455, password: "your-password")`

            ### "Connection refused"
            - OBS may not be running
            - WebSocket port may be blocked by firewall
            - Try restarting OBS

            ### "Authentication failed"
            - Check password in OBS WebSocket settings
            - Password is case-sensitive

            ## Recording Errors

            ### "Recording already active"
            - A recording is in progress
            - Use `obs_recording(action: GetStatus)` to check
            - Use `obs_recording(action: Stop)` to stop current recording

            ### "Recording not active"
            - No recording in progress to stop/pause
            - Use `obs_recording(action: Start)` to begin

            ### "Output directory not found"
            - The configured output path doesn't exist
            - Use `obs_recording(action: SetPath)` with a valid directory

            ## Source Errors

            ### "Source not found"
            - The source name doesn't exist
            - Use `obs_scene(action: ListSources)` to see available sources
            - Source names are case-sensitive

            ### "Scene not found"
            - The scene name doesn't exist
            - Use `obs_scene(action: List)` to see available scenes

            ### "Window not found"
            - The window may have closed or the title changed
            - Use `obs_source(action: ListWindows)` to refresh the window list

            ## Audio Errors

            ### "Audio input not found"
            - Use `obs_audio(action: GetInputs)` to list available inputs
            - Common names: "Desktop Audio", "Mic/Aux"
            - Names depend on your OBS configuration

            ## Recovery Steps

            ### Full Reset Workflow
            1. `obs_connection(action: Disconnect)` - Clean disconnect
            2. `obs_connection(action: Connect)` - Fresh connection
            3. `obs_connection(action: GetStatus)` - Verify connected
            4. `obs_scene(action: GetCurrent)` - Check scene
            5. `obs_scene(action: ListSources)` - Verify sources

            ### Recording Recovery
            1. `obs_recording(action: GetStatus)` - Check state
            2. If stuck: `obs_recording(action: Stop)` - Force stop
            3. `obs_recording(action: GetSettings)` - Verify settings
            4. `obs_recording(action: Start)` - Try again
            """;
    }
}
