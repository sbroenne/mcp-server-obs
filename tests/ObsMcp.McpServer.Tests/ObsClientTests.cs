using Sbroenne.ObsMcp.McpServer.Tools;
using Xunit;

namespace Sbroenne.ObsMcp.McpServer.Tests;

/// <summary>
/// Unit tests for OBS data transfer objects (DTOs).
/// These tests verify that the status and info types work correctly.
/// Tool behavior is tested via MCP protocol in McpServerIntegrationTests.
/// </summary>
public class ObsStatusTypesTests
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
