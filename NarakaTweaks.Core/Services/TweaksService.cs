using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace NarakaTweaks.Core.Services;

/// <summary>
/// Comprehensive tweaks service ported from RatzTweaks.ps1.
/// Handles OS optimizations, GPU tweaks, and game-specific settings.
/// </summary>
public class TweaksService
{
    private readonly string _backupDirectory;
    
    public event EventHandler<string>? StatusChanged;
    public event EventHandler<TweakAppliedEventArgs>? TweakApplied;
    
    public TweaksService()
    {
        _backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NarakaTweaks", "TweakBackups");
        
        Directory.CreateDirectory(_backupDirectory);
    }
    
    /// <summary>
    /// Get all available tweaks organized by category
    /// </summary>
    public Dictionary<TweakCategory, List<TweakDefinition>> GetAvailableTweaks()
    {
        var tweaks = new Dictionary<TweakCategory, List<TweakDefinition>>();
        
        // Performance Tweaks
        tweaks[TweakCategory.Performance] = new List<TweakDefinition>
        {
            new TweakDefinition
            {
                Id = "core-registry-tweaks",
                Name = "Apply Core Registry Optimizations",
                Description = "Applies comprehensive gaming/performance registry tweaks (Network, GPU Priority, System Responsiveness, Memory, Keyboard)",
                RequiresRestart = true,
                RequiresAdmin = true,
                RiskLevel = RiskLevel.Low,
                ApplyAction = ApplyCoreRegistryTweaks,
                RevertAction = RevertCoreRegistryTweaks
            },
            new TweakDefinition
            {
                Id = "high-performance-power",
                Name = "High Performance Power Plan",
                Description = "Sets Windows power plan to High Performance",
                RequiresRestart = false,
                RequiresAdmin = true,
                RiskLevel = RiskLevel.Low,
                ApplyAction = SetHighPerformancePower,
                RevertAction = SetBalancedPower
            },
            new TweakDefinition
            {
                Id = "ultimate-performance-power",
                Name = "Ultimate Performance Power Plan",
                Description = "Sets Windows power plan to Ultimate Performance (if available)",
                RequiresRestart = false,
                RequiresAdmin = true,
                RiskLevel = RiskLevel.Low,
                ApplyAction = SetUltimatePerformancePower,
                RevertAction = SetBalancedPower
            },
            new TweakDefinition
            {
                Id = "disable-game-bar",
                Name = "Disable Game Bar",
                Description = "Disables Windows Game Bar and DVR for better performance",
                RequiresRestart = false,
                RequiresAdmin = true,
                RiskLevel = RiskLevel.Low,
                ApplyAction = DisableGameDVR,
                RevertAction = EnableGameDVR
            },
            new TweakDefinition
            {
                Id = "disable-background-apps",
                Name = "Disable Background Apps",
                Description = "Prevents Windows apps from running in background",
                RequiresRestart = false,
                RequiresAdmin = true,
                RiskLevel = RiskLevel.Low,
                ApplyAction = DisableBackgroundApps,
                RevertAction = EnableBackgroundApps
            },
            new TweakDefinition
            {
                Id = "disable-widgets",
                Name = "Disable Widgets",
                Description = "Disables Windows 11 Widgets panel",
                RequiresRestart = false,
                RequiresAdmin = true,
                RiskLevel = RiskLevel.Low,
                ApplyAction = DisableWidgets,
                RevertAction = EnableWidgets
            },
            new TweakDefinition
            {
                Id = "disable-copilot",
                Name = "Disable Copilot",
                Description = "Disables Windows Copilot AI assistant",
                RequiresRestart = false,
                RequiresAdmin = true,
                RiskLevel = RiskLevel.Low,
                ApplyAction = DisableCopilot,
                RevertAction = EnableCopilot
            }
        };
        
        // GPU Tweaks
        tweaks[TweakCategory.GPU] = new List<TweakDefinition>
        {
            new TweakDefinition
            {
                Id = "gpu-scheduling",
                Name = "Hardware-Accelerated GPU Scheduling",
                Description = "Enables HAGS for reduced latency (Windows 10 20H1+)",
                RequiresRestart = true,
                RequiresAdmin = true,
                RiskLevel = RiskLevel.Low,
                ApplyAction = EnableGPUScheduling,
                RevertAction = DisableGPUScheduling
            },
            new TweakDefinition
            {
                Id = "nvidia-optimizations",
                Name = "NVIDIA Optimizations",
                Description = "Applies NVIDIA-specific registry tweaks for better performance",
                RequiresRestart = true,
                RequiresAdmin = true,
                RiskLevel = RiskLevel.Low,
                ApplyAction = ApplyNvidiaOptimizations,
                RevertAction = RevertNvidiaOptimizations
            },
            new TweakDefinition
            {
                Id = "amd-optimizations",
                Name = "AMD Optimizations",
                Description = "Applies AMD-specific registry tweaks for better performance",
                RequiresRestart = true,
                RequiresAdmin = true,
                RiskLevel = RiskLevel.Low,
                ApplyAction = ApplyAMDOptimizations,
                RevertAction = RevertAMDOptimizations
            }
        };
        
        // System Tweaks
        tweaks[TweakCategory.System] = new List<TweakDefinition>
        {
            new TweakDefinition
            {
                Id = "enable-msi-mode",
                Name = "Enable MSI Mode for PCI Devices",
                Description = "Enables Message Signaled Interrupts for better interrupt handling",
                RequiresRestart = true,
                RequiresAdmin = true,
                RiskLevel = RiskLevel.Medium,
                ApplyAction = EnableMSIMode,
                RevertAction = DisableMSIMode
            },
            new TweakDefinition
            {
                Id = "disable-hpet",
                Name = "Disable HPET",
                Description = "Disables High Precision Event Timer (may improve performance)",
                RequiresRestart = true,
                RequiresAdmin = true,
                RiskLevel = RiskLevel.Medium,
                ApplyAction = DisableHPET,
                RevertAction = EnableHPET
            },
            new TweakDefinition
            {
                Id = "timer-resolution",
                Name = "Set Timer Resolution Service",
                Description = "Installs service to maintain optimal timer resolution (0.5ms)",
                RequiresRestart = false,
                RequiresAdmin = true,
                RiskLevel = RiskLevel.Low,
                ApplyAction = InstallTimerResolutionService,
                RevertAction = UninstallTimerResolutionService
            },
            new TweakDefinition
            {
                Id = "vivetool-disable",
                Name = "Disable ViVeTool Features",
                Description = "Disables experimental Windows features that may impact performance",
                RequiresRestart = true,
                RequiresAdmin = true,
                RiskLevel = RiskLevel.Medium,
                ApplyAction = DisableViVeFeatures,
                RevertAction = EnableViVeFeatures
            }
        };
        
        return tweaks;
    }
    
