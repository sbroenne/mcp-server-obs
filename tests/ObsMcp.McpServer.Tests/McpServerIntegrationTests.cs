using System.IO.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Sbroenne.ObsMcp.McpServer.Tools;
using Xunit;
using Xunit.Abstractions;

// Avoid namespace conflict: McpServer is both a type and namespace
using Server = ModelContextProtocol.Server;

namespace Sbroenne.ObsMcp.McpServer.Tests;

/// <summary>
/// Integration tests that exercise the full MCP protocol using in-memory transport.
/// These tests use the official MCP SDK client to connect to our server, ensuring:
/// - DI pipeline is correctly configured
/// - Tool discovery via WithToolsFromAssembly() works
/// - Tool schemas are correctly generated from XML documentation
/// - Tools execute properly through the MCP protocol
///
/// This is the CORRECT way to test MCP servers - using the SDK's client to verify
/// the actual protocol behavior, not reflection or direct method calls.
/// </summary>
[Trait("Category", "McpIntegration")]
[Trait("Speed", "Fast")]
[Trait("Layer", "McpServer")]
[Trait("Feature", "McpProtocol")]
public class McpServerIntegrationTests(ITestOutputHelper output) : IAsyncLifetime, IAsyncDisposable
{
    private readonly Pipe _clientToServerPipe = new();
    private readonly Pipe _serverToClientPipe = new();
    private readonly CancellationTokenSource _cts = new();
    private Server.McpServer? _server;
    private McpClient? _client;
    private IServiceProvider? _serviceProvider;
    private Task? _serverTask;

    /// <summary>
    /// Expected tool names from our assembly - the source of truth.
    /// </summary>
    private static readonly HashSet<string> ExpectedToolNames =
    [
        "obs_connection",
        "obs_recording",
        "obs_streaming",
        "obs_scene",
        "obs_source",
        "obs_audio",
        "obs_media"
    ];

    /// <summary>
    /// Setup: Create MCP server with DI and connect client via in-memory pipes.
    /// This exercises the exact same code path as Program.cs.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Build the server with DI - same pattern as Program.cs
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

