using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging to stderr for MCP protocol compliance
// (stdout is reserved for JSON-RPC messages)
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "obs-mcp-server",
            Version = "1.0.0"
        };

        options.ServerInstructions = """
            # OBS MCP Server - Recording Setup Instructions

            This server controls OBS Studio for screen recording and streaming.
            
            ## Available Tools (7 resource-based tools with actions)
            
            - `obs_connection` - Connect, Disconnect, GetStatus, GetStats
            - `obs_recording` - Start, Stop, Pause, Resume, GetStatus, GetSettings, SetFormat, SetQuality, SetPath, GetPath
            - `obs_streaming` - Start, Stop, GetStatus
            - `obs_scene` - List, GetCurrent, Set, ListSources
            - `obs_source` - AddWindowCapture, ListWindows, SetWindowCapture, Remove, SetEnabled
            - `obs_audio` - GetInputs, Mute, Unmute, GetMuteState, SetVolume, GetVolume, MuteAll, UnmuteAll
            - `obs_media` - TakeScreenshot, StartVirtualCamera, StopVirtualCamera
            
            ## IMPORTANT: Audio is MUTED by Default
            
            When starting a recording, audio is automatically muted (for screen capture scenarios).
            Use `obs_recording(action: Start, muteAudio: false)` to record WITH audio.
            Use `obs_audio` tool to manage audio inputs (desktop audio, mic) manually.
            
            ## IMPORTANT: Black Screen Prevention
            
            Before starting a recording, you MUST add a capture source to your scene.
            Without a source, recordings will show a BLACK SCREEN!
            
            ## Primary Use Case: Recording Application Windows
            
            The main use case is recording specific application windows (e.g., VS Code, Chrome, Terminal).
            
            ### Window Capture Workflow (Recommended)
            
            Follow these steps IN ORDER to set up window recording:
            
            1. **Connect to OBS** (if not already connected):
               - Use `obs_connection(action: Connect)` with host, port, and password if authentication is enabled
            
            2. **Add a window capture source**:
               - Use `obs_source(action: AddWindowCapture, sourceName: "Window Capture")`
               - This creates the capture source but doesn't select a window yet
            
            3. **List available windows**:
               - Use `obs_source(action: ListWindows, sourceName: "Window Capture")`
               - This returns a list of windows with their names and capture values
               - ALWAYS ask the user which window they want to record
            
            4. **Select the target window**:
               - Use `obs_source(action: SetWindowCapture, sourceName: "Window Capture", windowValue: "...")`
               - Use the exact "value" string from the window list
            
            5. **Start recording** (audio muted by default):
               - Use `obs_recording(action: Start)` to begin capturing (audio muted)
               - Use `obs_recording(action: Start, muteAudio: false)` to include audio
            
            ### Example Workflow
            
            ```
            1. obs_connection(action: Connect, host: "localhost", port: 4455)
            2. obs_source(action: AddWindowCapture, sourceName: "My Capture")
            3. obs_source(action: ListWindows, sourceName: "My Capture")
               -> Returns: Visual Studio Code (value: "Code.exe:Chrome_WidgetWin_1:Code.exe")
            4. obs_source(action: SetWindowCapture, sourceName: "My Capture", windowValue: "Code.exe:Chrome_WidgetWin_1:Code.exe")
            5. obs_recording(action: Start)  # Audio muted by default
            ```
            
            ## Audio Control
            
            - `obs_audio(action: GetInputs)` - List audio inputs (Desktop Audio, Mic/Aux)
            - `obs_audio(action: MuteAll)` / `obs_audio(action: UnmuteAll)` - Mute/unmute all
            - `obs_audio(action: Mute, inputName: "Desktop Audio")` - Mute specific input
            - `obs_audio(action: SetVolume, inputName: "Mic/Aux", volume: 0.5)` - Set volume (0.0-1.0)
            
            ## Display Capture (Alternative)
            
            To capture an entire monitor/screen instead of a specific window, add display capture source in OBS manually,
            then use `obs_recording(action: Start)`.
            
            ## Managing Recording Sessions
            
            - `obs_recording(action: Pause)` / `obs_recording(action: Resume)` - Pause/resume
            - `obs_recording(action: Stop)` - Stop and save the recording
            - `obs_recording(action: GetStatus)` - Check if recording is active
            
            ## Recording Settings
            
            - `obs_recording(action: GetSettings)` - View current format, quality, encoder, and path
            - `obs_recording(action: SetFormat, format: "mp4")` - Change format (mp4, mkv, mov, flv, ts)
            - `obs_recording(action: SetQuality, quality: "HQ")` - Change quality preset
            - `obs_recording(action: SetPath, path: "C:/Videos")` - Change output directory
            - `obs_recording(action: GetPath)` - View current output directory
            
            ## Key Guidelines
            
            1. Always check connection status before operations
            2. Always add a capture source before recording
            3. For window capture, always list windows and let user choose
            4. Audio is muted by default - use muteAudio=false if needed
            5. Use window capture for specific apps
            """;
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var app = builder.Build();
await app.RunAsync();
