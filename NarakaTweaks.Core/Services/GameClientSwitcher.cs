using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace NarakaTweaks.Core.Services;

/// <summary>
/// Manages switching between different Naraka game clients and authentication servers.
/// Handles Steam/Epic Games validation to force revalidation of game files.
/// </summary>
public class GameClientSwitcher
{
    private readonly string _steamExePath;
    private readonly string _epicGamesLauncherPath;
    
    public event EventHandler<string>? StatusChanged;
    
    public GameClientSwitcher()
    {
        _steamExePath = FindSteamExecutable();
        _epicGamesLauncherPath = FindEpicGamesLauncher();
    }
    
    /// <summary>
    /// Switch to a different game client by extracting specific files and forcing validation
    /// </summary>
    public async Task<SwitchResult> SwitchClientAsync(
        string narakaInstallPath,
        string sourceFilesPath,
        ClientRegion targetRegion,
        GamePlatform platform)
    {
        var result = new SwitchResult
        {
            StartTime = DateTime.UtcNow,
            TargetRegion = targetRegion,
            Platform = platform
        };
        
        try
        {
            StatusChanged?.Invoke(this, $"Switching to {targetRegion} client...");
            
            // Step 1: Backup current configuration
            StatusChanged?.Invoke(this, "Backing up current configuration...");
            var backupPath = await BackupCurrentConfigAsync(narakaInstallPath);
            result.BackupPath = backupPath;
            
            // Step 2: Copy authentication files from source
            StatusChanged?.Invoke(this, "Installing authentication files...");
            var filesToCopy = GetAuthenticationFiles(sourceFilesPath, targetRegion);
            var copiedFiles = await CopyAuthenticationFilesAsync(filesToCopy, narakaInstallPath);
            result.ModifiedFiles.AddRange(copiedFiles);
            
            // Step 3: Update configuration files
            StatusChanged?.Invoke(this, "Updating configuration...");
            await UpdateConfigurationAsync(narakaInstallPath, targetRegion);
            
            // Step 4: Force game validation
            StatusChanged?.Invoke(this, "Initiating game file validation...");
            var validationSuccess = await ForceGameValidationAsync(narakaInstallPath, platform);
            
            if (!validationSuccess)
            {
                StatusChanged?.Invoke(this, "Warning: Could not automatically trigger validation. Please verify game files manually.");
            }
            
            result.Success = true;
            result.EndTime = DateTime.UtcNow;
            StatusChanged?.Invoke(this, $"Successfully switched to {targetRegion} client!");
            
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            StatusChanged?.Invoke(this, $"Switch failed: {ex.Message}");
            return result;
        }
    }
    
    /// <summary>
    /// Detect which platform Naraka is installed on
    /// </summary>
    public GamePlatform DetectPlatform(string narakaInstallPath)
    {
        // Check for Steam
        var steamAppId = "1203220"; // Naraka's Steam App ID
        var steamAppsPath = Path.Combine(narakaInstallPath, "..", "..", "steamapps");
        
        if (Directory.Exists(steamAppsPath))
        {
            var manifestPath = Path.Combine(steamAppsPath, $"appmanifest_{steamAppId}.acf");
            if (File.Exists(manifestPath))
                return GamePlatform.Steam;
        }
        
        // Check for Epic Games
        var epicManifestPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Epic", "EpicGamesLauncher", "Data", "Manifests");
        
        if (Directory.Exists(epicManifestPath))
        {
            var manifests = Directory.GetFiles(epicManifestPath, "*.item");
            foreach (var manifest in manifests)
            {
                try
                {
                    var content = File.ReadAllText(manifest);
                    if (content.Contains("Naraka", StringComparison.OrdinalIgnoreCase))
                        return GamePlatform.EpicGames;
                }
                catch { }
            }
        }
        
        return GamePlatform.Standalone;
    }
    
    /// <summary>
    /// Get the current client region based on configuration files
    /// </summary>
    public ClientRegion DetectCurrentRegion(string narakaInstallPath)
    {
        try
        {
            // Check configuration files for region indicators
            var configPath = Path.Combine(narakaInstallPath, "Config", "region.cfg");
            
            if (File.Exists(configPath))
            {
                var content = File.ReadAllText(configPath).ToLowerInvariant();
                
                if (content.Contains("china") || content.Contains("cn"))
                    return ClientRegion.China;
                
                if (content.Contains("japan") || content.Contains("jp"))
                    return ClientRegion.Japan;
                
                if (content.Contains("sea") || content.Contains("asia"))
                    return ClientRegion.SEA;
            }
            
            // Default to Global if can't determine
            return ClientRegion.Global;
        }
        catch
        {
            return ClientRegion.Global;
        }
    }
    
    private List<FileMapping> GetAuthenticationFiles(string sourceFilesPath, ClientRegion region)
    {
        // Define which files need to be copied for each region
        var files = new List<FileMapping>();
        
        // Common authentication files
        files.Add(new FileMapping
        {
            SourcePath = Path.Combine(sourceFilesPath, "Auth", "login.dll"),
            TargetRelativePath = "Binaries\\Win64\\login.dll"
        });
        
        files.Add(new FileMapping
        {
            SourcePath = Path.Combine(sourceFilesPath, "Auth", "auth_config.json"),
            TargetRelativePath = "Config\\auth_config.json"
        });
        
        // Region-specific files
        if (region == ClientRegion.China)
        {
            files.Add(new FileMapping
            {
                SourcePath = Path.Combine(sourceFilesPath, "CN", "region.cfg"),
                TargetRelativePath = "Config\\region.cfg"
            });
        }
        else
        {
            files.Add(new FileMapping
            {
                SourcePath = Path.Combine(sourceFilesPath, "Global", "region.cfg"),
                TargetRelativePath = "Config\\region.cfg"
            });
        }
        
        return files;
    }
    
