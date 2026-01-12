# OBS Studio MCP Server

A .NET 10 MCP (Model Context Protocol) server for controlling OBS Studio. Use it standalone with any MCP client or via the included VS Code extension.

> ⚠️ **This project is in active development.** APIs and features may change.

> **Platform:** Windows only (OBS WebSocket limitation)

## Features

Control OBS Studio through AI assistants (GitHub Copilot, Claude, etc.):

- **Recording**: Start, stop, pause, resume, configure format/quality (audio muted by default)
- **Streaming**: Start, stop, monitor status
- **Scenes**: List and switch scenes
- **Sources**: Add window captures, manage visibility
- **Audio**: Mute/unmute inputs, control volume (desktop audio, mic)
- **Window Capture**: Programmatically select windows to record
- **Media**: Screenshots, virtual camera

## Installation

### VS Code / VS Code Insiders (Recommended)

Install directly from the VS Code Marketplace:

[![Install in VS Code](https://img.shields.io/badge/VS%20Code-Install%20Extension-blue?logo=visualstudiocode)](https://marketplace.visualstudio.com/items?itemName=sbroenne.obs-mcp-server)

1. Open VS Code
2. Go to Extensions (`Ctrl+Shift+X`)
3. Search for **"OBS Studio MCP Server"**
4. Click **Install**

Or run: `code --install-extension sbroenne.obs-mcp-server`

### Other MCP Clients (Claude Desktop, Cursor, Windsurf, etc.)

Download from [GitHub Releases](https://github.com/sbroenne/mcp-server-obs/releases):

| Download | Description |
|----------|-------------|
| `obs-mcp-server-*.zip` | Requires [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) |

Extract and add to your MCP client config:

```json
{
  "servers": {
    "obs": {
      "command": "dotnet",
      "args": ["C:/Tools/obs-mcp-server/Sbroenne.ObsMcp.McpServer.dll"],
      "env": {
        "OBS_HOST": "${input:obs_host}",
        "OBS_PORT": "${input:obs_port}",
        "OBS_PASSWORD": "${input:obs_password}"
      }
    }
  },
  "inputs": [
    {
      "id": "obs_host",
      "type": "promptString",
      "description": "OBS WebSocket host",
      "default": "localhost"
    },
    {
      "id": "obs_port",
      "type": "promptString",
      "description": "OBS WebSocket port",
      "default": "4455"
    },
    {
      "id": "obs_password",
      "type": "promptString",
      "description": "OBS WebSocket password",
      "password": true
    }
  ]
}
```

**Config file locations:**
| Client | Path |
|--------|------|
| Claude Desktop | `%APPDATA%\Claude\claude_desktop_config.json` |
| Cursor | `~/.cursor/mcp.json` |
| Windsurf | `~/.codeium/windsurf/mcp_config.json` |

## Prerequisites

1. **OBS Studio** - Download from [obsproject.com](https://obsproject.com/)
2. **OBS WebSocket** - Built into OBS Studio 28.0+

### Enable OBS WebSocket

1. Open OBS Studio
2. Go to **Tools** → **WebSocket Server Settings**
3. Enable **WebSocket server**
4. Note the port (default: `4455`) and password
5. Click **Apply**

## Available Tools

| Tool | Actions |
|------|---------|
| `obs_connection` | Connect, Disconnect, GetStatus, GetStats |
| `obs_recording` | Start, Stop, Pause, Resume, GetStatus, GetSettings, SetFormat, SetQuality, SetPath, GetPath |
| `obs_streaming` | Start, Stop, GetStatus |
| `obs_scene` | List, GetCurrent, Set, ListSources |
| `obs_source` | AddWindowCapture, ListWindows, SetWindowCapture, Remove, SetEnabled |
| `obs_audio` | GetInputs, Mute, Unmute, GetMuteState, SetVolume, GetVolume, MuteAll, UnmuteAll |
| `obs_media` | SaveScreenshot, StartVirtualCamera, StopVirtualCamera |

> **Note:** Recording starts with audio **muted** by default. Use `muteAudio=false` to include audio.

## Quick Start: Record a Window

> **Important**: Without a capture source, recordings will be BLACK!

1. **Connect**: `obs_connection(action: Connect)`
2. **Add source**: `obs_source(action: AddWindowCapture, sourceName: "My Capture")`
3. **List windows**: `obs_source(action: ListWindows, sourceName: "My Capture")`
4. **Select window**: `obs_source(action: SetWindowCapture, sourceName: "My Capture", windowValue: "...")`
5. **Record**: `obs_recording(action: Start)` or `obs_recording(action: Start, path: "C:/Videos")`

Or just ask your AI assistant: *"Record VS Code in OBS"*

## Development

### Build & Test

```powershell
# Build
dotnet build

# Run unit tests (no OBS required)
dotnet test --filter "Category!=Integration"

# Run integration tests (requires OBS with .env configured)
dotnet test --filter "Category=Integration"
```

### Project Structure

```
mcp-server-obs/
├── src/ObsMcp.McpServer/       # .NET 10 MCP Server
│   └── Tools/                  # 7 resource-based tools
├── tests/                      # Unit & integration tests
├── vscode-extension/           # VS Code extension wrapper
└── .github/workflows/          # Release automation
```

### Release

- **MCP Server**: Push tag `mcp-v1.0.0` → Builds standalone zips
- **VS Code Extension**: Push tag `vscode-v1.0.0` → Builds .vsix + publishes to Marketplace

## License

MIT
