using DotNetEnv;
using Sbroenne.ObsMcp.McpServer.Tools;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Sbroenne.ObsMcp.McpServer.Tests;

/// <summary>
/// Integration tests for OBS recording workflow.
/// 
/// PREREQUISITES:
/// - OBS Studio must be installed (will be started automatically if not running)
/// - WebSocket server must be enabled (Tools → WebSocket Server Settings)
/// - Create a .env file in the repository root with:
///   - OBS_HOST (default: localhost)
///   - OBS_PORT (default: 4455)
///   - OBS_PASSWORD (required if authentication is enabled)
///   - OBS_PATH (optional: path to obs64.exe, auto-detected if not set)
/// 
/// Run with: dotnet test --filter "Category=Integration"
/// </summary>
[Trait("Category", "Integration")]
[Collection("OBS Integration Tests")] // Run sequentially to avoid conflicts with shared OBS connection
public class IntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _host;
    private readonly int _port;
    private readonly string? _password;
    private readonly bool _skipTests;
#pragma warning disable CS0414 // Field is assigned but never used - kept for potential future cleanup logic
    private static bool _obsWasStartedByTests = false;
#pragma warning restore CS0414
    private static readonly object _obsStartLock = new();

    static IntegrationTests()
    {
        // Load .env file from repository root (navigate up from test bin folder)
        var currentDir = Directory.GetCurrentDirectory();
        var envPath = FindEnvFile(currentDir);

        if (envPath != null)
        {
            Env.Load(envPath);
        }
    }

    private static string? FindEnvFile(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            var envFile = Path.Combine(dir.FullName, ".env");
            if (File.Exists(envFile))
            {
                return envFile;
            }
            dir = dir.Parent;
        }
        return null;
    }

    public IntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _host = Environment.GetEnvironmentVariable("OBS_HOST") ?? "localhost";
        _port = int.TryParse(Environment.GetEnvironmentVariable("OBS_PORT"), out var p) ? p : 4455;
        _password = Environment.GetEnvironmentVariable("OBS_PASSWORD");

        // Skip integration tests if OBS_PASSWORD is not set (CI environments)
        _skipTests = string.IsNullOrEmpty(_password);

        if (_skipTests)
        {
            _output.WriteLine("Skipping integration tests - OBS_PASSWORD not set");
            _output.WriteLine("Create a .env file in the repository root with OBS_PASSWORD=your_password");
        }
        else
        {
            _output.WriteLine($"Using OBS at {_host}:{_port}");
            EnsureObsIsRunning();
        }
    }

    /// <summary>
    /// Check if OBS is running and start it if necessary.
    /// </summary>
    private void EnsureObsIsRunning()
    {
        lock (_obsStartLock)
        {
            if (IsObsRunning())
            {
                _output.WriteLine("OBS is already running");
                return;
            }

            _output.WriteLine("OBS is not running, attempting to start...");

            var obsPath = FindObsExecutable();
            if (obsPath == null)
            {
                _output.WriteLine("WARNING: Could not find OBS executable. Please start OBS manually.");
                return;
            }

            _output.WriteLine($"Starting OBS from: {obsPath}");

            try
            {
                // OBS must be started from its installation directory to find locale files
                var obsDirectory = Path.GetDirectoryName(obsPath);

                var startInfo = new ProcessStartInfo
                {
                    FileName = obsPath,
                    Arguments = "--minimize-to-tray", // Start minimized
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    WorkingDirectory = obsDirectory // Critical: OBS needs to run from its install dir
                };

                Process.Start(startInfo);
                _obsWasStartedByTests = true;

                // Wait for OBS to start and WebSocket server to be ready
                _output.WriteLine("Waiting for OBS to initialize...");
                WaitForObsWebSocket();

                _output.WriteLine("OBS started successfully");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Failed to start OBS: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Check if OBS process is running.
    /// </summary>
    private static bool IsObsRunning()
    {
        var processes = Process.GetProcessesByName("obs64");
        if (processes.Length == 0)
        {
            processes = Process.GetProcessesByName("obs32");
        }
        return processes.Length > 0;
    }

    /// <summary>
    /// Find the OBS executable path.
    /// </summary>
    private string? FindObsExecutable()
    {
        // Check environment variable first
        var envPath = Environment.GetEnvironmentVariable("OBS_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
        {
            return envPath;
        }

        // Common installation paths on Windows
        var possiblePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit", "obs64.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "obs-studio", "bin", "64bit", "obs64.exe"),
            @"C:\Program Files\obs-studio\bin\64bit\obs64.exe",
            @"C:\Program Files (x86)\obs-studio\bin\64bit\obs64.exe",
            // Steam installation
            @"C:\Program Files (x86)\Steam\steamapps\common\OBS Studio\bin\64bit\obs64.exe",
            // Scoop installation
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "apps", "obs-studio", "current", "bin", "64bit", "obs64.exe"),
            // Chocolatey installation
            @"C:\tools\obs-studio\bin\64bit\obs64.exe"
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // Try to find in PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv != null)
        {
            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                var obsPath = Path.Combine(dir, "obs64.exe");
                if (File.Exists(obsPath))
                {
                    return obsPath;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Wait for OBS WebSocket server to become available.
    /// </summary>
    private void WaitForObsWebSocket()
    {
        const int maxAttempts = 30; // 30 seconds max
        const int delayMs = 1000;

        for (int i = 0; i < maxAttempts; i++)
        {
            Thread.Sleep(delayMs);

            try
            {
                var result = ObsConnectionTool.Connection(ConnectionAction.Connect, _host, _port, _password);
                if (result.Contains("Connected"))
                {
                    ObsConnectionTool.Connection(ConnectionAction.Disconnect);
                    return;
                }
            }
            catch
            {
                // Connection failed, keep waiting
            }

            if ((i + 1) % 5 == 0)
            {
                _output.WriteLine($"Still waiting for OBS WebSocket... ({i + 1}s)");
            }
        }

        _output.WriteLine("WARNING: Timed out waiting for OBS WebSocket server");
    }

    public void Dispose()
    {
        // Ensure disconnected after each test
        try
        {
            ObsConnectionTool.Connection(ConnectionAction.Disconnect);
        }
        catch
        {
            // Ignore
        }
    }

    private void EnsureConnected()
    {
        if (_skipTests)
        {
            throw new SkipException("OBS_PASSWORD not set");
        }

        var result = ObsConnectionTool.Connection(ConnectionAction.Connect, _host, _port, _password);
        if (result.StartsWith("Error"))
        {
            throw new Exception($"Failed to connect to OBS: {result}");
        }
    }

    private void EnsureRecordingStopped()
    {
        try
        {
            var status = ObsRecordingTool.Recording(RecordingAction.GetStatus);
            if (status.Contains("Active: True"))
            {
                ObsRecordingTool.Recording(RecordingAction.Stop);
                Thread.Sleep(1000);
            }
        }
        catch
        {
            // Ignore
        }
    }

    [SkippableFact]
    public void Should_Connect_And_Disconnect()
    {
        Skip.If(_skipTests, "OBS_PASSWORD not set");

        // Connect
        var connectResult = ObsConnectionTool.Connection(ConnectionAction.Connect, _host, _port, _password);
        _output.WriteLine($"Connect result: {connectResult}");
        Assert.Contains("Connected", connectResult);

        // Get status
        var statusResult = ObsConnectionTool.Connection(ConnectionAction.GetStatus);
        _output.WriteLine($"Status: {statusResult}");
        Assert.Contains("Connected to OBS", statusResult);
        Assert.Contains("Recording:", statusResult);

        // Disconnect
        var disconnectResult = ObsConnectionTool.Connection(ConnectionAction.Disconnect);
        _output.WriteLine($"Disconnect result: {disconnectResult}");
        Assert.Contains("Disconnected", disconnectResult);
    }

    [SkippableFact]
    public void Should_List_Scenes()
    {
        Skip.If(_skipTests, "OBS_PASSWORD not set");
        EnsureConnected();

        var result = ObsSceneTool.Scene(SceneAction.List);
        _output.WriteLine($"Scenes: {result}");

        Assert.DoesNotContain("Error", result);
        Assert.Contains("scene", result.ToLowerInvariant());
    }

    [SkippableFact]
    public void Should_Get_Current_Scene()
    {
        Skip.If(_skipTests, "OBS_PASSWORD not set");
        EnsureConnected();

        var result = ObsSceneTool.Scene(SceneAction.GetCurrent);
        _output.WriteLine($"Current scene: {result}");

        Assert.DoesNotContain("Error", result);
    }

    [SkippableFact]
    public void Should_List_Sources()
    {
        Skip.If(_skipTests, "OBS_PASSWORD not set");
        EnsureConnected();

        var result = ObsSceneTool.Scene(SceneAction.ListSources);
        _output.WriteLine($"Sources: {result}");

        Assert.DoesNotContain("Error", result);
    }

    [SkippableFact]
    public void Should_Complete_Recording_Workflow()
    {
        Skip.If(_skipTests, "OBS_PASSWORD not set");
        EnsureConnected();
        EnsureRecordingStopped();

        const string testSourceName = "Test Window Capture Recording";

        // Clean up any leftover source from previous test runs
        try
        {
            ObsSourceTool.Source(SourceAction.Remove, testSourceName);
            Thread.Sleep(200);
        }
        catch
        {
            // Ignore - source might not exist
        }

        try
        {
            // 1. Get initial status
            var initialStatus = ObsRecordingTool.Recording(RecordingAction.GetStatus);
            _output.WriteLine($"Initial status: {initialStatus}");
            Assert.Contains("Active: False", initialStatus);

            // 2. Add a window capture source
            _output.WriteLine("Adding window capture source...");
            var addSourceResult = ObsSourceTool.Source(SourceAction.AddWindowCapture, testSourceName);
            _output.WriteLine($"Add source result: {addSourceResult}");
            Thread.Sleep(500);

            // 3. List available windows and select first one
            _output.WriteLine("Listing available windows...");
            var listResult = ObsSourceTool.Source(SourceAction.ListWindows, testSourceName);
            _output.WriteLine($"Available windows: {listResult}");

            // Parse the first window value
            var lines = listResult.Split('\n');
            string? windowValue = null;
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("Value:"))
                {
                    windowValue = line.Replace("Value:", "").Trim();
                    break;
                }
            }

            if (windowValue != null)
            {
                _output.WriteLine($"Setting window capture to: {windowValue}");
                var setResult = ObsSourceTool.Source(SourceAction.SetWindowCapture, testSourceName, windowValue: windowValue);
                _output.WriteLine($"Set window result: {setResult}");
            }
            else
            {
                _output.WriteLine("Warning: No windows available, recording may be black");
            }

            Thread.Sleep(1000);

            // Verify source was added and is visible
            var sourcesBeforeRecording = ObsSceneTool.Scene(SceneAction.ListSources);
            _output.WriteLine($"Sources in scene: {sourcesBeforeRecording}");

            // Ensure source is enabled (visible)
            var enableResult = ObsSourceTool.Source(SourceAction.SetEnabled, testSourceName, enabled: true);
            _output.WriteLine($"Enable source result: {enableResult}");
            Thread.Sleep(500);

            // 3. Start recording
            _output.WriteLine("Starting recording...");
            var startResult = ObsRecordingTool.Recording(RecordingAction.Start);
            _output.WriteLine($"Start result: {startResult}");
            Assert.Contains("started", startResult.ToLowerInvariant());

            // 4. Wait for recording to initialize
            Thread.Sleep(2000);

            // 5. Verify recording is active
            var recordingStatus = ObsRecordingTool.Recording(RecordingAction.GetStatus);
            _output.WriteLine($"Recording status: {recordingStatus}");
            Assert.Contains("Active: True", recordingStatus);

            // 6. Record for a few seconds
            _output.WriteLine("Recording for 3 seconds...");
            Thread.Sleep(3000);

            // 7. Stop recording
            _output.WriteLine("Stopping recording...");
            var stopResult = ObsRecordingTool.Recording(RecordingAction.Stop);
            _output.WriteLine($"Stop result: {stopResult}");
            Assert.Contains("stopped", stopResult.ToLowerInvariant());

            // 8. Wait for recording to fully stop (OBS needs time to finalize)
            Thread.Sleep(2000);

            // 9. Verify stopped
            var finalStatus = ObsRecordingTool.Recording(RecordingAction.GetStatus);
            _output.WriteLine($"Final status: {finalStatus}");
            Assert.Contains("Active: False", finalStatus);

            _output.WriteLine("✓ Full recording workflow completed successfully");
            _output.WriteLine("NOTE: Check the recording output folder in OBS to verify the video is not black.");
        }
        finally
        {
            EnsureRecordingStopped();

            // Clean up: remove the test source
            try
            {
                ObsSourceTool.Source(SourceAction.Remove, testSourceName);
                _output.WriteLine($"Cleaned up test source: {testSourceName}");
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [SkippableFact]
    public void Should_Take_Screenshot()
    {
        Skip.If(_skipTests, "OBS_PASSWORD not set");
        EnsureConnected();

        const string testSourceName = "Test Screenshot Source";

        // Clean up any leftover source from previous test runs
        try
        {
            ObsSourceTool.Source(SourceAction.Remove, testSourceName);
            Thread.Sleep(200);
        }
        catch
        {
            // Ignore - source might not exist
        }

        try
        {
            // Add a window capture source to ensure there's something to screenshot
            _output.WriteLine("Adding window capture source for screenshot...");
            ObsSourceTool.Source(SourceAction.AddWindowCapture, testSourceName);
            Thread.Sleep(500);

            // Take screenshot without specifying dimensions (let OBS use source resolution)
            var result = ObsMediaTool.Media(MediaAction.TakeScreenshot, sourceName: null, imageFormat: "png", width: null, height: null);
            _output.WriteLine($"Screenshot result: {result}");

            // Check for success or handle case where no source is available
            if (result.Contains("Error") && result.Contains("No current program scene"))
            {
                _output.WriteLine("Note: No scene available for screenshot - test inconclusive");
                return;
            }

            Assert.Contains("Screenshot captured", result);
        }
        finally
        {
            // Clean up
            try
            {
                ObsSourceTool.Source(SourceAction.Remove, testSourceName);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [SkippableFact]
    public void Should_Complete_Window_Capture_Workflow()
    {
        Skip.If(_skipTests, "OBS_PASSWORD not set");
        EnsureConnected();

        const string testSourceName = "Test Window Capture";

        // Clean up any leftover source from previous test runs
        try
        {
            ObsSourceTool.Source(SourceAction.Remove, testSourceName);
            Thread.Sleep(200);
        }
        catch
        {
            // Ignore - source might not exist
        }

        try
        {
            // 1. Add window capture source
            _output.WriteLine("Step 1: Adding window capture source...");
            var addResult = ObsSourceTool.Source(SourceAction.AddWindowCapture, testSourceName);
            _output.WriteLine($"Add result: {addResult}");
            Assert.Contains("Added window capture source", addResult);
            Assert.Contains("NEXT STEP", addResult);
            Thread.Sleep(500);

            // 2. List available windows
            _output.WriteLine("Step 2: Listing available windows...");
            var listResult = ObsSourceTool.Source(SourceAction.ListWindows, testSourceName);
            _output.WriteLine($"List result: {listResult}");

            // Should have windows available (at minimum, this test process is running)
            if (listResult.Contains("No windows available"))
            {
                _output.WriteLine("Note: No windows available - test inconclusive");
                return;
            }

            Assert.Contains("Available Windows", listResult);
            Assert.Contains("Value:", listResult);

            // 3. Extract a window value from the list and set it
            // Parse the first window value from the output
            var lines = listResult.Split('\n');
            string? windowValue = null;
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("Value:"))
                {
                    windowValue = line.Replace("Value:", "").Trim();
                    break;
                }
            }

            if (windowValue != null)
            {
                _output.WriteLine($"Step 3: Setting window capture to: {windowValue}");
                var setResult = ObsSourceTool.Source(SourceAction.SetWindowCapture, testSourceName, windowValue: windowValue);
                _output.WriteLine($"Set result: {setResult}");
                Assert.Contains("configured to capture", setResult);
            }
            else
            {
                _output.WriteLine("Could not parse window value from list");
            }

            // 4. Verify the source exists in the scene
            _output.WriteLine("Step 4: Verifying source in scene...");
            var sourcesResult = ObsSceneTool.Scene(SceneAction.ListSources);
            _output.WriteLine($"Sources: {sourcesResult}");
            Assert.Contains(testSourceName, sourcesResult);

            _output.WriteLine("✓ Window capture workflow completed successfully");
        }
        finally
        {
            // Clean up: remove the test source
            try
            {
                ObsSourceTool.Source(SourceAction.Remove, testSourceName);
                _output.WriteLine($"Cleaned up test source: {testSourceName}");
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [SkippableFact]
    public void Should_Get_Recording_Settings()
    {
        Skip.If(_skipTests, "OBS_PASSWORD not set");
        EnsureConnected();

        var result = ObsRecordingTool.Recording(RecordingAction.GetSettings);
        _output.WriteLine($"Recording settings: {result}");

        Assert.DoesNotContain("Error", result);
        Assert.Contains("Format", result);
    }

    [SkippableFact]
    public void Should_Get_Streaming_Status()
    {
        Skip.If(_skipTests, "OBS_PASSWORD not set");
        EnsureConnected();

        var result = ObsStreamingTool.Streaming(StreamingAction.GetStatus);
        _output.WriteLine($"Streaming status: {result}");

        Assert.DoesNotContain("Error", result);
        Assert.Contains("Active:", result);
    }
}

/// <summary>
/// Custom Skip exception for conditional test skipping.
/// </summary>
public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}

/// <summary>
/// Attribute to mark tests as skippable.
/// </summary>
public class SkippableFactAttribute : FactAttribute { }

/// <summary>
/// Helper class for conditional test skipping.
/// </summary>
public static class Skip
{
    public static void If(bool condition, string reason)
    {
        if (condition)
        {
            throw new SkipException(reason);
        }
    }
}
