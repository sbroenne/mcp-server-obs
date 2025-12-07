# OBS MCP Server - Usage Instructions

This MCP server controls OBS Studio for screen recording and streaming.

## Available Tools

| Tool | Actions |
|------|---------|
| `obs_connection` | Connect, Disconnect, GetStatus, GetStats |
| `obs_recording` | Start, Stop, Pause, Resume, GetStatus, GetSettings, SetFormat, SetQuality |
| `obs_streaming` | Start, Stop, GetStatus |
| `obs_scene` | List, GetCurrent, Set, ListSources |
| `obs_source` | AddWindowCapture, ListWindows, SetWindowCapture, Remove, SetEnabled |
| `obs_media` | SaveScreenshot, StartVirtualCamera, StopVirtualCamera |

## CRITICAL: Prevent Black Screen Recordings

Before recording, you MUST add a capture source. Without one, recordings will be BLACK!

## Window Capture Workflow (Required for Recording)

Follow these steps IN ORDER:

1. **Connect** (if not connected):
   ```
   obs_connection(action: Connect)
   ```

2. **Add window capture source**:
   ```
   obs_source(action: AddWindowCapture, sourceName: "Window Capture")
   ```

3. **List available windows** and ask user which to record:
   ```
   obs_source(action: ListWindows, sourceName: "Window Capture")
   ```
   Returns windows with their capture values.

4. **Set the window** using the exact value from step 3:
   ```
   obs_source(action: SetWindowCapture, sourceName: "Window Capture", windowValue: "Code.exe:Chrome_WidgetWin_1:Code.exe")
   ```

5. **Start recording**:
   ```
   obs_recording(action: Start)
   ```

## Recording Control

- `obs_recording(action: Start)` - Begin recording
- `obs_recording(action: Stop)` - Stop and save
- `obs_recording(action: Pause)` - Pause recording
- `obs_recording(action: Resume)` - Resume recording
- `obs_recording(action: GetStatus)` - Check if recording

## Recording Settings

- `obs_recording(action: GetSettings)` - View format/quality/encoder
- `obs_recording(action: SetFormat, format: "mp4")` - mp4, mkv, mov, flv, ts
- `obs_recording(action: SetQuality, quality: "HQ")` - Stream, Small, HQ, Lossless

## Scene Management

- `obs_scene(action: List)` - List all scenes
- `obs_scene(action: GetCurrent)` - Get current scene
- `obs_scene(action: Set, sceneName: "Scene")` - Switch scene
- `obs_scene(action: ListSources)` - List sources in current scene

## Key Guidelines

1. **Always connect first** before any other operation
2. **Always add a capture source** before recording
3. **Always list windows** and let user choose which to record
4. **Use exact window values** from ListWindows output
5. **Check status** if unsure about current state
