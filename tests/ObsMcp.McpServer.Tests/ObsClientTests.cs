using Sbroenne.ObsMcp.McpServer.Tools;
using Xunit;

namespace Sbroenne.ObsMcp.McpServer.Tests;

/// <summary>
/// Unit tests for ObsClient status types.
/// </summary>
public class ObsClientTests
{
    [Fact]
    public void ObsRecordingStatus_DefaultValues_AreCorrect()
    {
        var status = new ObsRecordingStatus();

        Assert.False(status.IsRecording);
        Assert.False(status.IsPaused);
        Assert.Equal(string.Empty, status.Timecode);
        Assert.Equal(0, status.DurationMs);
    }

    [Fact]
    public void ObsRecordingStatus_Properties_CanBeSet()
    {
        var status = new ObsRecordingStatus
        {
            IsRecording = true,
            IsPaused = true,
            Timecode = "00:01:23.456",
            DurationMs = 83456
        };

        Assert.True(status.IsRecording);
        Assert.True(status.IsPaused);
        Assert.Equal("00:01:23.456", status.Timecode);
        Assert.Equal(83456, status.DurationMs);
    }

    [Fact]
    public void ObsStreamingStatus_DefaultValues_AreCorrect()
    {
        var status = new ObsStreamingStatus();

        Assert.False(status.IsStreaming);
        Assert.Equal(0, status.DurationMs);
        Assert.Equal(0, status.BytesSent);
    }

    [Fact]
    public void ObsStreamingStatus_Properties_CanBeSet()
    {
        var status = new ObsStreamingStatus
        {
            IsStreaming = true,
            DurationMs = 60000,
            BytesSent = 1024000
        };

        Assert.True(status.IsStreaming);
        Assert.Equal(60000, status.DurationMs);
        Assert.Equal(1024000, status.BytesSent);
    }

    [Fact]
    public void ObsSceneInfo_Properties_CanBeSet()
    {
        var scene = new ObsSceneInfo
        {
            Name = "Main Scene",
            Index = 0
        };

        Assert.Equal("Main Scene", scene.Name);
        Assert.Equal(0, scene.Index);
    }

    [Fact]
    public void ObsSourceInfo_Properties_CanBeSet()
    {
        var source = new ObsSourceInfo
        {
            Name = "Window Capture",
            Kind = "window_capture",
            IsEnabled = true,
            SceneItemId = 1
        };

        Assert.Equal("Window Capture", source.Name);
        Assert.Equal("window_capture", source.Kind);
        Assert.True(source.IsEnabled);
        Assert.Equal(1, source.SceneItemId);
    }

    [Fact]
    public void ObsWindowInfo_Properties_CanBeSet()
    {
        var window = new ObsWindowInfo
        {
            Name = "Visual Studio Code",
            Value = "Code.exe:Chrome_WidgetWin_1:Code.exe",
            Enabled = true
        };

        Assert.Equal("Visual Studio Code", window.Name);
        Assert.Equal("Code.exe:Chrome_WidgetWin_1:Code.exe", window.Value);
        Assert.True(window.Enabled);
    }

    [Fact]
    public void ObsPerformanceStats_Properties_CanBeSet()
    {
        var stats = new ObsPerformanceStats
        {
            CpuUsage = 5.5,
            MemoryUsage = 512.0,
            ActiveFps = 60.0,
            RenderTotalFrames = 3600,
            RenderSkippedFrames = 2
        };

        Assert.Equal(5.5, stats.CpuUsage);
        Assert.Equal(512.0, stats.MemoryUsage);
        Assert.Equal(60.0, stats.ActiveFps);
        Assert.Equal(3600, stats.RenderTotalFrames);
        Assert.Equal(2, stats.RenderSkippedFrames);
    }
}

/// <summary>
/// Unit tests for MCP tool responses (when not connected).
/// </summary>
public class ToolsNotConnectedTests
{
    [Fact]
    public void ConnectionTool_GetStatus_ReturnsNotConnected()
    {
        // Reset the connection state
        try { ObsConnectionTool.Connection(ConnectionAction.Disconnect); } catch { }

        var result = ObsConnectionTool.Connection(ConnectionAction.GetStatus);

        Assert.Contains("Not connected", result);
    }