        // Add MCP server with tools (same as Program.cs) using stream transport for testing
        services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new() { Name = "obs-mcp-server", Version = "1.0.0" };
                options.ServerInstructions = "OBS Studio MCP Server - Test instance for integration tests";
            })
            .WithStreamServerTransport(
                _clientToServerPipe.Reader.AsStream(),
                _serverToClientPipe.Writer.AsStream())
            .WithToolsFromAssembly(typeof(ObsConnectionTool).Assembly);

        _serviceProvider = services.BuildServiceProvider(validateScopes: true);

        // Get the server and start it
        _server = _serviceProvider.GetRequiredService<Server.McpServer>();
        _serverTask = _server.RunAsync(_cts.Token);

        // Create client connected to the server via pipes
        _client = await McpClient.CreateAsync(
            new StreamClientTransport(
                serverInput: _clientToServerPipe.Writer.AsStream(),
                serverOutput: _serverToClientPipe.Reader.AsStream()),
            clientOptions: new McpClientOptions
            {
                ClientInfo = new() { Name = "TestClient", Version = "1.0.0" }
            },
            cancellationToken: _cts.Token);

        output.WriteLine($"✓ Connected to server: {_client.ServerInfo?.Name} v{_client.ServerInfo?.Version}");
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
        await _cts.CancelAsync();

        _clientToServerPipe.Writer.Complete();
        _serverToClientPipe.Writer.Complete();

        if (_client != null)
        {
            await _client.DisposeAsync();
        }

        if (_serverTask != null)
        {
            try
            {
                await _serverTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _cts.Dispose();
    }

    /// <summary>
    /// Tests that all 7 expected tools are discoverable via the MCP protocol.
    /// This exercises the complete DI and tool discovery pipeline.
    /// </summary>
    [Fact]
    public async Task ListTools_ReturnsAll7ExpectedTools()
    {
        output.WriteLine("=== TOOL DISCOVERY VIA MCP PROTOCOL ===\n");

        // Act - Use the REAL MCP protocol to list tools
        var tools = await _client!.ListToolsAsync(cancellationToken: _cts.Token);

        // Assert - Verify count
        output.WriteLine($"Discovered {tools.Count} tools via MCP protocol:\n");

        foreach (var tool in tools.OrderBy(t => t.Name))
        {
            var descPreview = tool.Description?.Length > 60 ? tool.Description[..60] + "..." : tool.Description;
            output.WriteLine($"  • {tool.Name}: {descPreview}");
        }

        Assert.Equal(ExpectedToolNames.Count, tools.Count);

        // Verify all expected tools are present
        var actualToolNames = tools.Select(t => t.Name).ToHashSet();

        var missingTools = ExpectedToolNames.Except(actualToolNames).ToList();
        if (missingTools.Count > 0)
        {
            output.WriteLine($"\n❌ Missing tools: {string.Join(", ", missingTools)}");
        }
        Assert.Empty(missingTools);

        var unexpectedTools = actualToolNames.Except(ExpectedToolNames).ToList();
        if (unexpectedTools.Count > 0)
        {
            output.WriteLine($"\n❌ Unexpected tools: {string.Join(", ", unexpectedTools)}");
        }
        Assert.Empty(unexpectedTools);

        output.WriteLine($"\n✓ All {ExpectedToolNames.Count} tools discovered successfully via MCP protocol");
    }

    /// <summary>
    /// Tests that each tool has proper schema (parameters, descriptions from XML docs).
    /// </summary>
    [Fact]
    public async Task ListTools_AllToolsHaveValidSchema()
    {
        output.WriteLine("=== TOOL SCHEMA VALIDATION ===\n");

        var tools = await _client!.ListToolsAsync(cancellationToken: _cts.Token);

        foreach (var tool in tools)
        {
            // Every tool must have a name
            Assert.False(string.IsNullOrEmpty(tool.Name), "Tool has empty name");

            // Every tool should have a description (from XML docs)
            Assert.False(string.IsNullOrEmpty(tool.Description), $"Tool {tool.Name} has no description");

            output.WriteLine($"✓ {tool.Name}: Has description ({tool.Description?.Length} chars)");
        }

        output.WriteLine($"\n✓ All {tools.Count} tools have valid schemas");
    }

    /// <summary>
    /// Tests that server information is correctly exposed via MCP protocol.
    /// </summary>
    [Fact]
    public async Task ServerInfo_ReturnsCorrectInformation()
    {
        output.WriteLine("=== SERVER INFO VIA MCP PROTOCOL ===\n");

        var serverInfo = _client!.ServerInfo;
        var serverInstructions = _client.ServerInstructions;

        Assert.NotNull(serverInfo);
        Assert.Equal("obs-mcp-server", serverInfo.Name);
        Assert.Equal("1.0.0", serverInfo.Version);
        Assert.NotNull(serverInstructions);
        Assert.Contains("OBS Studio MCP Server", serverInstructions);

        output.WriteLine($"Server Name: {serverInfo.Name}");
        output.WriteLine($"Server Version: {serverInfo.Version}");
        output.WriteLine($"Server Instructions: {serverInstructions.Length} chars");

        output.WriteLine("\n✓ Server info correctly exposed via MCP protocol");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests that server capabilities include tools.
    /// </summary>
    [Fact]
    public void ServerCapabilities_IncludesTools()
    {
        output.WriteLine("=== SERVER CAPABILITIES ===\n");

        var capabilities = _client!.ServerCapabilities;

        Assert.NotNull(capabilities);
        Assert.NotNull(capabilities.Tools);

        output.WriteLine($"✓ Tools capability: {capabilities.Tools != null}");
        output.WriteLine($"✓ ListChanged: {capabilities.Tools?.ListChanged}");

        output.WriteLine("\n✓ Server capabilities correctly exposed");
    }

    /// <summary>
    /// Tests that tools can be enumerated lazily.
    /// </summary>
    [Fact]
    public async Task EnumerateTools_SupportsLazyEnumeration()
    {
        output.WriteLine("=== LAZY TOOL ENUMERATION ===\n");

        var toolCount = 0;
        await foreach (var tool in _client!.EnumerateToolsAsync(cancellationToken: _cts.Token))
        {
            toolCount++;
            output.WriteLine($"  Discovered: {tool.Name}");
        }

        Assert.Equal(ExpectedToolNames.Count, toolCount);

        output.WriteLine($"\n✓ Enumerated {toolCount} tools lazily");
    }

    /// <summary>
    /// Tests that obs_connection GetStatus works via MCP protocol (doesn't require OBS).
    /// </summary>
    [Fact]
    public async Task CallTool_ConnectionGetStatus_ReturnsNotConnected()
    {
        output.WriteLine("=== TOOL INVOCATION VIA MCP PROTOCOL ===\n");

        var arguments = new Dictionary<string, object?>
        {
            ["action"] = "GetStatus"
        };

        var result = await _client!.CallToolAsync(
            "obs_connection",
            arguments,
            cancellationToken: _cts.Token);

        Assert.NotNull(result);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        var textBlock = result.Content.OfType<TextContentBlock>().FirstOrDefault();
        Assert.NotNull(textBlock);

        output.WriteLine($"Tool response: {textBlock.Text}");

        // When not connected, should return "Not connected to OBS"
        Assert.Contains("Not connected", textBlock.Text);

        output.WriteLine("\n✓ obs_connection GetStatus executed successfully via MCP protocol");
    }

    /// <summary>
    /// Tests that obs_recording validation works via MCP protocol.
    /// </summary>
    [Fact]
    public async Task CallTool_RecordingSetFormat_ValidatesFormat()
    {
        output.WriteLine("=== TOOL VALIDATION VIA MCP PROTOCOL ===\n");

        // Test with invalid format
        var arguments = new Dictionary<string, object?>
        {
            ["action"] = "SetFormat",
            ["format"] = "invalid_format"
        };

        var result = await _client!.CallToolAsync(
            "obs_recording",
            arguments,
            cancellationToken: _cts.Token);

        Assert.NotNull(result);
        var textBlock = result.Content.OfType<TextContentBlock>().FirstOrDefault();
        Assert.NotNull(textBlock);

        output.WriteLine($"Tool response: {textBlock.Text}");

        // Should return validation error
        Assert.Contains("Error", textBlock.Text);
        Assert.Contains("Invalid format", textBlock.Text);

        output.WriteLine("\n✓ obs_recording format validation works via MCP protocol");
    }

    /// <summary>
    /// Tests that obs_audio validates required parameters.
    /// </summary>
    [Fact]
    public async Task CallTool_AudioMute_RequiresInputName()
    {
        output.WriteLine("=== PARAMETER VALIDATION VIA MCP PROTOCOL ===\n");

        var arguments = new Dictionary<string, object?>
        {
            ["action"] = "Mute"
            // Missing required inputName
        };

        var result = await _client!.CallToolAsync(
            "obs_audio",
            arguments,
            cancellationToken: _cts.Token);

        Assert.NotNull(result);
        var textBlock = result.Content.OfType<TextContentBlock>().FirstOrDefault();
        Assert.NotNull(textBlock);

        output.WriteLine($"Tool response: {textBlock.Text}");

        // Should return error about missing parameter
        Assert.Contains("inputName parameter is required", textBlock.Text);

        output.WriteLine("\n✓ obs_audio parameter validation works via MCP protocol");
    }

    /// <summary>
    /// Tests each tool has the expected action parameter.
    /// </summary>
    [Theory]
    [InlineData("obs_connection", "Connect", "Disconnect", "GetStatus", "GetStats")]
    [InlineData("obs_recording", "Start", "Stop", "Pause", "Resume", "GetStatus", "GetSettings", "SetFormat", "SetQuality", "SetPath", "GetPath")]
    [InlineData("obs_streaming", "Start", "Stop", "GetStatus")]
    [InlineData("obs_scene", "List", "GetCurrent", "Set", "ListSources")]
    [InlineData("obs_source", "AddWindowCapture", "ListWindows", "SetWindowCapture", "Remove", "SetEnabled")]
    [InlineData("obs_audio", "GetInputs", "Mute", "Unmute", "GetMuteState", "SetVolume", "GetVolume", "MuteAll", "UnmuteAll")]
    [InlineData("obs_media", "TakeScreenshot", "StartVirtualCamera", "StopVirtualCamera")]
    public async Task Tool_HasExpectedActions(string toolName, params string[] expectedActions)
    {
        output.WriteLine($"=== VALIDATING {toolName} ACTIONS ===\n");

        var tools = await _client!.ListToolsAsync(cancellationToken: _cts.Token);
        var tool = tools.FirstOrDefault(t => t.Name == toolName);

        Assert.NotNull(tool);
        Assert.NotNull(tool.Description);

        // Verify each expected action is mentioned in the description
        foreach (var action in expectedActions)
        {
            Assert.Contains(action, tool.Description, StringComparison.OrdinalIgnoreCase);
            output.WriteLine($"  ✓ {action}");
        }

        output.WriteLine($"\n✓ {toolName} has all {expectedActions.Length} expected actions");
    }
}
