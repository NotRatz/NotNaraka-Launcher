using System;
using System.IO;

namespace Launcher.Shared.Configuration;

/// <summary>
///     Provides well-known directories for storing launcher configuration and caches.
/// </summary>
public class LauncherPaths
{
    private const string RootFolderName = "NarakaTweaks";

    public string ConfigurationRoot { get; }
    public string CacheRoot { get; }

    public LauncherPaths()
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        ConfigurationRoot = Path.Combine(programData, RootFolderName, "config");
        CacheRoot = Path.Combine(programData, RootFolderName, "cache");
    }
}