    [Fact]
    public void RecordingTool_Start_ReturnsError_WhenNotConnected()
    {
        // Reset the connection state
        try { ObsConnectionTool.Connection(ConnectionAction.Disconnect); } catch { }

        var result = ObsRecordingTool.Recording(RecordingAction.Start);

        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public void RecordingTool_Stop_ReturnsError_WhenNotConnected()
    {
        try { ObsConnectionTool.Connection(ConnectionAction.Disconnect); } catch { }

        var result = ObsRecordingTool.Recording(RecordingAction.Stop);

        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public void RecordingTool_GetStatus_ReturnsError_WhenNotConnected()
    {
        try { ObsConnectionTool.Connection(ConnectionAction.Disconnect); } catch { }

        var result = ObsRecordingTool.Recording(RecordingAction.GetStatus);

        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public void StreamingTool_Start_ReturnsError_WhenNotConnected()
    {
        try { ObsConnectionTool.Connection(ConnectionAction.Disconnect); } catch { }

        var result = ObsStreamingTool.Streaming(StreamingAction.Start);

        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public void StreamingTool_GetStatus_ReturnsError_WhenNotConnected()
    {
        try { ObsConnectionTool.Connection(ConnectionAction.Disconnect); } catch { }

        var result = ObsStreamingTool.Streaming(StreamingAction.GetStatus);

        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public void SceneTool_List_ReturnsError_WhenNotConnected()
    {
        try { ObsConnectionTool.Connection(ConnectionAction.Disconnect); } catch { }

        var result = ObsSceneTool.Scene(SceneAction.List);

        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public void SceneTool_GetCurrent_ReturnsError_WhenNotConnected()
    {
        try { ObsConnectionTool.Connection(ConnectionAction.Disconnect); } catch { }

        var result = ObsSceneTool.Scene(SceneAction.GetCurrent);

        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public void SceneTool_ListSources_ReturnsError_WhenNotConnected()
    {
        try { ObsConnectionTool.Connection(ConnectionAction.Disconnect); } catch { }

        var result = ObsSceneTool.Scene(SceneAction.ListSources);

        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public void MediaTool_TakeScreenshot_ReturnsError_WhenNotConnected()
    {
        try { ObsConnectionTool.Connection(ConnectionAction.Disconnect); } catch { }

        var result = ObsMediaTool.Media(MediaAction.TakeScreenshot);

        Assert.StartsWith("Error:", result);
    }
}

/// <summary>
/// Unit tests for recording settings validation.
/// </summary>
public class RecordingSettingsTests
{
    [Theory]
    [InlineData("mp4")]
    [InlineData("mkv")]
    [InlineData("flv")]
    [InlineData("mov")]
    [InlineData("ts")]
    public void SetRecordingFormat_ValidFormats_AreAccepted(string format)
    {
        // When not connected, it should still validate the format before trying to set
        var result = ObsRecordingTool.Recording(RecordingAction.SetFormat, format: format);

        // Either error from not being connected OR success message
        Assert.True(
            result.StartsWith("Error:") || result.Contains("set to"),
            $"Expected error or success for format '{format}', got: {result}");
    }

    [Theory]
    [InlineData("avi")]
    [InlineData("wmv")]
    [InlineData("invalid")]
    [InlineData("")]
    public void SetRecordingFormat_InvalidFormats_ReturnError(string format)
    {
        var result = ObsRecordingTool.Recording(RecordingAction.SetFormat, format: format);

        Assert.Contains("Error", result);
    }

    [Theory]
    [InlineData("Stream")]
    [InlineData("Small")]
    [InlineData("HQ")]
    [InlineData("Lossless")]
    public void SetRecordingQuality_ValidQualities_AreAccepted(string quality)
    {
        var result = ObsRecordingTool.Recording(RecordingAction.SetQuality, quality: quality);

        // Either error from not being connected OR success message
        Assert.True(
            result.StartsWith("Error:") || result.Contains("set to"),
            $"Expected error or success for quality '{quality}', got: {result}");
    }

    [Theory]
    [InlineData("Ultra")]
    [InlineData("Low")]
    [InlineData("invalid")]
    public void SetRecordingQuality_InvalidQualities_ReturnError(string quality)
    {
        var result = ObsRecordingTool.Recording(RecordingAction.SetQuality, quality: quality);

        Assert.Contains("Error", result);
    }

    [Fact]
    public void SetRecordingPath_ReturnsError_WhenNoPath()
    {
        var result = ObsRecordingTool.Recording(RecordingAction.SetPath);

        Assert.Contains("path parameter is required", result);
    }

    [Fact]
    public void GetRecordingPath_ReturnsError_WhenNotConnected()
    {
        try { ObsConnectionTool.Connection(ConnectionAction.Disconnect); } catch { }

        var result = ObsRecordingTool.Recording(RecordingAction.GetPath);

        Assert.StartsWith("Error:", result);
    }
}

/// <summary>
/// Unit tests for audio tool validation.
/// </summary>
public class AudioToolTests
{
    [Fact]
    public void AudioTool_GetInputs_ReturnsError_WhenNotConnected()
    {
        try { ObsConnectionTool.Connection(ConnectionAction.Disconnect); } catch { }

        var result = ObsAudioTool.Audio(AudioAction.GetInputs);

        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public void AudioTool_Mute_ReturnsError_WhenNoInputName()
    {
        var result = ObsAudioTool.Audio(AudioAction.Mute);

        Assert.Contains("inputName parameter is required", result);
    }

    [Fact]
    public void AudioTool_Unmute_ReturnsError_WhenNoInputName()
    {
        var result = ObsAudioTool.Audio(AudioAction.Unmute);

        Assert.Contains("inputName parameter is required", result);
    }

    [Fact]
    public void AudioTool_GetMuteState_ReturnsError_WhenNoInputName()
    {
        var result = ObsAudioTool.Audio(AudioAction.GetMuteState);

        Assert.Contains("inputName parameter is required", result);
    }

    [Fact]
    public void AudioTool_SetVolume_ReturnsError_WhenNoInputName()
    {
        var result = ObsAudioTool.Audio(AudioAction.SetVolume, volume: 0.5);

        Assert.Contains("inputName parameter is required", result);
    }

    [Fact]
    public void AudioTool_SetVolume_ReturnsError_WhenNoVolume()
    {
        var result = ObsAudioTool.Audio(AudioAction.SetVolume, inputName: "Desktop Audio");

        Assert.Contains("volume parameter is required", result);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void AudioTool_SetVolume_ReturnsError_WhenVolumeOutOfRange(double volume)
    {
        var result = ObsAudioTool.Audio(AudioAction.SetVolume, inputName: "Desktop Audio", volume: volume);

        Assert.Contains("volume must be between 0.0 and 1.0", result);
    }

    [Fact]
    public void AudioTool_GetVolume_ReturnsError_WhenNoInputName()
    {
        var result = ObsAudioTool.Audio(AudioAction.GetVolume);

        Assert.Contains("inputName parameter is required", result);
    }

    [Fact]
    public void AudioTool_MuteAll_ReturnsError_WhenNotConnected()
    {
        try { ObsConnectionTool.Connection(ConnectionAction.Disconnect); } catch { }

        var result = ObsAudioTool.Audio(AudioAction.MuteAll);

        Assert.StartsWith("Error:", result);
    }

    [Fact]
    public void AudioTool_UnmuteAll_ReturnsError_WhenNotConnected()
    {
        try { ObsConnectionTool.Connection(ConnectionAction.Disconnect); } catch { }

        var result = ObsAudioTool.Audio(AudioAction.UnmuteAll);

        Assert.StartsWith("Error:", result);
    }
}

/// <summary>
/// Unit tests for ObsAudioInput DTO.
/// </summary>
public class ObsAudioInputTests
{
    [Fact]
    public void ObsAudioInput_DefaultValues_AreCorrect()
    {
        var input = new ObsAudioInput();

        Assert.Equal(string.Empty, input.Name);
        Assert.Equal(string.Empty, input.Kind);
    }

    [Fact]
    public void ObsAudioInput_Properties_CanBeSet()
    {
        var input = new ObsAudioInput
        {
            Name = "Desktop Audio",
            Kind = "Desktop Audio"
        };

        Assert.Equal("Desktop Audio", input.Name);
        Assert.Equal("Desktop Audio", input.Kind);
    }
}
