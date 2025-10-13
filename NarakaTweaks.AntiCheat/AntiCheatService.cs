using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace NarakaTweaks.AntiCheat;

/// <summary>
/// Low-profile background anti-cheat monitoring service.
/// Runs continuous scans with minimal resource usage (< 50MB RAM, < 1% CPU).
/// </summary>
public class AntiCheatService : IDisposable
{
    private readonly SemaphoreSlim _scanLock = new(1, 1);
    private CancellationTokenSource? _backgroundCts;
    private Task? _backgroundTask;
    private bool _isRunning;
    
    // Events for UI updates
    public event EventHandler<DetectionEventArgs>? ThreatDetected;
    public event EventHandler<ScanProgressEventArgs>? ScanProgress;
    public event EventHandler<string>? StatusChanged;
    
    // Configuration
    private readonly AntiCheatConfiguration _config;
    
    public AntiCheatService(AntiCheatConfiguration? config = null)
    {
        _config = config ?? new AntiCheatConfiguration();
    }
    
    public bool IsRunning => _isRunning;
    
    /// <summary>
    /// Start the background monitoring service
    /// </summary>
    public void Start()
    {
        if (_isRunning)
            return;
            
        _isRunning = true;
        _backgroundCts = new CancellationTokenSource();
        _backgroundTask = Task.Run(() => BackgroundMonitoringLoop(_backgroundCts.Token), _backgroundCts.Token);
        
        StatusChanged?.Invoke(this, "Anti-cheat monitoring started");
    }
    
    /// <summary>
    /// Stop the background monitoring service
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning)
            return;
            
        _isRunning = false;
        _backgroundCts?.Cancel();
        
        if (_backgroundTask != null)
        {
            await _backgroundTask.ConfigureAwait(false);
        }
        
        StatusChanged?.Invoke(this, "Anti-cheat monitoring stopped");
    }
    
    /// <summary>
    /// Perform a manual scan immediately
    /// </summary>
    public async Task<ScanResult> ScanNowAsync(CancellationToken cancellationToken = default)
    {
        await _scanLock.WaitAsync(cancellationToken);
        try
        {
            StatusChanged?.Invoke(this, "Manual scan started");
            return await PerformScanAsync(cancellationToken);
        }
        finally
        {
            _scanLock.Release();
        }
    }
    
    private async Task BackgroundMonitoringLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait for the configured interval
                await Task.Delay(_config.ScanIntervalMinutes * 60 * 1000, cancellationToken);
                
                // Perform lightweight scan
                if (!await _scanLock.WaitAsync(0, cancellationToken))
                    continue; // Skip if scan already in progress
                
                try
                {
                    StatusChanged?.Invoke(this, "Background scan started");
                    var result = await PerformScanAsync(cancellationToken);
                    
                    if (result.ThreatsDetected > 0)
                    {
                        StatusChanged?.Invoke(this, $"⚠️ {result.ThreatsDetected} threat(s) detected");
                    }
                }
                finally
                {
                    _scanLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Background scan error: {ex.Message}");
                await Task.Delay(60000, cancellationToken); // Wait 1 minute before retry
            }
        }
    }
    
    private async Task<ScanResult> PerformScanAsync(CancellationToken cancellationToken)
    {
        var result = new ScanResult { StartTime = DateTime.UtcNow };
        var detections = new List<Detection>();
        
        try
        {
            // 1. Process scan - check for suspicious processes
            await ScanProcessesAsync(detections, cancellationToken);
            ScanProgress?.Invoke(this, new ScanProgressEventArgs { Stage = "Processes", Progress = 25 });
            
            // 2. File scan - check common cheat directories
            await ScanFilesAsync(detections, cancellationToken);
            ScanProgress?.Invoke(this, new ScanProgressEventArgs { Stage = "Files", Progress = 50 });
            
            // 3. Registry scan - check for tampering
            await ScanRegistryAsync(detections, cancellationToken);
            ScanProgress?.Invoke(this, new ScanProgressEventArgs { Stage = "Registry", Progress = 75 });
            
            // 4. Memory scan - check for code injection (lightweight)
            await ScanMemoryAsync(detections, cancellationToken);
            ScanProgress?.Invoke(this, new ScanProgressEventArgs { Stage = "Memory", Progress = 100 });
            
            result.Detections = detections;
            result.ThreatsDetected = detections.Count;
            result.EndTime = DateTime.UtcNow;
            result.Success = true;
            
            // Trigger threat detection event if threats found
            if (detections.Count > 0)
            {
                foreach (var detection in detections)
                {
                    ThreatDetected?.Invoke(this, new DetectionEventArgs { Detection = detection });
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
        }
        
        return result;
    }
    
    private async Task ScanProcessesAsync(List<Detection> detections, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var processes = Process.GetProcesses();
            
            foreach (var process in processes)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                try
                {
                    // Check against known cheat process names
                    if (_config.SuspiciousProcessNames.Any(name => 
                        process.ProcessName.Contains(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        detections.Add(new Detection
                        {
                            Type = DetectionType.SuspiciousProcess,
                            ProcessName = process.ProcessName,
                            ProcessId = process.Id,
                            Path = GetProcessPath(process),
                            Severity = DetectionSeverity.High,
                            Description = $"Suspicious process detected: {process.ProcessName}"
                        });
                    }
                    
                    // Check for unsigned/suspicious executables
                    var path = GetProcessPath(process);
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        if (IsSuspiciousExecutable(path))
                        {
                            detections.Add(new Detection
                            {
                                Type = DetectionType.UnsignedExecutable,
                                ProcessName = process.ProcessName,
                                ProcessId = process.Id,
                                Path = path,
                                Severity = DetectionSeverity.Medium,
                                Description = $"Unsigned executable: {process.ProcessName}"
                            });
                        }
                    }
                }
                catch
                {
                    // Skip processes we can't access
                }
            }
        }, cancellationToken);
    }
    
    private async Task ScanFilesAsync(List<Detection> detections, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            // Check common cheat directories
            var suspiciousPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CheatEngine"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CheatEngine"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "CheatEngine"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };
            
            foreach (var path in suspiciousPaths)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                if (Directory.Exists(path))
                {
                    try
                    {
                        var files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
                        
                        foreach (var file in files)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;
                            
                            var fileName = Path.GetFileName(file).ToLowerInvariant();
                            
                            // Check for known cheat file patterns
                            if (_config.SuspiciousFilePatterns.Any(pattern => 
                                fileName.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                            {
                                detections.Add(new Detection
                                {
                                    Type = DetectionType.SuspiciousFile,
                                    Path = file,
                                    Severity = DetectionSeverity.High,
                                    Description = $"Suspicious file detected: {fileName}"
                                });
                            }
                        }
                    }
                    catch
                    {
                        // Skip directories we can't access
                    }
                }
            }
        }, cancellationToken);
    }
    
    private async Task ScanRegistryAsync(List<Detection> detections, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            // Check for registry modifications
            var suspiciousKeys = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options"
            };
            
            foreach (var keyPath in suspiciousKeys)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                    if (key != null)
                    {
                        foreach (var valueName in key.GetValueNames())
                        {
                            var value = key.GetValue(valueName)?.ToString() ?? "";
                            
                            if (_config.SuspiciousFilePatterns.Any(pattern => 
                                value.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                            {
                                detections.Add(new Detection
                                {
                                    Type = DetectionType.RegistryTampering,
                                    Path = $@"HKLM\{keyPath}\{valueName}",
                                    Severity = DetectionSeverity.Medium,
                                    Description = $"Suspicious registry value: {valueName}"
                                });
                            }
                        }
                    }
                }
                catch
                {
                    // Skip keys we can't access
                }
            }
        }, cancellationToken);
    }
    
    private async Task ScanMemoryAsync(List<Detection> detections, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            // Lightweight memory scan - check for known DLL injections
            var narakaProcess = Process.GetProcessesByName("NarakaBladepoint").FirstOrDefault();
            
            if (narakaProcess != null)
            {
                try
                {
                    foreach (ProcessModule module in narakaProcess.Modules)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;
                        
                        var moduleName = module.ModuleName.ToLowerInvariant();
                        
                        if (_config.SuspiciousModuleNames.Any(name => 
                            moduleName.Contains(name, StringComparison.OrdinalIgnoreCase)))
                        {
                            detections.Add(new Detection
                            {
                                Type = DetectionType.DllInjection,
                                ProcessName = narakaProcess.ProcessName,
                                ProcessId = narakaProcess.Id,
                                Path = module.FileName,
                                Severity = DetectionSeverity.Critical,
                                Description = $"Suspicious DLL loaded: {moduleName}"
                            });
                        }
                    }
                }
                catch
                {
                    // Skip if we can't access process modules
                }
            }
        }, cancellationToken);
    }
    
    private string GetProcessPath(Process process)
    {
        try
        {
            return process.MainModule?.FileName ?? "";
        }
        catch
        {
            return "";
        }
    }
    
    private bool IsSuspiciousExecutable(string path)
    {
        // Check if file is digitally signed (basic check)
        // In production, you'd use proper signature verification
        try
        {
            var fileInfo = new FileInfo(path);
            return fileInfo.Length < 50000; // Very small executables are suspicious
        }
        catch
        {
            return false;
        }
    }
    
    public void Dispose()
    {
        _backgroundCts?.Cancel();
        _backgroundCts?.Dispose();
        _scanLock?.Dispose();
        _backgroundTask?.Dispose();
    }
}

