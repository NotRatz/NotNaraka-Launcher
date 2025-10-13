using System.Collections.Generic;

namespace Launcher.Shared.Configuration;

/// <summary>
///     Represents persisted launcher configuration that replaces the ad-hoc state
///     previously tracked by the PowerShell implementation.
/// </summary>
public class LauncherConfiguration
{
    public string SelectedClientId { get; set; } = LauncherClients.OfficialGlobal.Id;

    public bool HasCompletedInitialSetup { get; set; }
        = false;

    public Dictionary<string, bool> CoreTweaks { get; set; } = new();

    public Dictionary<string, bool> OptionalTweaks { get; set; } = new();

    public Dictionary<string, bool> GpuTweaks { get; set; } = new();

    public Dictionary<string, string> QualitySettings { get; set; } = new();

    public List<ClientInstallation> ClientInstallations { get; set; } = new();
    
    // Game location settings
    public string? SteamGamePath { get; set; } = null;
    public string? EpicGamePath { get; set; } = null;
    public string? OfficialGamePath { get; set; } = null;
    
    // Preferred launch method
    public string? PreferredLaunchMethod { get; set; } = null; // "Steam", "Epic", "Official", or null
}

/// <summary>
///     Provides a canonical list of known Naraka clients.
/// </summary>
public static class LauncherClients
{
    public static readonly ClientDescriptor OfficialGlobal = new("global", "Official Global", "Retail client distributed through the official servers.");
    public static readonly ClientDescriptor Steam = new("steam", "Steam", "Steam-distributed build with overlay integration.");
    public static readonly ClientDescriptor Chinese = new("cn", "Chinese", "Region-locked client that requires separate credentials.");

    public static IReadOnlyList<ClientDescriptor> All { get; } = new[] { OfficialGlobal, Steam, Chinese };
}

public record ClientDescriptor(string Id, string Name, string Description);

public class ClientInstallation
{
    public string ClientId { get; set; } = LauncherClients.OfficialGlobal.Id;

    public string? InstallPath { get; set; }
        = null;

    public string? Version { get; set; }
        = null;

    public bool IsVerified { get; set; }
        = false;
}