    private async Task<string> BackupCurrentConfigAsync(string narakaInstallPath)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NarakaTweaks", "Backups", timestamp);
        
        Directory.CreateDirectory(backupDir);
        
        // Backup important configuration files
        var filesToBackup = new[]
        {
            Path.Combine(narakaInstallPath, "Config", "auth_config.json"),
            Path.Combine(narakaInstallPath, "Config", "region.cfg"),
            Path.Combine(narakaInstallPath, "Binaries", "Win64", "login.dll")
        };
        
        await Task.Run(() =>
        {
            foreach (var file in filesToBackup)
            {
                if (File.Exists(file))
                {
                    var relativePath = Path.GetRelativePath(narakaInstallPath, file);
                    var backupPath = Path.Combine(backupDir, relativePath);
                    
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                    File.Copy(file, backupPath, overwrite: true);
                }
            }
        });
        
        return backupDir;
    }
    
    private async Task<List<string>> CopyAuthenticationFilesAsync(
        List<FileMapping> files,
        string narakaInstallPath)
    {
        var copiedFiles = new List<string>();
        
        await Task.Run(() =>
        {
            foreach (var file in files)
            {
                if (!File.Exists(file.SourcePath))
                {
                    StatusChanged?.Invoke(this, $"Warning: Source file not found: {file.SourcePath}");
                    continue;
                }
                
                var targetPath = Path.Combine(narakaInstallPath, file.TargetRelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                
                File.Copy(file.SourcePath, targetPath, overwrite: true);
                copiedFiles.Add(targetPath);
            }
        });
        
        return copiedFiles;
    }
    
    private async Task UpdateConfigurationAsync(string narakaInstallPath, ClientRegion region)
    {
        await Task.Run(() =>
        {
            // Update any additional configuration that needs region-specific values
            var configPath = Path.Combine(narakaInstallPath, "Config", "game.ini");
            
            if (File.Exists(configPath))
            {
                var lines = File.ReadAllLines(configPath).ToList();
                
                // Update region setting
                var regionLineIndex = lines.FindIndex(l => l.StartsWith("Region="));
                var newRegionLine = $"Region={region}";
                
                if (regionLineIndex >= 0)
                    lines[regionLineIndex] = newRegionLine;
                else
                    lines.Add(newRegionLine);
                
                File.WriteAllLines(configPath, lines);
            }
        });
    }
    
    private async Task<bool> ForceGameValidationAsync(string narakaInstallPath, GamePlatform platform)
    {
        try
        {
            switch (platform)
            {
                case GamePlatform.Steam:
                    return await ForceSteamValidationAsync(narakaInstallPath);
                
                case GamePlatform.EpicGames:
                    return await ForceEpicValidationAsync(narakaInstallPath);
                
                default:
                    // For standalone, no automatic validation available
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }
    
    private async Task<bool> ForceSteamValidationAsync(string narakaInstallPath)
    {
        if (string.IsNullOrEmpty(_steamExePath) || !File.Exists(_steamExePath))
            return false;
        
        await Task.Run(() =>
        {
            // Steam App ID for Naraka
            var appId = "1203220";
            
            // Method 1: Delete a non-critical file to trigger validation
            var fileToDelete = Path.Combine(narakaInstallPath, "Binaries", "Win64", "login.dll");
            if (File.Exists(fileToDelete))
            {
                File.Delete(fileToDelete);
            }
            
            // Method 2: Launch Steam with validation command
            var startInfo = new ProcessStartInfo
            {
                FileName = _steamExePath,
                Arguments = $"-validate {appId}",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            Process.Start(startInfo);
        });
        
        return true;
    }
    
    private async Task<bool> ForceEpicValidationAsync(string narakaInstallPath)
    {
        if (string.IsNullOrEmpty(_epicGamesLauncherPath) || !File.Exists(_epicGamesLauncherPath))
            return false;
        
        await Task.Run(() =>
        {
            // Epic Games doesn't have a simple command-line validation
            // Best we can do is launch the launcher to the library
            var startInfo = new ProcessStartInfo
            {
                FileName = _epicGamesLauncherPath,
                Arguments = "-openlauncheronly",
                UseShellExecute = true
            };
            
            Process.Start(startInfo);
        });
        
        return false; // User needs to manually verify
    }
    
    private string FindSteamExecutable()
    {
        // Check registry for Steam install path
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var steamPath = key?.GetValue("SteamExe")?.ToString();
            
            if (!string.IsNullOrEmpty(steamPath) && File.Exists(steamPath))
                return steamPath;
        }
        catch { }
        
        // Check common install locations
        var commonPaths = new[]
        {
            @"C:\Program Files (x86)\Steam\steam.exe",
            @"C:\Program Files\Steam\steam.exe"
        };
        
        return commonPaths.FirstOrDefault(File.Exists) ?? "";
    }
    
    private string FindEpicGamesLauncher()
    {
        var commonPaths = new[]
        {
            @"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win32\EpicGamesLauncher.exe",
            @"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe"
        };
        
        return commonPaths.FirstOrDefault(File.Exists) ?? "";
    }
}

public class SwitchResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ClientRegion TargetRegion { get; set; }
    public GamePlatform Platform { get; set; }
    public string? BackupPath { get; set; }
    public List<string> ModifiedFiles { get; set; } = new();
    public TimeSpan Duration => EndTime - StartTime;
}

public enum GamePlatform
{
    Steam,
    EpicGames,
    Standalone
}

public class FileMapping
{
    public string SourcePath { get; set; } = "";
    public string TargetRelativePath { get; set; } = "";
}