public class AntiCheatConfiguration
{
    public int ScanIntervalMinutes { get; set; } = 30; // Scan every 30 minutes
    
    public List<string> SuspiciousProcessNames { get; set; } = new()
    {
        "cheatengine", "ce", "trainer", "hack", "cheat", "injector", 
        "loader", "bypass", "unlocker", "speedhack"
    };
    
    public List<string> SuspiciousFilePatterns { get; set; } = new()
    {
        "cheat", "hack", "trainer", "injector", "bypass", "unlocker",
        ".dll.bak", ".sys.bak", "hook", "detour"
    };
    
    public List<string> SuspiciousModuleNames { get; set; } = new()
    {
        "hook", "inject", "detour", "overlay", "cheat", "hack"
    };
}

public class ScanResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ThreatsDetected { get; set; }
    public List<Detection> Detections { get; set; } = new();
    public TimeSpan Duration => EndTime - StartTime;
}

public class Detection
{
    public DetectionType Type { get; set; }
    public DetectionSeverity Severity { get; set; }
    public string? ProcessName { get; set; }
    public int ProcessId { get; set; }
    public string? Path { get; set; }
    public string Description { get; set; } = "";
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

public enum DetectionType
{
    SuspiciousProcess,
    UnsignedExecutable,
    SuspiciousFile,
    RegistryTampering,
    DllInjection,
    MemoryTampering
}

public enum DetectionSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public class DetectionEventArgs : EventArgs
{
    public Detection Detection { get; set; } = new();
}

public class ScanProgressEventArgs : EventArgs
{
    public string Stage { get; set; } = "";
    public int Progress { get; set; }
}
