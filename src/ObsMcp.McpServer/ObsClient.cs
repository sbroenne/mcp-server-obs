using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet;

namespace Sbroenne.ObsMcp.McpServer;

/// <summary>
/// Wrapper around OBS WebSocket client providing a simplified interface.
/// </summary>
public class ObsClient : IDisposable
{
    private readonly OBSWebsocket _obs;
    private bool _disposed;

    public ObsClient()
    {
        _obs = new OBSWebsocket();
    }

    public bool IsConnected => _obs.IsConnected;

    public void Connect(string host, int port, string? password)
    {
        var url = $"ws://{host}:{port}";
        var connected = new ManualResetEventSlim(false);
        var error = (string?)null;

        EventHandler connectedHandler = (_, _) => connected.Set();
        EventHandler<OBSWebsocketDotNet.Communication.ObsDisconnectionInfo> disconnectedHandler = (_, e) =>
        {
            error = e.DisconnectReason ?? $"CloseCode: {e.ObsCloseCode}";
            connected.Set();
        };

        _obs.Connected += connectedHandler;
        _obs.Disconnected += disconnectedHandler;

        try
        {
            _obs.ConnectAsync(url, password ?? "");

            if (!connected.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new InvalidOperationException("Connection timed out");
            }

            if (error != null)
            {
                throw new InvalidOperationException($"Connection failed: {error}");
            }
        }
        finally
        {
            _obs.Connected -= connectedHandler;
            _obs.Disconnected -= disconnectedHandler;
        }
    }

    public void Disconnect()
    {
        if (_obs.IsConnected)
        {
            _obs.Disconnect();
        }
    }

    // Recording operations
    public void StartRecording() => _obs.StartRecord();
    public string StopRecording() => _obs.StopRecord();
    public void PauseRecording() => _obs.PauseRecord();
    public void ResumeRecording() => _obs.ResumeRecord();

    public ObsRecordingStatus GetRecordingStatus()
    {
        var status = _obs.GetRecordStatus();
        return new ObsRecordingStatus
        {
            IsRecording = status.IsRecording,
            IsPaused = status.IsRecordingPaused,
            Timecode = status.RecordTimecode ?? "",
            DurationMs = status.RecordingDuration
        };
    }

    // Streaming operations
    public void StartStreaming() => _obs.StartStream();
    public void StopStreaming() => _obs.StopStream();

    public ObsStreamingStatus GetStreamingStatus()
    {
        var status = _obs.GetStreamStatus();
        return new ObsStreamingStatus
        {
            IsStreaming = status.IsActive,
            IsReconnecting = status.IsReconnecting,
            DurationMs = status.Duration,
            BytesSent = status.BytesSent
        };
    }

    // Scene operations
    public List<ObsSceneInfo> GetScenes()
    {
        var scenes = _obs.GetSceneList();
        var currentScene = scenes.CurrentProgramSceneName;
        return scenes.Scenes.Select((s, i) => new ObsSceneInfo
        {
            Name = s.Name,
            Index = int.TryParse(s.Index, out var idx) ? idx : i,
            IsCurrent = s.Name == currentScene
        }).ToList();
    }

    public string GetCurrentScene()
    {
        var scenes = _obs.GetSceneList();
        return scenes.CurrentProgramSceneName;
    }

    public void SetScene(string sceneName)
    {
        _obs.SetCurrentProgramScene(sceneName);
    }

    // Source operations
    public List<ObsSourceInfo> GetSources(string? sceneName = null)
    {
        var scene = sceneName ?? GetCurrentScene();
        var items = _obs.GetSceneItemList(scene);
        return items.Select(item =>
        {
            var enabled = true;
            try { enabled = _obs.GetSceneItemEnabled(scene, item.ItemId); } catch { }
            return new ObsSourceInfo
            {
                Name = item.SourceName,
                Kind = item.SourceType.ToString(),
                SceneItemId = item.ItemId,
                IsEnabled = enabled
            };
        }).ToList();
    }

    public int AddWindowCapture(string sourceName, string? sceneName = null)
    {
        var scene = sceneName ?? GetCurrentScene();
        var inputSettings = new JObject();

        return _obs.CreateInput(scene, sourceName, "window_capture", inputSettings, true);
    }

