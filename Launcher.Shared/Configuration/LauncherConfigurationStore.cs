using System;
using System.IO;
using System.Text.Json;
using Launcher.Shared.Storage;

namespace Launcher.Shared.Configuration;

/// <summary>
///     Handles persistence for <see cref="LauncherConfiguration"/>.
/// </summary>
public class LauncherConfigurationStore
{
    private readonly LauncherPaths _paths;
    private readonly IFileSystem _fileSystem;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    public LauncherConfigurationStore(LauncherPaths paths, IFileSystem fileSystem)
    {
        _paths = paths;
        _fileSystem = fileSystem;
    }

    public string ConfigurationFilePath => Path.Combine(_paths.ConfigurationRoot, "launcher.json");

    public LauncherConfiguration LoadOrCreateDefault(Action<string>? reportStatus = null)
    {
        var path = ConfigurationFilePath;

        try
        {
            if (_fileSystem.FileExists(path))
            {
                var json = _fileSystem.ReadAllText(path);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var configuration = JsonSerializer.Deserialize<LauncherConfiguration>(json, _serializerOptions);
                    if (configuration != null)
                    {
                        return configuration;
                    }
                }

                reportStatus?.Invoke("Configuration file was empty or invalid. Recreating with defaults...");
            }
            else
            {
                reportStatus?.Invoke("Configuration file not found. Creating default profile...");
            }
        }
        catch (Exception ex)
        {
            reportStatus?.Invoke($"Failed to read configuration: {ex.Message}. Using defaults.");
        }

        var defaults = CreateDefaultConfiguration();
        Save(defaults);
        return defaults;
    }

    public void Save(LauncherConfiguration configuration)
    {
        var json = JsonSerializer.Serialize(configuration, _serializerOptions);
        
        // Ensure the directory exists before writing
        var directory = Path.GetDirectoryName(ConfigurationFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            _fileSystem.EnsureDirectory(directory);
        }
        
        _fileSystem.WriteAllText(ConfigurationFilePath, json);
    }

    private static LauncherConfiguration CreateDefaultConfiguration()
    {
        var configuration = new LauncherConfiguration();

        foreach (var client in LauncherClients.All)
        {
            configuration.ClientInstallations.Add(new ClientInstallation
            {
                ClientId = client.Id,
                InstallPath = null,
                Version = null,
                IsVerified = false
            });
        }

        configuration.CoreTweaks["system.restorePoint"] = true;
        configuration.CoreTweaks["network.flush"] = true;
        configuration.OptionalTweaks["desktop.cleanIcons"] = false;
        configuration.OptionalTweaks["game.disableIntro"] = false;
        configuration.GpuTweaks["nvidia.lowLatency"] = true;
        configuration.GpuTweaks["amd.chill"] = false;

        return configuration;
    }
}
