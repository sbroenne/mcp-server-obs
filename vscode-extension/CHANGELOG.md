# Changelog

All notable changes to the OBS MCP Server VS Code extension will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.3] - 2025-11-25

### Changed
- Restructured project to separate VS Code extension from .NET MCP server
- Switched from webpack to TypeScript compiler for extension build
- Updated build scripts for cleaner separation of concerns

### Fixed
- OBS auto-start now correctly sets working directory to avoid locale errors

## [0.0.2] - 2025-11-25

### Added
- Converted MCP server from Node.js/TypeScript to .NET 8
- Self-contained deployment (no .NET runtime required)
- Window capture tools for programmatic window selection
- `obs_list_windows` - List available windows to capture
- `obs_set_window_capture` - Set which window to capture

### Changed
- Extension now bundles .NET executable instead of Node.js bundle
- Reduced external dependencies

## [0.0.1] - 2025-11-24

### Added
- Initial release
- Connection management (connect, disconnect, status)
- Recording controls (start, stop, pause, resume, status)
- Streaming controls (start, stop, status)
- Scene management (list, get current, switch)
- Source management (list, add display/window capture, remove)
- Screenshot capture
- Virtual camera controls
- Performance statistics
- Recording settings (format, quality)
