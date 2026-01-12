using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace Sbroenne.ObsMcp.McpServer.Prompts;

/// <summary>
/// Prompts for OBS recording workflows
/// </summary>
[McpServerPromptType]
public static class ObsRecordingPrompts
{
    /// <summary>
    /// Step-by-step guide for recording a specific application window in OBS.
    /// Use this when the user wants to record a specific application like VS Code, Chrome, or Terminal.
    /// </summary>
    /// <param name="applicationName">The name of the application to record (e.g., 'VS Code', 'Chrome', 'Terminal')</param>
    [McpServerPrompt(Name = "record_window")]
    public static IEnumerable<ChatMessage> RecordWindowWorkflow(string? applicationName = null)
    {
        var appText = string.IsNullOrEmpty(applicationName) 
            ? "a specific application window" 
            : $"the {applicationName} window";

        yield return new ChatMessage(
            ChatRole.User,
            $$"""
            I want to record {{appText}} using OBS. Please guide me through the complete workflow:

            1. First, connect to OBS if not already connected
            2. Add a window capture source
            3. List available windows and help me select the right one
            4. Configure the window capture with my selection
            5. Start the recording (with audio muted by default)

            Please execute each step and explain what's happening.
            """);
    }

    /// <summary>
    /// Quick recording setup for screen capture with optimal settings.
    /// Use this when the user wants to quickly start recording their screen or a display.
    /// </summary>
    /// <param name="includeAudio">Whether to include system audio in the recording</param>
    [McpServerPrompt(Name = "quick_screen_record")]
    public static IEnumerable<ChatMessage> QuickScreenRecordWorkflow(bool includeAudio = false)
    {
        var audioText = includeAudio ? "with system audio" : "without audio (silent)";

        yield return new ChatMessage(
            ChatRole.User,
            $$"""
            I want to quickly start a screen recording {{audioText}}. Please:

            1. Connect to OBS
            2. Check current scene has a display or window capture source
            3. Start recording with appropriate audio settings
            4. Confirm recording has started and show the output path

            Keep it simple and fast.
            """);
    }

    /// <summary>
    /// Troubleshooting guide for common OBS recording issues.
    /// Use this when the user is experiencing problems with OBS recording.
    /// </summary>
    /// <param name="issue">Description of the issue (e.g., 'black screen', 'no audio', 'connection failed')</param>
    [McpServerPrompt(Name = "troubleshoot_recording")]
    public static IEnumerable<ChatMessage> TroubleshootRecordingWorkflow(string? issue = null)
    {
        var issueText = string.IsNullOrEmpty(issue) 
            ? "I'm having issues with OBS recording" 
            : $"I'm experiencing this issue: {issue}";

        yield return new ChatMessage(
            ChatRole.User,
            $$"""
            {{issueText}}

            Please help me diagnose and fix the problem:

            1. Check OBS connection status
            2. Verify current scene configuration and sources
            3. Check recording status and settings
            4. Check audio input status
            5. Provide specific recommendations based on what you find

            Common issues include: black screen (no capture source), no audio (muted inputs), 
            connection failures (OBS not running or WebSocket not enabled).
            """);
    }

    /// <summary>
    /// Guide for optimizing OBS recording settings for quality and performance.
    /// Use this when the user wants to configure OBS for better recording quality.
    /// </summary>
    /// <param name="useCase">The type of content being recorded (e.g., 'gaming', 'tutorial', 'presentation')</param>
    [McpServerPrompt(Name = "optimize_settings")]
    public static IEnumerable<ChatMessage> OptimizeSettingsWorkflow(string? useCase = null)
    {
        var useCaseText = string.IsNullOrEmpty(useCase) 
            ? "general purpose" 
            : useCase;

        yield return new ChatMessage(
            ChatRole.User,
            $$"""
            I want to optimize my OBS recording settings for {{useCaseText}} content. Please:

            1. Connect to OBS and get current recording settings
            2. Analyze the current configuration
            3. Recommend optimal settings for my use case:
               - Best recording format (mp4 vs mkv)
               - Quality preset recommendation
               - Audio configuration

            Then help me apply the recommended changes.
            """);
    }
}
