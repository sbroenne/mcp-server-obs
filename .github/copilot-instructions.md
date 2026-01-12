# OBS Studio MCP Server - Development Instructions

## Project Overview

This is a VS Code extension that provides an MCP (Model Context Protocol) server for controlling OBS Studio. The MCP server is implemented in .NET 10, and the VS Code extension is a thin TypeScript wrapper that registers the server.

## Architecture

```
mcp-server-obs/
├── src/ObsMcp.McpServer/       # .NET 10 MCP Server (main codebase)
│   ├── Program.cs              # Entry point with server instructions
│   ├── ObsClient.cs            # OBS WebSocket client wrapper
│   └── Tools/                  # 7 MCP tools with action enums
├── tests/ObsMcp.McpServer.Tests/  # Unit & integration tests
├── vscode-extension/           # VS Code extension (TypeScript)
│   └── src/extension.ts        # Registers MCP server with VS Code
└── ObsMcp.sln                  # .NET solution file
```

## Key Technologies

- **.NET 10** - MCP server implementation
- **ModelContextProtocol** NuGet package - MCP SDK for .NET
- **obs-websocket-dotnet** - OBS WebSocket client library
- **xUnit** - Unit and integration testing
- **TypeScript** - VS Code extension only

## MCP Tool Pattern

Tools follow the **resource + action enum** pattern (like mcp-server-excel):

| Tool | Actions |
|------|---------|
| `obs_connection` | Connect, Disconnect, GetStatus, GetStats |
| `obs_recording` | Start, Stop, Pause, Resume, GetStatus, GetSettings, SetFormat, SetQuality |
| `obs_streaming` | Start, Stop, GetStatus |
| `obs_scene` | List, GetCurrent, Set, ListSources |
| `obs_source` | AddWindowCapture, ListWindows, SetWindowCapture, Remove, SetEnabled |
| `obs_audio` | GetInputs, Mute, Unmute, GetMuteState, SetVolume, GetVolume, MuteAll, UnmuteAll |
| `obs_media` | SaveScreenshot, StartVirtualCamera, StopVirtualCamera |

**Audio Note:** Recording starts with audio MUTED by default. Use `muteAudio=false` to include audio.

Each tool file contains:
- An `enum` for actions (e.g., `ConnectionAction`)
- A single `[McpServerTool]` method that switches on the action
- Shared client access via `ObsConnectionTool.GetClient()`

## Development Commands

```powershell
# Build
dotnet build

# Run all tests (unit + integration)
dotnet test

# Run unit tests only (no OBS required)
dotnet test --filter "Category!=Integration"

# Run integration tests (requires OBS running with .env configured)
dotnet test --filter "Category=Integration"

# Publish for VS Code extension
dotnet publish src/ObsMcp.McpServer -c Release -r win-x64 --self-contained -o vscode-extension/server
```

## Testing

### Unit Tests
- Test tool validation logic without OBS connection
- Located in `tests/ObsMcp.McpServer.Tests/ObsClientTests.cs`

### Integration Tests
- Require OBS Studio running with WebSocket enabled
- Configure `.env` file with `OBS_PASSWORD`
- Located in `tests/ObsMcp.McpServer.Tests/McpServerIntegrationTests.cs`
- **MUST fail if OBS is not running** - never silently skip or return early
- Mark with `[Trait("Category", "Integration")]`

### Test Requirements
- **Integration tests MUST fail clearly** if prerequisites (like OBS) are not available
- Never use `return;` to skip tests silently - use proper assertions that fail
- All tests must have clear failure messages explaining what went wrong

## Git Workflow

- **Never commit or push without explicit user consent**
- Always ask before running `git commit` or `git push`
- Create issues and PRs for changes - do not commit directly to main

## Code Style

- C# 12 with nullable reference types (`<Nullable>enable</Nullable>`)
- PascalCase for public members, camelCase for private
- Each tool in its own file: `Obs{Resource}Tool.cs`
- Action enums: `{Resource}Action`
- Handle errors with try/catch, return `"Error: {message}"` strings
- Use XML documentation (`///`) for MCP tool and parameter descriptions (SDK auto-extracts these)

## Adding New Tools

1. Create `src/ObsMcp.McpServer/Tools/Obs{Resource}Tool.cs`
2. Define `{Resource}Action` enum with actions (use `/// <summary>` on each value)
3. Add `[McpServerTool]` method with:
   - `/// <summary>` for tool description
   - `/// <param name="...">` for each parameter description
4. Use `ObsConnectionTool.GetClient()` for OBS access
5. Add unit tests in `ObsClientTests.cs`
6. Add integration tests in `IntegrationTests.cs`
7. Update `Program.cs` ServerInstructions