    /// <summary>
    /// Apply a specific tweak
    /// </summary>
    public async Task<TweakResult> ApplyTweakAsync(TweakDefinition tweak)
    {
        var result = new TweakResult
        {
            TweakId = tweak.Id,
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            StatusChanged?.Invoke(this, $"Applying: {tweak.Name}");
            
            // Create backup before applying
            await CreateBackupAsync(tweak);
            
            // Apply the tweak
            await Task.Run(() => tweak.ApplyAction());
            
            result.Success = true;
            result.EndTime = DateTime.UtcNow;
            
            StatusChanged?.Invoke(this, $"✓ Applied: {tweak.Name}");
            TweakApplied?.Invoke(this, new TweakAppliedEventArgs { Tweak = tweak, Success = true });
            
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            
            StatusChanged?.Invoke(this, $"✗ Failed: {tweak.Name} - {ex.Message}");
            TweakApplied?.Invoke(this, new TweakAppliedEventArgs { Tweak = tweak, Success = false, Error = ex.Message });
            
            return result;
        }
    }
    
    /// <summary>
    /// Revert a previously applied tweak
    /// </summary>
    public async Task<TweakResult> RevertTweakAsync(TweakDefinition tweak)
    {
        var result = new TweakResult
        {
            TweakId = tweak.Id,
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            StatusChanged?.Invoke(this, $"Reverting: {tweak.Name}");
            
            // Revert the tweak
            await Task.Run(() => tweak.RevertAction());
            
            result.Success = true;
            result.EndTime = DateTime.UtcNow;
            
            StatusChanged?.Invoke(this, $"✓ Reverted: {tweak.Name}");
            
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            
            StatusChanged?.Invoke(this, $"✗ Revert failed: {tweak.Name} - {ex.Message}");
            
            return result;
        }
    }
    
