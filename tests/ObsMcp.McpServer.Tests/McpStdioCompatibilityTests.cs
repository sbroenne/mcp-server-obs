using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace Sbroenne.ObsMcp.McpServer.Tests;

public class McpStdioCompatibilityTests
{
    [Theory]
    [InlineData("2024-11-05")]
    [InlineData("2025-03-26")]
    [InlineData("2025-06-18")]
    [InlineData("2025-11-25")]
    [InlineData("2026-07-28")]
    public async Task Server_SupportsCurrentAndEarlierClients(string protocolVersion)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var client = await McpClient.CreateAsync(
            new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = "OBS stdio compatibility test",
                Command = "dotnet",
                Arguments = [typeof(Program).Assembly.Location]
            }),
            new McpClientOptions { ProtocolVersion = protocolVersion },
            cancellationToken: timeout.Token);

        Assert.Equal("obs-mcp-server", client.ServerInfo.Name);
        Assert.Equal(Program.ServerVersion, client.ServerInfo.Version);
        var tools = await client.ListToolsAsync(cancellationToken: timeout.Token);
        Assert.Equal(7, tools.Count);
        var mediaTool = Assert.Single(tools, tool => tool.Name == "obs_media");
        Assert.Equal(["SaveScreenshot"], mediaTool.ProtocolTool.InputSchema
            .GetProperty("properties").GetProperty("action").GetProperty("enum")
            .EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(4, (await client.ListPromptsAsync(cancellationToken: timeout.Token)).Count);
        Assert.Equal(3, (await client.ListResourcesAsync(cancellationToken: timeout.Token)).Count);

        var status = await client.CallToolAsync("obs_connection",
            new Dictionary<string, object?> { ["action"] = "GetStatus" },
            cancellationToken: timeout.Token);
        Assert.False(status.IsError);
        Assert.Contains("Not connected to OBS", Assert.Single(status.Content.OfType<TextContentBlock>()).Text);

        var failure = await client.CallToolAsync("obs_recording",
            new Dictionary<string, object?> { ["action"] = "SetFormat", ["format"] = "invalid" },
            cancellationToken: timeout.Token);
        Assert.True(failure.IsError);
        Assert.Contains("Invalid format", Assert.Single(failure.Content.OfType<TextContentBlock>()).Text);
    }
}
