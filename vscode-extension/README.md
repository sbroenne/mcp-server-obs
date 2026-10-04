# OBS MCP Server for VS Code

Control OBS Studio recordings, streaming, and scenes directly from VS Code using AI assistants like GitHub Copilot.

## Features

- **Zero Configuration** - Automatically registers the OBS MCP server with VS Code
- **Full OBS Control** - Manage recordings, streaming, scenes, and sources
- **Window Capture** - Programmatically select which window to record
- **Screenshot Support** - Save screenshots of scenes or sources to files

## Requirements

- Windows OS
- [OBS Studio](https://obsproject.com/) with WebSocket server enabled
- VS Code 1.110.0 or later
- GitHub Copilot (or other MCP-compatible AI assistant)

The extension automatically acquires .NET 10 through the .NET Install Tool and uses the acquired runtime to launch the bundled server. If runtime setup fails, it reports the error instead of registering a server that cannot start.

## Installation

### VS Code Marketplace (Recommended)

[![Install in VS Code](https://img.shields.io/badge/VS%20Code-Install%20Extension-blue?logo=visualstudiocode)](https://marketplace.visualstudio.com/items?itemName=sbroenne.obs-mcp)

1. Open VS Code
2. Go to Extensions (`Ctrl+Shift+X`)
3. Search for **"OBS Studio MCP Server"**
4. Click **Install**

Or run: `code --install-extension sbroenne.obs-mcp`

### Install from VSIX (Manual)

1. Download the `.vsix` file from [GitHub Releases](https://github.com/sbroenne/mcp-server-obs/releases)
2. In VS Code, open the Command Palette (`Ctrl+Shift+P`)
3. Run **Extensions: Install from VSIX...**
4. Select the downloaded `.vsix` file

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

| Tool | Actions |
|------|---------|
| `obs_connection` | Connect, Disconnect, GetStatus, GetStats |
| `obs_recording` | Start, Stop, Pause, Resume, GetStatus, GetSettings, SetFormat, SetQuality, SetPath, GetPath |
| `obs_streaming` | Start, Stop, GetStatus |
| `obs_scene` | List, GetCurrent, Set, ListSources |
| `obs_source` | AddWindowCapture, ListWindows, SetWindowCapture, Remove, SetEnabled |
| `obs_audio` | GetInputs, Mute, Unmute, GetMuteState, SetVolume, GetVolume, MuteAll, UnmuteAll |
| `obs_media` | SaveScreenshot |

Recording starts with audio muted by default. Use `obs_recording(action: Start, muteAudio: false)` to preserve the configured audio input states.

## Example Usage

In Copilot Chat, try:

- "Connect to OBS and start recording"
- "Add a window capture of VS Code"
- "List all available windows and capture Chrome"
- "Save a screenshot of the current scene to C:/Screenshots/capture.png"
- "Switch to scene 'Gaming' and start streaming"

## Troubleshooting

### Connection Failed
- Ensure OBS Studio is running
- Verify WebSocket server is enabled in OBS
- Check the port and password settings
- Make sure no firewall is blocking the connection

### Recording Issues
- Ensure you have at least one source in your scene
- For window capture, use `obs_source(action: ListWindows, sourceName: "Window Capture")` to see available windows

### Runtime Setup Failed
- Ensure the .NET Install Tool extension (`ms-dotnettools.vscode-dotnet-runtime`) is installed
- Allow it to download the .NET 10 runtime, then reload VS Code
- No separate system-wide .NET installation is required

## Development

Use the .NET 10 SDK and Node.js 24:

```powershell
npm ci
npm test
npm run package
```

Packaging publishes the server once and compiles the extension through `vscode:prepublish`. Startup tests cover runtime acquisition, server launch arguments, configuration changes, and setup failures.

## License

MIT

## Links

- [GitHub Repository](https://github.com/sbroenne/mcp-server-obs)
- [Report Issues](https://github.com/sbroenne/mcp-server-obs/issues)
