using Moq;
using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet;
using Xunit;

namespace Sbroenne.ObsMcp.McpServer.Tests;

public class ObsRecordingSettingsTests
{
    [Theory]
    [InlineData("mkv", "hybrid_mp4", "mkv")]
    [InlineData(null, "hybrid_mp4", "hybrid_mp4")]
    public void GetRecordingSettings_UsesCurrentFormatAndEffectiveDefaults(
        string? configuredFormat, string defaultFormat, string expectedFormat)
    {
        var obs = CreateMock();
        obs.Setup(client => client.GetProfileParameter("SimpleOutput", "RecFormat2"))
            .Returns(Parameter(configuredFormat, defaultFormat));
        obs.Setup(client => client.GetProfileParameter("SimpleOutput", "RecQuality"))
            .Returns(Parameter(null, "HQ"));
        obs.Setup(client => client.GetProfileParameter("SimpleOutput", "RecEncoder"))
            .Returns(Parameter(null, "x264"));
        obs.Setup(client => client.GetProfileParameter("SimpleOutput", "FilePath"))
            .Returns(Parameter(null, @"C:\Videos"));
        using var client = new ObsClient(obs.Object);

        var settings = client.GetRecordingSettings();

        Assert.Equal(expectedFormat, settings.Format);
        Assert.Equal("HQ", settings.Quality);
        Assert.Equal("x264", settings.Encoder);
        Assert.Equal(@"C:\Videos", settings.Path);
        obs.Verify(websocket => websocket.GetProfileParameter("SimpleOutput", "RecFormat"), Times.Never);
    }

    [Theory]
    [InlineData("mkv", null, "mkv", "RecFormat2")]
    [InlineData(null, "hybrid_mp4", "hybrid_mp4", "RecFormat2")]
    [InlineData(null, null, "mp4", "RecFormat")]
    public void SetRecordingFormat_UpdatesTheActiveSetting(
        string? configuredFormat, string? defaultFormat, string originalFormat, string parameterName)
    {
        var obs = CreateMock();
        obs.Setup(client => client.GetProfileParameter("SimpleOutput", "RecFormat2"))
            .Returns(Parameter(configuredFormat, defaultFormat));
        if (parameterName == "RecFormat")
        {
            obs.Setup(client => client.GetProfileParameter("SimpleOutput", "RecFormat"))
                .Returns(Parameter(originalFormat, null));
        }
        obs.Setup(client => client.SetProfileParameter("SimpleOutput", parameterName, "mov"));
        using var client = new ObsClient(obs.Object);

        client.SetRecordingFormat("mov");

        obs.Verify(websocket => websocket.SetProfileParameter("SimpleOutput", parameterName, "mov"), Times.Once);
    }

    [Fact]
    public void GetRecordingSettings_SupportsLegacyRecordingFormats()
    {
        var obs = CreateMock();
        obs.Setup(client => client.GetProfileParameter("SimpleOutput", "RecFormat2"))
            .Returns(Parameter(null, null));
        obs.Setup(client => client.GetProfileParameter("SimpleOutput", "RecFormat"))
            .Returns(Parameter("mp4", null));
        foreach (var (name, value) in new[] { ("RecQuality", "HQ"), ("RecEncoder", "x264"), ("FilePath", @"C:\Videos") })
        {
            obs.Setup(client => client.GetProfileParameter("SimpleOutput", name)).Returns(Parameter(value, null));
        }
        using var client = new ObsClient(obs.Object);

        Assert.Equal("mp4", client.GetRecordingSettings().Format);
    }

    [Fact]
    public void GetRecordingSettings_RejectsMissingProfileResponses()
    {
        var obs = CreateMock();
        obs.Setup(client => client.GetProfileParameter("SimpleOutput", "RecFormat2")).Returns(() => null!);
        using var client = new ObsClient(obs.Object);

        var error = Assert.Throws<InvalidOperationException>(() => client.GetRecordingSettings());

        Assert.Contains("invalid recording setting", error.Message);
    }

    private static Mock<IOBSWebsocket> CreateMock()
    {
        var obs = new Mock<IOBSWebsocket>(MockBehavior.Strict);
        obs.SetupGet(client => client.IsConnected).Returns(false);
        return obs;
    }

    private static JObject Parameter(string? value, string? defaultValue)
    {
        return new JObject { ["parameterValue"] = value, ["defaultParameterValue"] = defaultValue };
    }
}
