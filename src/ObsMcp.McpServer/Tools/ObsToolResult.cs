using ModelContextProtocol.Protocol;

namespace Sbroenne.ObsMcp.McpServer.Tools;

internal static class ObsToolResult
{
    internal static CallToolResult FromText(string text)
    {
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = text }],
            IsError = text.StartsWith("Error: ", StringComparison.Ordinal)
        };
    }
}
