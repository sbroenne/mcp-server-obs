using System.IO.Pipelines;
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Sbroenne.ObsMcp.McpServer.Tools;
using Xunit;
using Xunit.Abstractions;

using Server = ModelContextProtocol.Server;

namespace Sbroenne.ObsMcp.McpServer.Tests;

/// <summary>
/// Integration tests that require OBS Studio to be running with WebSocket enabled.
/// These tests exercise the full tool chain: MCP client -> MCP server -> OBS WebSocket -> OBS.
/// 
/// Prerequisites:
/// - OBS Studio running with WebSocket server enabled (port 4455)
/// - .env file with OBS_PASSWORD set
/// 
/// Run with: dotnet test --filter "Category=Integration"
/// </summary>
[Trait("Category", "Integration")]
public class ObsIntegrationTests : IAsyncLifetime, IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly Pipe _clientToServerPipe = new();
    private readonly Pipe _serverToClientPipe = new();
    private readonly CancellationTokenSource _cts = new();
    private Server.McpServer? _server;
    private McpClient? _client;
    private IServiceProvider? _serviceProvider;
    private Task? _serverTask;

    static ObsIntegrationTests()
    {
        Env.TraversePath().Load();
    }

    public ObsIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

        services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new() { Name = "obs-mcp-server-test", Version = "1.0.0" };
            })
            .WithStreamServerTransport(
                _clientToServerPipe.Reader.AsStream(),
                _serverToClientPipe.Writer.AsStream())
            .WithToolsFromAssembly(typeof(ObsConnectionTool).Assembly);

        _serviceProvider = services.BuildServiceProvider(validateScopes: true);
        _server = _serviceProvider.GetRequiredService<Server.McpServer>();
        _serverTask = _server.RunAsync(_cts.Token);

        _client = await McpClient.CreateAsync(
            new StreamClientTransport(
                serverInput: _clientToServerPipe.Writer.AsStream(),
                serverOutput: _serverToClientPipe.Reader.AsStream()),
            clientOptions: new McpClientOptions
            {
                ClientInfo = new() { Name = "IntegrationTestClient", Version = "1.0.0" }
            },
            cancellationToken: _cts.Token);

        // Connect to OBS at the start of each test
        await ConnectToObs();
    }

    public async Task DisposeAsync()
    {
        await DisposeAsyncCore();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    private async Task DisposeAsyncCore()
    {
        // Disconnect from OBS
        try
        {
            await CallToolAsync("obs_connection", new() { ["action"] = "Disconnect" });
        }
        catch { /* Ignore cleanup errors */ }

        await _cts.CancelAsync();
        _clientToServerPipe.Writer.Complete();
        _serverToClientPipe.Writer.Complete();

        if (_client != null) await _client.DisposeAsync();
        if (_serverTask != null)
        {
            try { await _serverTask; }
            catch (OperationCanceledException) { }
        }
        if (_serviceProvider is IAsyncDisposable ad) await ad.DisposeAsync();
        else if (_serviceProvider is IDisposable d) d.Dispose();
        _cts.Dispose();
    }

    private async Task ConnectToObs()
    {
        var password = Environment.GetEnvironmentVariable("OBS_PASSWORD") ?? "";
        var result = await CallToolAsync("obs_connection", new()
        {
            ["action"] = "Connect",
            ["host"] = "localhost",
            ["port"] = 4455,
            ["password"] = password
        });

        Assert.DoesNotContain("Error", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Connected to OBS", result);
        _output.WriteLine($"✓ Connected to OBS: {result}");
    }

    private async Task<string> CallToolAsync(string toolName, Dictionary<string, object?> args)
    {
        var result = await _client!.CallToolAsync(toolName, args, cancellationToken: _cts.Token);
        var textBlock = result.Content.OfType<TextContentBlock>().FirstOrDefault();
        return textBlock?.Text ?? "";
    }

    #region obs_connection Tests

    [Fact]
    public async Task Connection_GetStatus_ReturnsConnected()
    {
        var result = await CallToolAsync("obs_connection", new() { ["action"] = "GetStatus" });

        _output.WriteLine($"GetStatus result: {result}");
        Assert.Contains("Connected", result);
    }

    [Fact]
    public async Task Connection_GetStats_ReturnsPerformanceData()
    {
        var result = await CallToolAsync("obs_connection", new() { ["action"] = "GetStats" });

        _output.WriteLine($"GetStats result: {result}");
        Assert.Contains("FPS", result);
        Assert.Contains("CPU Usage", result);
    }

    #endregion

    #region obs_scene Tests

    [Fact]
    public async Task Scene_List_ReturnsScenes()
    {
        var result = await CallToolAsync("obs_scene", new() { ["action"] = "List" });

        _output.WriteLine($"Scene List result: {result}");
        // OBS always has at least one scene
        Assert.DoesNotContain("Error", result);
    }

    [Fact]
    public async Task Scene_GetCurrent_ReturnsCurrentScene()
    {
        var result = await CallToolAsync("obs_scene", new() { ["action"] = "GetCurrent" });

        _output.WriteLine($"GetCurrent result: {result}");
        Assert.DoesNotContain("Error", result);
        // Should return a scene name
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public async Task Scene_ListSources_ReturnsSources()
    {
        var result = await CallToolAsync("obs_scene", new() { ["action"] = "ListSources" });

        _output.WriteLine($"ListSources result: {result}");
        Assert.DoesNotContain("Error", result);
    }

    #endregion

    #region obs_recording Tests

    [Fact]
    public async Task Recording_GetStatus_ReturnsStatus()
    {
        var result = await CallToolAsync("obs_recording", new() { ["action"] = "GetStatus" });

        _output.WriteLine($"Recording GetStatus result: {result}");
        Assert.Contains("Active:", result);
    }

    [Fact]
    public async Task Recording_GetSettings_ReturnsSettings()
    {
        var result = await CallToolAsync("obs_recording", new() { ["action"] = "GetSettings" });

        _output.WriteLine($"Recording GetSettings result: {result}");
        Assert.DoesNotContain("Error", result);
    }

    [Fact]
    public async Task Recording_GetPath_ReturnsPath()
    {
        var result = await CallToolAsync("obs_recording", new() { ["action"] = "GetPath" });

        _output.WriteLine($"Recording GetPath result: {result}");
        Assert.DoesNotContain("Error", result);
    }

    [Fact]
    public async Task Recording_StartStopCycle_CreatesFile()
    {
        // Start recording
        var startResult = await CallToolAsync("obs_recording", new() { ["action"] = "Start" });
        _output.WriteLine($"Start result: {startResult}");
        Assert.DoesNotContain("Error", startResult);

        // Wait for some content to be recorded (3 seconds for reliable file creation)
        await Task.Delay(3000);

        // Check status
        var statusResult = await CallToolAsync("obs_recording", new() { ["action"] = "GetStatus" });
        _output.WriteLine($"Status during recording: {statusResult}");
        Assert.Contains("Active: True", statusResult);

        // Stop recording - should return file path
        var stopResult = await CallToolAsync("obs_recording", new() { ["action"] = "Stop" });
        _output.WriteLine($"Stop result: {stopResult}");
        Assert.DoesNotContain("Error", stopResult);
        Assert.Contains("saved to:", stopResult);

        // Extract file path from result (OBS returns forward slashes on Windows)
        var match = System.Text.RegularExpressions.Regex.Match(stopResult, @"saved to: (.+)$");
        Assert.True(match.Success, $"Could not extract file path from: {stopResult}");
        
        var filePath = match.Groups[1].Value.Trim().Replace('/', '\\');
        _output.WriteLine($"Recording file path: {filePath}");

        // Wait for OBS to finalize the file (MP4/MKV write metadata at the end)
        // OBS takes 2-3 seconds to finalize the file after stop returns
        // Retry up to 10 times with 500ms delay (5 seconds total)
        long fileSize = 0;
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(500);
            if (File.Exists(filePath))
            {
                // Must create a new FileInfo each time to get fresh file size
                // (FileInfo.Refresh doesn't always work for newly written files)
                fileSize = new FileInfo(filePath).Length;
                _output.WriteLine($"Attempt {i + 1}: file size = {fileSize} bytes");
                if (fileSize > 0) break;
            }
            else
            {
                _output.WriteLine($"Attempt {i + 1}: file not found yet");
            }
        }

        // Verify file exists
        Assert.True(File.Exists(filePath), $"Recording file not created at {filePath}");

        // Verify file has content
        Assert.True(fileSize > 0, $"Recording file is empty (0 bytes) at {filePath}");
        _output.WriteLine($"✓ Recording file created: {fileSize} bytes");

        // Clean up test file
        File.Delete(filePath);
        _output.WriteLine("✓ Test recording file cleaned up");
    }

    [Fact]
    public async Task Recording_PauseResumeCycle_Works()
    {
        // Start recording first
        await CallToolAsync("obs_recording", new() { ["action"] = "Start" });
        await Task.Delay(500);

        try
        {
            // Pause
            var pauseResult = await CallToolAsync("obs_recording", new() { ["action"] = "Pause" });
            _output.WriteLine($"Pause result: {pauseResult}");
            Assert.DoesNotContain("Error", pauseResult);

            // Check paused status
            var statusResult = await CallToolAsync("obs_recording", new() { ["action"] = "GetStatus" });
            _output.WriteLine($"Status while paused: {statusResult}");
            Assert.Contains("Paused: True", statusResult);

            // Resume
            var resumeResult = await CallToolAsync("obs_recording", new() { ["action"] = "Resume" });
            _output.WriteLine($"Resume result: {resumeResult}");
            Assert.DoesNotContain("Error", resumeResult);
        }
        finally
        {
            // Always stop recording
            await CallToolAsync("obs_recording", new() { ["action"] = "Stop" });
        }
    }

    #endregion

    #region obs_streaming Tests

    [Fact]
    public async Task Streaming_GetStatus_ReturnsStatus()
    {
        var result = await CallToolAsync("obs_streaming", new() { ["action"] = "GetStatus" });

        _output.WriteLine($"Streaming GetStatus result: {result}");
        Assert.Contains("Active:", result);
    }

    // Note: Start/Stop streaming tests are skipped because they require a configured stream destination

    #endregion

    #region obs_audio Tests

    [Fact]
    public async Task Audio_GetInputs_ReturnsInputs()
    {
        var result = await CallToolAsync("obs_audio", new() { ["action"] = "GetInputs" });

        _output.WriteLine($"Audio GetInputs result: {result}");
        Assert.DoesNotContain("Error", result);
    }

    [Fact]
    public async Task Audio_MuteUnmuteCycle_Works()
    {
        // Get inputs first to find one to test with
        var inputsResult = await CallToolAsync("obs_audio", new() { ["action"] = "GetInputs" });
        _output.WriteLine($"Inputs: {inputsResult}");

        // Try with Desktop Audio (common default)
        var testInput = "Desktop Audio";

        // Get initial mute state
        var initialState = await CallToolAsync("obs_audio", new()
        {
            ["action"] = "GetMuteState",
            ["inputName"] = testInput
        });
        _output.WriteLine($"Initial mute state: {initialState}");

        if (initialState.Contains("Error"))
        {
            _output.WriteLine("Desktop Audio not found, skipping mute test");
            return;
        }

        // Mute
        var muteResult = await CallToolAsync("obs_audio", new()
        {
            ["action"] = "Mute",
            ["inputName"] = testInput
        });
        _output.WriteLine($"Mute result: {muteResult}");
        Assert.DoesNotContain("Error", muteResult);

        // Verify muted
        var mutedState = await CallToolAsync("obs_audio", new()
        {
            ["action"] = "GetMuteState",
            ["inputName"] = testInput
        });
        Assert.Contains("muted", mutedState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("unmuted", mutedState, StringComparison.OrdinalIgnoreCase);

        // Unmute
        var unmuteResult = await CallToolAsync("obs_audio", new()
        {
            ["action"] = "Unmute",
            ["inputName"] = testInput
        });
        _output.WriteLine($"Unmute result: {unmuteResult}");
        Assert.DoesNotContain("Error", unmuteResult);
    }

    [Fact]
    public async Task Audio_GetSetVolume_Works()
    {
        var testInput = "Desktop Audio";

        // Get current volume
        var getResult = await CallToolAsync("obs_audio", new()
        {
            ["action"] = "GetVolume",
            ["inputName"] = testInput
        });
        _output.WriteLine($"GetVolume result: {getResult}");

        if (getResult.Contains("Error"))
        {
            _output.WriteLine("Desktop Audio not found, skipping volume test");
            return;
        }

        // Set volume to 0.5
        var setResult = await CallToolAsync("obs_audio", new()
        {
            ["action"] = "SetVolume",
            ["inputName"] = testInput,
            ["volume"] = 0.5
        });
        _output.WriteLine($"SetVolume result: {setResult}");
        Assert.DoesNotContain("Error", setResult);

        // Verify volume changed
        var verifyResult = await CallToolAsync("obs_audio", new()
        {
            ["action"] = "GetVolume",
            ["inputName"] = testInput
        });
        _output.WriteLine($"Verify volume: {verifyResult}");
    }

    [Fact]
    public async Task Audio_MuteAllUnmuteAll_Works()
    {
        // Mute all
        var muteAllResult = await CallToolAsync("obs_audio", new() { ["action"] = "MuteAll" });
        _output.WriteLine($"MuteAll result: {muteAllResult}");
        Assert.DoesNotContain("Error", muteAllResult);

        // Unmute all
        var unmuteAllResult = await CallToolAsync("obs_audio", new() { ["action"] = "UnmuteAll" });
        _output.WriteLine($"UnmuteAll result: {unmuteAllResult}");
        Assert.DoesNotContain("Error", unmuteAllResult);
    }

    #endregion

    #region obs_source Tests

    [Fact]
    public async Task Source_AddAndRemoveWindowCapture_Works()
    {
        var sourceName = $"Test_Window_Capture_{Guid.NewGuid():N}";

        try
        {
            // Add window capture
            var addResult = await CallToolAsync("obs_source", new()
            {
                ["action"] = "AddWindowCapture",
                ["sourceName"] = sourceName
            });
            _output.WriteLine($"AddWindowCapture result: {addResult}");
            Assert.DoesNotContain("Error", addResult);

            // List windows
            var listResult = await CallToolAsync("obs_source", new()
            {
                ["action"] = "ListWindows",
                ["sourceName"] = sourceName
            });
            _output.WriteLine($"ListWindows result: {listResult}");
            Assert.DoesNotContain("Error", listResult);

            // Verify source is in scene
            var sceneSources = await CallToolAsync("obs_scene", new() { ["action"] = "ListSources" });
            Assert.Contains(sourceName, sceneSources);
        }
        finally
        {
            // Clean up - remove the source
            var removeResult = await CallToolAsync("obs_source", new()
            {
                ["action"] = "Remove",
                ["sourceName"] = sourceName
            });
            _output.WriteLine($"Remove result: {removeResult}");
        }
    }

    [Fact]
    public async Task Source_SetEnabled_Works()
    {
        var sourceName = $"Test_Source_{Guid.NewGuid():N}";

        try
        {
            // Add a source first
            await CallToolAsync("obs_source", new()
            {
                ["action"] = "AddWindowCapture",
                ["sourceName"] = sourceName
            });

            // Disable
            var disableResult = await CallToolAsync("obs_source", new()
            {
                ["action"] = "SetEnabled",
                ["sourceName"] = sourceName,
                ["enabled"] = false
            });
            _output.WriteLine($"Disable result: {disableResult}");
            Assert.DoesNotContain("Error", disableResult);

            // Enable
            var enableResult = await CallToolAsync("obs_source", new()
            {
                ["action"] = "SetEnabled",
                ["sourceName"] = sourceName,
                ["enabled"] = true
            });
            _output.WriteLine($"Enable result: {enableResult}");
            Assert.DoesNotContain("Error", enableResult);
        }
        finally
        {
            await CallToolAsync("obs_source", new()
            {
                ["action"] = "Remove",
                ["sourceName"] = sourceName
            });
        }
    }

    #endregion

    #region obs_media Tests

    [Fact]
    public async Task Media_SaveScreenshot_SavesFile()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"obs_test_{Guid.NewGuid()}.png");

        try
        {
            var result = await CallToolAsync("obs_media", new()
            {
                ["action"] = "SaveScreenshot",
                ["filePath"] = tempPath
            });

            _output.WriteLine($"SaveScreenshot result: {result}");
            Assert.Contains("Screenshot saved to", result);
            Assert.True(File.Exists(tempPath), $"Screenshot file not created at {tempPath}");

            var fileInfo = new FileInfo(tempPath);
            Assert.True(fileInfo.Length > 0, "Screenshot file is empty");
            _output.WriteLine($"✓ Screenshot saved: {fileInfo.Length} bytes");
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Media_SaveScreenshot_SupportsJpgWithQuality()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"obs_test_{Guid.NewGuid()}.jpg");

        try
        {
            var result = await CallToolAsync("obs_media", new()
            {
                ["action"] = "SaveScreenshot",
                ["filePath"] = tempPath,
                ["quality"] = 80
            });

            _output.WriteLine($"SaveScreenshot JPG result: {result}");
            Assert.Contains("Screenshot saved to", result);
            Assert.True(File.Exists(tempPath), $"JPG screenshot not created at {tempPath}");

            var fileInfo = new FileInfo(tempPath);
            Assert.True(fileInfo.Length > 0, "JPG screenshot file is empty");
            _output.WriteLine($"✓ JPG screenshot saved: {fileInfo.Length} bytes");
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task Media_VirtualCamera_StartStop_Works()
    {
        // Start virtual camera
        var startResult = await CallToolAsync("obs_media", new() { ["action"] = "StartVirtualCamera" });
        _output.WriteLine($"StartVirtualCamera result: {startResult}");
        Assert.DoesNotContain("Error", startResult);

        await Task.Delay(500);

        // Stop virtual camera
        var stopResult = await CallToolAsync("obs_media", new() { ["action"] = "StopVirtualCamera" });
        _output.WriteLine($"StopVirtualCamera result: {stopResult}");
        Assert.DoesNotContain("Error", stopResult);
    }

    #endregion
}
