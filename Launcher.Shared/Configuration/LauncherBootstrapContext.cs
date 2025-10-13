using System;

namespace Launcher.Shared.Configuration;

/// <summary>
///     Captures the state returned from the bootstrapper so the UI can access
///     launcher paths and configuration without touching PowerShell scripts.
/// </summary>
public class LauncherBootstrapContext
{
    public LauncherBootstrapContext(LauncherPaths paths, LauncherConfiguration configuration, LauncherConfigurationStore configurationStore)
    {
        Paths = paths;
        Configuration = configuration;
        ConfigurationStore = configurationStore;
    }

    public LauncherPaths Paths { get; }

    public LauncherConfiguration Configuration { get; }

    public LauncherConfigurationStore ConfigurationStore { get; }

    public event EventHandler? ConfigurationChanged;

    public void NotifyConfigurationChanged()
    {
        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }
}