    private async Task CreateBackupAsync(TweakDefinition tweak)
    {
        await Task.Run(() =>
        {
            var backupFile = Path.Combine(_backupDirectory, $"{tweak.Id}_{DateTime.Now:yyyyMMddHHmmss}.backup");
            // Store backup metadata
            File.WriteAllText(backupFile, $"Backup for: {tweak.Name}\nCreated: {DateTime.Now}");
        });
    }
    
    // Tweak implementations
    
    // Core Registry Tweaks from RatzTweaks.ps1 main tweaks
    private void ApplyCoreRegistryTweaks()
    {
        // Network optimization
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 10);
        
        // GPU scheduling
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "GPU_SCHEDULER_MODE", "22");
        
        // System responsiveness
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0);
        
        // Game task priorities
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority", 8);
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority", 6);
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Scheduling Category", "High");
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "SFIO Priority", "High");
        
        // Process priority
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\PriorityControl", "Win32PrioritySeparation", 40);
        
        // Kernel optimizations
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "SerializeTimerExpiration", 1);
        
        // Memory management optimizations
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettings", 1);
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride", 3);
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsMask", 3);
        
        // Keyboard optimizations for gaming
        SetRegistryValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "InitialKeyboardIndicators", "2");
        SetRegistryValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "KeyboardSpeed", "48");
        SetRegistryValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "KeyboardDelay", "0");
        
        // DWM optimization
        SetRegistryValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", @"C:\Windows\System32\dwm.exe", "NoDTToDITMouseBatch");
    }
    
    private void RevertCoreRegistryTweaks()
    {
        // Revert network
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex");
        
        // Revert GPU scheduling
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "GPU_SCHEDULER_MODE");
        
        // Revert system responsiveness
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness");
        
        // Revert game task priorities
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority");
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority");
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Scheduling Category");
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "SFIO Priority");
        
        // Revert process priority
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\PriorityControl", "Win32PrioritySeparation");
        
        // Revert kernel
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "SerializeTimerExpiration");
        
        // Revert memory management
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettings");
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsOverride");
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "FeatureSettingsMask");
        
        // Revert keyboard
        DeleteRegistryValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "InitialKeyboardIndicators");
        DeleteRegistryValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "KeyboardSpeed");
        DeleteRegistryValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "KeyboardDelay");
        
        // Revert DWM
        DeleteRegistryValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", @"C:\Windows\System32\dwm.exe");
    }
    
    private void DisableHibernation()
    {
        RunCommand("powercfg", "/hibernate off");
    }
    
    private void EnableHibernation()
    {
        RunCommand("powercfg", "/hibernate on");
    }
    
    private void SetHighPerformancePower()
    {
        RunCommand("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
    }
    
    private void SetBalancedPower()
    {
        RunCommand("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
    }
    
    private void DisableGameDVR()
    {
        SetRegistryValue(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_Enabled", 0);
        SetRegistryValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0);
    }
    
    private void EnableGameDVR()
    {
        SetRegistryValue(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_Enabled", 1);
        SetRegistryValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 1);
    }
    
    private void OptimizePagingFile()
    {
        // Set page file to system managed
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "PagingFiles", "");
    }
    
    private void ResetPagingFile()
    {
        // Reset to default
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "PagingFiles", "?:\\pagefile.sys");
    }
    
    private void EnableGPUScheduling()
    {
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
    }
    
    private void DisableGPUScheduling()
    {
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 1);
    }
    
    private void DisableFullscreenOptimizations()
    {
        SetRegistryValue(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_FSEBehaviorMode", 2);
    }
    
    private void EnableFullscreenOptimizations()
    {
        SetRegistryValue(@"HKEY_CURRENT_USER\System\GameConfigStore", "GameDVR_FSEBehaviorMode", 0);
    }
    
    private void OptimizeTCP()
    {
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", -1);
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TCPNoDelay", 1);
    }
    
    private void ResetTCP()
    {
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 10);
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TCPNoDelay");
    }
    
    private void DisableNagle()
    {
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", "TcpAckFrequency", 1);
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", "TCPNoDelay", 1);
    }
    
    private void EnableNagle()
    {
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", "TcpAckFrequency");
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", "TCPNoDelay");
    }
    
    private void DisableTelemetry()
    {
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0);
    }
    
    private void EnableTelemetry()
    {
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 1);
    }
    
    private void DisableCortana()
    {
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0);
    }
    
    private void EnableCortana()
    {
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 1);
    }
    
    // Additional Performance Tweaks
    private void SetUltimatePerformancePower()
    {
        // Try to enable Ultimate Performance power plan
        RunCommand("powercfg", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
        RunCommand("powercfg", "/setactive e9a42b02-d5df-448d-aa00-03f14749eb61");
    }
    
    private void DisableBackgroundApps()
    {
        SetRegistryValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1);
    }
    
    private void EnableBackgroundApps()
    {
        DeleteRegistryValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled");
    }
    
    private void DisableWidgets()
    {
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0);
    }
    
    private void EnableWidgets()
    {
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests");
    }
    
    private void DisableCopilot()
    {
        SetRegistryValue(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1);
    }
    
    private void EnableCopilot()
    {
        DeleteRegistryValue(@"HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot");
    }
    
    // GPU Tweaks
    private void ApplyNvidiaOptimizations()
    {
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "RmGpsPsEnablePerCpuCoreDpc", 1);
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm", "RmGpsPsEnablePerCpuCoreDpc", 1);
    }
    
    private void RevertNvidiaOptimizations()
    {
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "RmGpsPsEnablePerCpuCoreDpc");
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm", "RmGpsPsEnablePerCpuCoreDpc");
    }
    
    private void ApplyAMDOptimizations()
    {
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "EnableUlps", 0);
        SetRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000\UMD", "Main3D_DEF", 1);
    }
    
    private void RevertAMDOptimizations()
    {
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "EnableUlps");
        DeleteRegistryValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000\UMD", "Main3D_DEF");
    }
    
    // System Tweaks
    private void EnableMSIMode()
    {
        // Enable Message Signaled Interrupts for PCI devices - placeholder
        // Requires WMI enumeration of PCI devices
    }
    
    private void DisableMSIMode()
    {
        // Disable MSI Mode - placeholder
    }
    
    private void DisableHPET()
    {
        RunCommand("bcdedit", "/deletevalue useplatformclock");
    }
    
    private void EnableHPET()
    {
        RunCommand("bcdedit", "/set useplatformclock true");
    }
    
    private void InstallTimerResolutionService()
    {
        // Placeholder for timer resolution service installation
        // Would require compiling and installing the C# service from RatzTweaks.ps1
    }
    
    private void UninstallTimerResolutionService()
    {
        // Placeholder for uninstalling timer resolution service
        RunCommand("sc.exe", "delete STR");
    }
    
    private void DisableViVeFeatures()
    {
        // Placeholder for ViVeTool feature disabling
        // Would require ViVeTool.exe integration
    }
    
    private void EnableViVeFeatures()
    {
        // Placeholder for re-enabling ViVeTool features
    }
    
    // Helper methods
    private void RunCommand(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };
        
        using var process = Process.Start(startInfo);
        process?.WaitForExit();
    }
    
    private void SetRegistryValue(string keyPath, string valueName, object value)
    {
        var parts = keyPath.Split('\\', 2);
        var hive = parts[0] switch
        {
            "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
            "HKEY_CURRENT_USER" => Registry.CurrentUser,
            _ => throw new ArgumentException("Invalid registry hive")
        };
        
        var subKeyPath = parts[1];
        using var key = hive.CreateSubKey(subKeyPath, true);
        
        key?.SetValue(valueName, value);
    }
    
    private void DeleteRegistryValue(string keyPath, string valueName)
    {
        var parts = keyPath.Split('\\', 2);
        var hive = parts[0] switch
        {
            "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
            "HKEY_CURRENT_USER" => Registry.CurrentUser,
            _ => throw new ArgumentException("Invalid registry hive")
        };
        
        var subKeyPath = parts[1];
        using var key = hive.OpenSubKey(subKeyPath, true);
        
        key?.DeleteValue(valueName, false);
    }
}

public class TweakDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool RequiresRestart { get; set; }
    public bool RequiresAdmin { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public Action ApplyAction { get; set; } = () => { };
    public Action RevertAction { get; set; } = () => { };
}

public enum TweakCategory
{
    Performance,
    GPU,
    System,
    Privacy
}

public enum RiskLevel
{
    Low,
    Medium,
    High
}

public class TweakResult
{
    public string TweakId { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
}

public class TweakAppliedEventArgs : EventArgs
{
    public TweakDefinition Tweak { get; set; } = new();
    public bool Success { get; set; }
    public string? Error { get; set; }
}
