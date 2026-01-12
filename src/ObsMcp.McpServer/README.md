# OBS MCP Server

A .NET 10 MCP (Model Context Protocol) server for controlling OBS Studio. This server can be used standalone with any MCP-compatible client or as part of the VS Code extension.

> **Platform:** Windows only (due to OBS WebSocket and window capture dependencies)

## Features

- Connect to OBS Studio via WebSocket
- Control recording (start, stop, pause, resume)
- Control streaming (start, stop)
- Manage scenes and sources
- Window capture with programmatic window selection
- Screenshot capture
- Virtual camera control

## Building

### Prerequisites

- **Windows 10/11**
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [OBS Studio](https://obsproject.com/) with WebSocket server enabled

### Build Commands

```powershell
# Build the project
dotnet build

# Run the server (stdio transport)
dotnet run

# Publish for distribution
dotnet publish -c Release -r win-x64
```

## Standalone Usage with MCP Clients

You can use this MCP server with any MCP-compatible client (Claude Desktop, VS Code with GitHub Copilot, Cursor, Windsurf, etc.) by adding it to your MCP configuration file.

### Configuration File Locations

| Client | Configuration Path |
|--------|-------------------|
| Claude Desktop | `%APPDATA%\Claude\claude_desktop_config.json` |
| VS Code / GitHub Copilot | `.vscode/mcp.json` in your workspace or user settings |
| Cursor | `~/.cursor/mcp.json` |
| Windsurf | `~/.codeium/windsurf/mcp_config.json` |
| Other MCP clients | Check your client's documentation |

### mcp.json Configuration

The configuration format is the same across all MCP-compatible clients. The `inputs` section prompts you for configurable values when the server starts:

| Input ID | Description |
|----------|-------------|
| `obs-server-path` | Path where you built or installed the server executable |
| `obs-project-path` | Path to your cloned repository (for development) |
| `obs-password` | Your OBS WebSocket password (stored securely) |

#### Option 1: Using Published Executable (Recommended)

```json
{
  "inputs": [
    {
      "id": "obs-server-path",
      "type": "promptString",
      "description": "Path to the OBS MCP server executable"
    },
    {
      "id": "obs-password",
      "type": "promptString",
      "description": "OBS WebSocket password",
      "password": true
    }
  ],
  "servers": {
    "obs": {
      "command": "${input:obs-server-path}/Sbroenne.ObsMcp.McpServer.exe",
      "args": [],
      "env": {
        "OBS_HOST": "localhost",
        "OBS_PORT": "4455",
        "OBS_PASSWORD": "${input:obs-password}"
      }
    }
  }
}
```

#### Option 2: Using dotnet run (Development)

```json
{
  "inputs": [
    {
      "id": "obs-project-path",
      "type": "promptString",
      "description": "Path to the mcp-server-obs repository"
    },
    {
      "id": "obs-password",
      "type": "promptString",
      "description": "OBS WebSocket password",
      "password": true
    }
  ],
  "servers": {
    "obs": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "${input:obs-project-path}/src/ObsMcp.McpServer/ObsMcp.McpServer.csproj"
      ],
      "env": {
        "OBS_HOST": "localhost",
        "OBS_PORT": "4455",
        "OBS_PASSWORD": "${input:obs-password}"
      }
    }
  }
}
```

After saving your configuration, restart your MCP client for the changes to take effect.

> **Note:** Use forward slashes (`/`) in paths. JSON requires escaping backslashes (`\\`), so forward slashes are simpler.

## Environment Variables

The server supports the following environment variables for connection configuration:

| Variable | Default | Description |
|----------|---------|-------------|
| `OBS_HOST` | `localhost` | OBS WebSocket host address |
| `OBS_PORT` | `4455` | OBS WebSocket port number |
| `OBS_PASSWORD` | *(none)* | OBS WebSocket password |

These can be set:
- In the `env` block of your `mcp.json` configuration (recommended)
- As system environment variables
- Passed directly to `obs_connect` tool (overrides environment variables)

## Available Tools

The server provides 6 resource-based tools with action enums:

| Tool | Actions | Description |
|------|---------|-------------|
| `obs_connection` | `Connect`, `Disconnect`, `GetStatus`, `GetStats` | Connection management |
| `obs_recording` | `Start`, `Stop`, `Pause`, `Resume`, `GetStatus`, `GetSettings`, `SetFormat`, `SetQuality` | Recording control |
| `obs_streaming` | `Start`, `Stop`, `GetStatus` | Streaming control |
| `obs_scene` | `List`, `GetCurrent`, `Set`, `ListSources` | Scene management |
| `obs_source` | `AddWindowCapture`, `ListWindows`, `SetWindowCapture`, `Remove`, `SetEnabled` | Source management |
| `obs_media` | `SaveScreenshot`, `StartVirtualCamera`, `StopVirtualCamera` | Media operations |

## Window Capture Workflow

The primary use case is recording specific application windows. Follow these steps:

1. **Connect to OBS**:
   ```
   obs_connection(action: Connect, host: "localhost", port: 4455, password: "your_password")
   ```

2. **Add a window capture source**:
   ```
   obs_source(action: AddWindowCapture, sourceName: "My Capture")
   ```

3. **List available windows**:
   ```
   obs_source(action: ListWindows, sourceName: "My Capture")
   ```
   Returns:
   ```
   1. [Code.exe]: Visual Studio Code
      Value: Code.exe:Chrome_WidgetWin_1:Code.exe
   2. [chrome.exe]: Google Chrome
      Value: chrome.exe:Chrome_WidgetWin_1:chrome.exe
   ```

4. **Select the window to capture**:
   ```
   obs_source(action: SetWindowCapture, sourceName: "My Capture", windowValue: "Code.exe:Chrome_WidgetWin_1:Code.exe")
   ```

5. **Start recording**:
   ```
   obs_recording(action: Start)
   ```

## OBS WebSocket Setup

1. Open OBS Studio
2. Go to **Tools → WebSocket Server Settings**
3. Enable **WebSocket server**
4. Note the port (default: 4455)
5. If authentication is enabled, note the password

## Project Structure

```
ObsMcp.McpServer/
├── Program.cs                 # Entry point, MCP server configuration
├── ObsClient.cs               # OBS WebSocket client wrapper
├── Tools/
│   ├── ObsConnectionTool.cs   # obs_connection tool (Connect, Disconnect, GetStatus, GetStats)
│   ├── ObsRecordingTool.cs    # obs_recording tool (Start, Stop, Pause, Resume, etc.)
│   ├── ObsStreamingTool.cs    # obs_streaming tool (Start, Stop, GetStatus)
│   ├── ObsSceneTool.cs        # obs_scene tool (List, GetCurrent, Set, ListSources)
│   ├── ObsSourceTool.cs       # obs_source tool (AddWindowCapture, ListWindows, etc.)
│   └── ObsMediaTool.cs        # obs_media tool (SaveScreenshot, VirtualCamera)
└── README.md                  # This file
```

## Dependencies

- [ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol) - MCP SDK for .NET
- [obs-websocket-dotnet](https://www.nuget.org/packages/obs-websocket-dotnet) - OBS WebSocket client

## License

MIT
