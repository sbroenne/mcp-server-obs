# OBS MCP Server for VS Code

Control OBS Studio recordings, streaming, and scenes directly from VS Code using AI assistants like GitHub Copilot.

## Features

- **Zero Configuration** - Automatically registers the OBS MCP server with VS Code
- **Full OBS Control** - Manage recordings, streaming, scenes, and sources
- **Window Capture** - Programmatically select which window to record
- **Screenshot Support** - Capture screenshots of scenes or sources
- **Virtual Camera** - Control OBS virtual camera

## Requirements

- Windows OS
- [OBS Studio](https://obsproject.com/) with WebSocket server enabled
- VS Code 1.106.0 or later
- GitHub Copilot (or other MCP-compatible AI assistant)

## Installation

### Option 1: VS Code Marketplace
Search for "OBS MCP Server" in the VS Code Extensions view and click Install.

### Option 2: Install from VSIX (Local Development)

1. Download the `.vsix` file from [GitHub Releases](https://github.com/sbroenne/mcp-server-obs/releases) or build locally
2. In VS Code, open the Command Palette (`Ctrl+Shift+P`)
3. Run **Extensions: Install from VSIX...**
4. Select the downloaded `.vsix` file

Or install via command line:
```powershell
code --install-extension obs-mcp-0.0.3.vsix
```

### Option 3: Build from Source

```powershell
# Clone the repository
git clone https://github.com/sbroenne/mcp-server-obs.git
cd mcp-server-obs

# Build the .NET MCP server and VS Code extension
cd vscode-extension
npm install
npm run package

# Install the extension
code --install-extension obs-mcp-0.0.3.vsix
```

## Setup

### 1. Enable OBS WebSocket Server

1. Open OBS Studio
2. Go to **Tools → WebSocket Server Settings**
3. Check **Enable WebSocket server**
4. Note the port (default: 4455)
5. If you set a password, configure it in VS Code settings

### 2. Configure the Extension

Open VS Code Settings and configure:

- `obs-mcp.host` - OBS WebSocket host (default: localhost)
- `obs-mcp.port` - OBS WebSocket port (default: 4455)
- `obs-mcp.password` - WebSocket password (if configured in OBS)

## Available Tools

Once connected, the following tools are available to AI assistants:

### Connection
- `obs_connect` - Connect to OBS WebSocket
- `obs_disconnect` - Disconnect from OBS
- `obs_get_status` - Get connection and recording status
- `obs_get_stats` - Get OBS performance statistics

### Recording
- `obs_start_recording` - Start recording
- `obs_stop_recording` - Stop recording
- `obs_pause_recording` - Pause recording
- `obs_resume_recording` - Resume recording
- `obs_get_recording_status` - Get recording status
- `obs_get_recording_settings` - Get recording format/quality
- `obs_set_recording_format` - Set recording format (mp4, mkv, etc.)
- `obs_set_recording_quality` - Set quality preset

### Streaming
- `obs_start_streaming` - Start streaming
- `obs_stop_streaming` - Stop streaming
- `obs_get_streaming_status` - Get streaming status

### Scenes
- `obs_list_scenes` - List available scenes
- `obs_get_current_scene` - Get current scene
- `obs_set_scene` - Switch to a scene

### Sources
- `obs_list_sources` - List sources in current scene
- `obs_add_display_capture` - Add screen capture source
- `obs_add_window_capture` - Add window capture source
- `obs_list_windows` - List available windows to capture
- `obs_set_window_capture` - Set which window to capture
- `obs_remove_source` - Remove a source
- `obs_set_source_enabled` - Show/hide a source

### Media
- `obs_take_screenshot` - Take a screenshot
- `obs_start_virtual_camera` - Start virtual camera
- `obs_stop_virtual_camera` - Stop virtual camera

## Example Usage

In Copilot Chat, try:

- "Connect to OBS and start recording"
- "Add a window capture of VS Code"
- "List all available windows and capture Chrome"
- "Take a screenshot of the current scene"
- "Switch to scene 'Gaming' and start streaming"

## Troubleshooting

### Connection Failed
- Ensure OBS Studio is running
- Verify WebSocket server is enabled in OBS
- Check the port and password settings
- Make sure no firewall is blocking the connection

### Recording Issues
- Ensure you have at least one source in your scene
- For window capture, use `obs_list_windows` to see available windows

## License

MIT

## Links

- [GitHub Repository](https://github.com/sbroenne/mcp-server-obs)
- [Report Issues](https://github.com/sbroenne/mcp-server-obs/issues)
