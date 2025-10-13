using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace NarakaTweaks.Core.Services;

/// <summary>
/// Detects installed game locations for Steam, Epic, and Official Naraka installations
/// </summary>
public class GameLocationDetector
{
    /// <summary>
    /// Detect all installed Naraka game locations
    /// </summary>
    public GameLocations DetectAll()
    {
        return new GameLocations
        {
            SteamPath = DetectSteamInstallation(),
            EpicPath = DetectEpicInstallation(),
            OfficialPath = DetectOfficialInstallation()
        };
    }
    
    /// <summary>
    /// Detect Steam installation of Naraka
    /// </summary>
    public string? DetectSteamInstallation()
    {
        try
        {
            // Method 1: Check Steam registry for install path
            using var steamKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            if (steamKey != null)
            {
                var steamPath = steamKey.GetValue("InstallPath") as string;
                if (!string.IsNullOrEmpty(steamPath))
                {
                    // Check common Steam library locations
                    var libraryFolders = new List<string> { Path.Combine(steamPath, "steamapps", "common") };
                    
                    // Read libraryfolders.vdf for additional library locations
                    var libraryVdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                    if (File.Exists(libraryVdf))
                    {
                        var additionalLibraries = ParseLibraryFoldersVdf(libraryVdf);
                        libraryFolders.AddRange(additionalLibraries);
                    }
                    
                    // Check for Naraka in each library
                    foreach (var library in libraryFolders)
                    {
                        var narakaPath = Path.Combine(library, "NARAKA BLADEPOINT");
                        if (Directory.Exists(narakaPath))
                        {
                            var exePath = Path.Combine(narakaPath, "NarakaBladepoint.exe");
                            if (File.Exists(exePath))
                            {
                                return narakaPath;
                            }
                        }
                    }
                }
            }
            
            // Method 2: Check Steam registry for app install location directly
            using var appKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 1203220");
            if (appKey != null)
            {
                var installLocation = appKey.GetValue("InstallLocation") as string;
                if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                {
                    return installLocation;
                }
            }
        }
        catch
        {
            // Ignore errors and return null
        }
        
        return null;
    }
    
    /// <summary>
    /// Detect Epic Games installation of Naraka
    /// </summary>
    public string? DetectEpicInstallation()
    {
        try
        {
            // Method 1: Check Epic Games registry
            using var epicKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Epic Games\EpicGamesLauncher");
            if (epicKey != null)
            {
                var epicPath = epicKey.GetValue("AppDataPath") as string;
                if (!string.IsNullOrEmpty(epicPath))
                {
                    // Check manifests folder for Naraka
                    var manifestsPath = Path.Combine(epicPath, "Manifests");
                    if (Directory.Exists(manifestsPath))
                    {
                        foreach (var manifestFile in Directory.GetFiles(manifestsPath, "*.item"))
                        {
                            try
                            {
                                var content = File.ReadAllText(manifestFile);
                                if (content.Contains("NARAKA", StringComparison.OrdinalIgnoreCase) ||
                                    content.Contains("Bladepoint", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Parse manifest JSON for InstallLocation
                                    var installLocation = ExtractInstallLocationFromManifest(content);
                                    if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                                    {
                                        return installLocation;
                                    }
                                }
                            }
                            catch
                            {
                                // Continue to next manifest
                            }
                        }
                    }
                }
            }
            
            // Method 2: Check common Epic install locations
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var epicGamesPath = Path.Combine(programFiles, "Epic Games", "NARAKA");
            if (Directory.Exists(epicGamesPath))
            {
                return epicGamesPath;
            }
        }
        catch
        {
            // Ignore errors and return null
        }
        
        return null;
    }
    
    /// <summary>
    /// Detect Official/Standalone installation of Naraka
    /// </summary>
    public string? DetectOfficialInstallation()
    {
        try
        {
            // Check registry for official client
            var uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\NARAKA BLADEPOINT";
            
            using var key = Registry.LocalMachine.OpenSubKey(uninstallKey) 
                           ?? Registry.CurrentUser.OpenSubKey(uninstallKey);
            
            if (key != null)
            {
                var installLocation = key.GetValue("InstallLocation") as string;
                if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                {
                    return installLocation;
                }
            }
            
            // Check common install locations
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var commonLocations = new[]
            {
                Path.Combine(programFiles, "NARAKA BLADEPOINT"),
                Path.Combine(programFiles, "Naraka"),
                @"C:\Games\NARAKA BLADEPOINT",
                @"D:\Games\NARAKA BLADEPOINT"
            };
            
            foreach (var location in commonLocations)
            {
                if (Directory.Exists(location))
                {
                    var exePath = Path.Combine(location, "NarakaBladepoint.exe");
                    if (File.Exists(exePath))
                    {
                        return location;
                    }
                }
            }
        }
        catch
        {
            // Ignore errors and return null
        }
        
        return null;
    }
    
    private List<string> ParseLibraryFoldersVdf(string vdfPath)
    {
        var libraries = new List<string>();
        
        try
        {
            var lines = File.ReadAllLines(vdfPath);
            foreach (var line in lines)
            {
                if (line.Contains("\"path\""))
                {
                    var parts = line.Split('"');
                    if (parts.Length >= 4)
                    {
                        var path = parts[3].Replace("\\\\", "\\");
                        var commonPath = Path.Combine(path, "steamapps", "common");
                        if (Directory.Exists(commonPath))
                        {
                            libraries.Add(commonPath);
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore parse errors
        }
        
        return libraries;
    }
    
    private string? ExtractInstallLocationFromManifest(string jsonContent)
    {
        try
        {
            // Simple JSON parsing for InstallLocation field
            var startIndex = jsonContent.IndexOf("\"InstallLocation\"");
            if (startIndex >= 0)
            {
                var colonIndex = jsonContent.IndexOf(':', startIndex);
                var valueStart = jsonContent.IndexOf('"', colonIndex) + 1;
                var valueEnd = jsonContent.IndexOf('"', valueStart);
                
                if (valueStart > 0 && valueEnd > valueStart)
                {
                    return jsonContent.Substring(valueStart, valueEnd - valueStart);
                }
            }
        }
        catch
        {
            // Ignore parse errors
        }
        
        return null;
    }
}

public class GameLocations
{
    public string? SteamPath { get; set; }
    public string? EpicPath { get; set; }
    public string? OfficialPath { get; set; }
    
    public bool HasAnyLocation => !string.IsNullOrEmpty(SteamPath) 
                                 || !string.IsNullOrEmpty(EpicPath) 
                                 || !string.IsNullOrEmpty(OfficialPath);
}