    public List<ObsWindowInfo> ListWindows(string sourceName)
    {
        var windows = new List<ObsWindowInfo>();

        try
        {
            // Use raw request to bypass obs-websocket-dotnet library bug
            // in GetInputPropertiesListPropertyItems which can't handle JArray response
            var requestData = new JObject
            {
                ["inputName"] = sourceName,
                ["propertyName"] = "window"
            };

            var response = _obs.SendRequest("GetInputPropertiesListPropertyItems", requestData);

            if (response != null)
            {
                var propertyItems = response["propertyItems"] as JArray;
                if (propertyItems != null)
                {
                    foreach (var item in propertyItems)
                    {
                        if (item is JObject jobj)
                        {
                            var name = jobj["itemName"]?.Value<string>() ?? "";
                            var value = jobj["itemValue"]?.Value<string>() ?? "";
                            var enabled = jobj["itemEnabled"]?.Value<bool>() ?? true;

                            if (!string.IsNullOrEmpty(value))
                            {
                                windows.Add(new ObsWindowInfo
                                {
                                    Name = name,
                                    Value = value,
                                    Enabled = enabled
                                });
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // If raw request fails, try the library method as fallback
            try
            {
                var properties = _obs.GetInputPropertiesListPropertyItems(sourceName, "window");

                foreach (var prop in properties)
                {
                    if (prop is JObject jobj)
                    {
                        var name = jobj["itemName"]?.Value<string>() ?? "";
                        var value = jobj["itemValue"]?.Value<string>() ?? "";
                        var enabled = jobj["itemEnabled"]?.Value<bool>() ?? true;

                        if (!string.IsNullOrEmpty(value))
                        {
                            windows.Add(new ObsWindowInfo
                            {
                                Name = name,
                                Value = value,
                                Enabled = enabled
                            });
                        }
                    }
                }
            }
            catch
            {
                // Both methods failed, throw the original error
                throw new InvalidOperationException($"Failed to list windows: {ex.Message}", ex);
            }
        }

        return windows;
    }

    public void SetWindowCapture(string sourceName, string windowValue)
    {
        var settings = new JObject
        {
            ["window"] = windowValue
        };
        _obs.SetInputSettings(sourceName, settings);
    }

    public void RemoveSource(string sourceName, string? sceneName = null)
    {
        var scene = sceneName ?? GetCurrentScene();
        var items = _obs.GetSceneItemList(scene);
        var item = items.FirstOrDefault(i => i.SourceName == sourceName);
        if (item != null)
        {
            _obs.RemoveSceneItem(scene, item.ItemId);
        }
    }

    public void SetSourceEnabled(string sourceName, bool enabled, string? sceneName = null)
    {
        var scene = sceneName ?? GetCurrentScene();
        var items = _obs.GetSceneItemList(scene);
        var item = items.FirstOrDefault(i => i.SourceName == sourceName);
        if (item != null)
        {
            _obs.SetSceneItemEnabled(scene, item.ItemId, enabled);
        }
    }

    // Screenshot - saves to file
    public void SaveScreenshot(string filePath, string? sourceName, string imageFormat, int? width, int? height, int? quality)
    {
        var scene = sourceName ?? GetCurrentScene();
        // Use -1 to indicate source resolution (OBS default)
        var w = width ?? -1;
        var h = height ?? -1;
        var q = quality ?? -1;

        // Use SaveSourceScreenshot which saves directly to file
        _obs.SaveSourceScreenshot(scene, imageFormat, filePath, w, h, q);
    }

    // Virtual camera
    public void StartVirtualCamera() => _obs.StartVirtualCam();
    public void StopVirtualCamera() => _obs.StopVirtualCam();

    // Performance stats
    public ObsPerformanceStats GetStats()
    {
        var stats = _obs.GetStats();
        return new ObsPerformanceStats
        {
            CpuUsage = stats.CpuUsage,
            MemoryUsage = stats.MemoryUsage,
            ActiveFps = stats.FPS,
            RenderTotalFrames = (int)stats.RenderTotalFrames,
            RenderSkippedFrames = (int)stats.RenderMissedFrames,
            OutputTotalFrames = (int)stats.OutputTotalFrames,
            OutputSkippedFrames = (int)stats.OutputSkippedFrames,
            AvailableDiskSpace = stats.FreeDiskSpace,
            AverageFrameRenderTime = stats.AverageFrameTime
        };
    }

    // Audio operations
    public List<ObsAudioInput> GetSpecialInputs()
    {
        var inputs = new List<ObsAudioInput>();
        var specialInputs = _obs.GetSpecialInputs();

        if (specialInputs.TryGetValue("desktop1", out var desktop1) && !string.IsNullOrEmpty(desktop1))
        {
            inputs.Add(new ObsAudioInput { Name = desktop1, Kind = "Desktop Audio" });
        }
        if (specialInputs.TryGetValue("desktop2", out var desktop2) && !string.IsNullOrEmpty(desktop2))
        {
            inputs.Add(new ObsAudioInput { Name = desktop2, Kind = "Desktop Audio 2" });
        }
        if (specialInputs.TryGetValue("mic1", out var mic1) && !string.IsNullOrEmpty(mic1))
        {
            inputs.Add(new ObsAudioInput { Name = mic1, Kind = "Mic/Aux" });
        }
        if (specialInputs.TryGetValue("mic2", out var mic2) && !string.IsNullOrEmpty(mic2))
        {
            inputs.Add(new ObsAudioInput { Name = mic2, Kind = "Mic/Aux 2" });
        }
        if (specialInputs.TryGetValue("mic3", out var mic3) && !string.IsNullOrEmpty(mic3))
        {
            inputs.Add(new ObsAudioInput { Name = mic3, Kind = "Mic/Aux 3" });
        }
        if (specialInputs.TryGetValue("mic4", out var mic4) && !string.IsNullOrEmpty(mic4))
        {
            inputs.Add(new ObsAudioInput { Name = mic4, Kind = "Mic/Aux 4" });
        }

        return inputs;
    }

    public bool GetInputMute(string inputName)
    {
        return _obs.GetInputMute(inputName);
    }

    public void SetInputMute(string inputName, bool muted)
    {
        _obs.SetInputMute(inputName, muted);
    }

    public (double mul, double db) GetInputVolume(string inputName)
    {
        var volume = _obs.GetInputVolume(inputName);
        return (volume.VolumeMul, volume.VolumeDb);
    }

    public void SetInputVolume(string inputName, double volumeMul)
    {
        _obs.SetInputVolume(inputName, (float)volumeMul, false);
    }

    // Recording settings
    public ObsRecordingSettings GetRecordingSettings()
    {
        var format = _obs.GetProfileParameter("SimpleOutput", "RecFormat") as JObject;
        var quality = _obs.GetProfileParameter("SimpleOutput", "RecQuality") as JObject;
        var encoder = _obs.GetProfileParameter("SimpleOutput", "RecEncoder") as JObject;
        var path = _obs.GetProfileParameter("SimpleOutput", "FilePath") as JObject;

        return new ObsRecordingSettings
        {
            Format = format?["parameterValue"]?.ToString() ?? "unknown",
            Quality = quality?["parameterValue"]?.ToString() ?? "unknown",
            Encoder = encoder?["parameterValue"]?.ToString() ?? "unknown",
            Path = path?["parameterValue"]?.ToString() ?? ""
        };
    }

    public void SetRecordingFormat(string format)
    {
        _obs.SetProfileParameter("SimpleOutput", "RecFormat", format);
    }

    public void SetRecordingQuality(string quality)
    {
        _obs.SetProfileParameter("SimpleOutput", "RecQuality", quality);
    }

    public string GetRecordingDirectory()
    {
        var response = _obs.SendRequest("GetRecordDirectory");
        return response?["recordDirectory"]?.Value<string>() ?? "unknown";
    }

    public void SetRecordingDirectory(string directory)
    {
        var requestData = new JObject
        {
            ["recordDirectory"] = directory
        };
        _obs.SendRequest("SetRecordDirectory", requestData);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Disconnect();
            _disposed = true;
        }
    }
}

// DTOs - using init properties for test compatibility
public class ObsRecordingStatus
{
    public bool IsRecording { get; init; }
    public bool IsPaused { get; init; }
    public string Timecode { get; init; } = string.Empty;
    public long DurationMs { get; init; }
}

public class ObsStreamingStatus
{
    public bool IsStreaming { get; init; }
    public bool IsReconnecting { get; init; }
    public long DurationMs { get; init; }
    public long BytesSent { get; init; }
}

public class ObsSceneInfo
{
    public string Name { get; init; } = string.Empty;
    public int Index { get; init; }
    public bool IsCurrent { get; init; }
}

public class ObsSourceInfo
{
    public string Name { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public int SceneItemId { get; init; }
    public bool IsEnabled { get; init; }
}

public class ObsWindowInfo
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public bool Enabled { get; init; }
}

public class ObsPerformanceStats
{
    public double CpuUsage { get; init; }
    public double MemoryUsage { get; init; }
    public double ActiveFps { get; init; }
    public int RenderTotalFrames { get; init; }
    public int RenderSkippedFrames { get; init; }
    public int OutputTotalFrames { get; init; }
    public int OutputSkippedFrames { get; init; }
    public double AvailableDiskSpace { get; init; }
    public double AverageFrameRenderTime { get; init; }
}

public class ObsRecordingSettings
{
    public string Format { get; init; } = string.Empty;
    public string Quality { get; init; } = string.Empty;
    public string Encoder { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
}

public class ObsAudioInput
{
    public string Name { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
}
