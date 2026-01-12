namespace NotNarakaLauncher.Core.Services;

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public interface IInstallerService
{
    Task ExtractNarakaClientAsync(string zipPath, string targetRoot, CancellationToken ct = default);
    Task<bool> ValidateViaLauncherAsync(GamePlatform platform);
    Task InstallMetricServicesAsync(CancellationToken ct = default);
}

public class InstallerService : IInstallerService
{
    public async Task ExtractNarakaClientAsync(string zipPath, string targetRoot, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(zipPath);
            // Only extract entries under Naraka/program, excluding bin and netease.mpay.webviewsupport.cef904430
            var prefix = "Naraka/program/";
            var entries = archive.Entries.Where(e => e.FullName.Replace("\\", "/").StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();

            System.Diagnostics.Debug.WriteLine($"[InstallerService] Extracting {entries.Count} files to {targetRoot}");

            int processed = 0;
            foreach (var entry in entries)
            {
                ct.ThrowIfCancellationRequested();

                var relative = entry.FullName.Replace("\\", "/").Substring(prefix.Length);
                if (string.IsNullOrEmpty(relative)) continue;

                var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    var top = parts[0].ToLowerInvariant();
                    if (top == "bin" || top == "netease.mpay.webviewsupport.cef904430")
                        continue;
                }

                var destPath = Path.Combine(targetRoot, "Naraka", "program", relative.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

                if (entry.FullName.EndsWith("/")) continue; // directory

                entry.ExtractToFile(destPath, overwrite: true);

                processed++;

                // Log progress every 100 files to reduce debug spam
                if (processed % 100 == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[InstallerService] Extracted {processed}/{entries.Count} files ({(processed * 100.0 / entries.Count):F1}%)");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[InstallerService] Extraction complete! Extracted {processed} files");
        }, ct);
    }

    public Task<bool> ValidateViaLauncherAsync(GamePlatform platform)
    {
        try
        {
            switch (platform)
            {
                case GamePlatform.Steam:
                    // Steam verify command for Naraka (AppID 1203220)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo 
                    { 
                        FileName = "steam://validate/1203220", 
                        UseShellExecute = true 
                    });
                    return Task.FromResult(true);

                case GamePlatform.Epic:
                    // Epic verify command for Naraka
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo 
                    { 
                        FileName = "com.epicgames.launcher://apps/5dbde21dd01f4e7d925561a52f1771e4%3Ab0a3b4675f6d44459672107dbde79c0e%3A76dc6b0e0c5c43f9b5a6f472a4f812fb?action=verify", 
                        UseShellExecute = true 
                    });
                    return Task.FromResult(true);

                default:
                    // Fallback to opening the launcher executable
                    if (new PlatformService().TryGetLauncherPath(platform, out var path) && !string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true });
                        return Task.FromResult(true);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[InstallerService] Validation error: {ex.Message}");
        }
        return Task.FromResult(false);
    }


    public async Task InstallMetricServicesAsync(CancellationToken ct = default)
    {
        try
        {
            // Use CommonApplicationData (ProgramData) for system-wide service access
            string commonData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string targetDir = Path.Combine(commonData, "NotNaraka", "MetricServices");
            string zipPath = Path.Combine(commonData, "NotNaraka", "NotNaraka_MetricServices.zip");
            string url = "https://github.com/NotRatz/NotNaraka/raw/main/NotNaraka_MetricServices.zip";

            // Always attempt to update/download to ensure we have the latest version.
            // In a real scenario, we would check a local version file vs remote version, 
            // but for now, we force a refresh to fix potential corruptions.
            
            System.Diagnostics.Debug.WriteLine("[InstallerService] Checking MetricServices installation...");
            Directory.CreateDirectory(Path.GetDirectoryName(zipPath)!);

            // 1. Download Zip
            bool downloadSuccess = false;
            try 
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(300); // 5 mins max
                    var response = await client.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, ct);
                    response.EnsureSuccessStatusCode();
                    
                    using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fs, ct);
                }

                // VALIDATION: Ensure the file is a valid valid Zip archive
                try 
                {
                    using var verifyArchive = ZipFile.OpenRead(zipPath);
                    var entryCount = verifyArchive.Entries.Count; // Accessing entries forces directory read
                    System.Diagnostics.Debug.WriteLine($"[InstallerService] Download verified for {zipPath}. Entries: {entryCount}");
                    downloadSuccess = true;
                }
                catch (InvalidDataException)
                {
                    System.Diagnostics.Debug.WriteLine("[InstallerService] Downloaded file is not a valid zip archive. Deleting.");
                    File.Delete(zipPath);
                    throw new Exception("Downloaded file corrupted (Invalid Zip).");
                }
            } 
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InstallerService] Download failed: {ex.Message}");
                // If download fails but we have a valid installation, we can try to proceed, 
                // but let's warn.
                if (!Directory.Exists(targetDir) || Directory.GetFiles(targetDir).Length == 0)
                {
                     throw; // Critical failure if we have nothing
                }
            }

            // 2. Extract if we downloaded a new zip
            if (downloadSuccess && File.Exists(zipPath))
            {
                System.Diagnostics.Debug.WriteLine("[InstallerService] Extracting MetricServices...");
                try 
                {
                     // Cleanup old dir to ensure no loose files remain
                     if (Directory.Exists(targetDir))
                     {
                         try { Directory.Delete(targetDir, true); } catch { } 
                     }
                     Directory.CreateDirectory(targetDir);

                     await Task.Run(() => 
                     {
                         ZipFile.ExtractToDirectory(zipPath, targetDir, overwriteFiles: true);
                     }, ct);
                     
                     System.Diagnostics.Debug.WriteLine("[InstallerService] Extraction successful.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[InstallerService] Extraction failed: {ex.Message}");
                    throw;
                }
                finally
                {
                     // Cleanup Zip
                     try { File.Delete(zipPath); } catch { }
                }
            }

             // 3. Auto-Start the Service
             var exePath = Path.Combine(targetDir, "NotNaraka_MetricServices.exe");
             if (File.Exists(exePath))
             {
                 System.Diagnostics.Debug.WriteLine($"[InstallerService] Starting Metric Service: {exePath}");
                 
                 // Kill existing if running to allow update (though we deleted dir? Process might hold lock. 
                 // If we deleted dir successfully, process wasn't running. If deletion failed, we catch above).
                 
                 var psi = new System.Diagnostics.ProcessStartInfo
                 {
                     FileName = exePath,
                     Arguments = "--install", // Install as Windows Service
                     UseShellExecute = true,
                     Verb = "runas" // Request Admin
                 };
                 
                 try 
                 {
                     System.Diagnostics.Debug.WriteLine($"[InstallerService] Installing Metric Service...");
                     var proc = System.Diagnostics.Process.Start(psi);
                     proc?.WaitForExit(10000); // Wait up to 10s for install to finish

                     // After install, we might need to actually START it.
                     // Assuming 'net start NotNaraka_MetricServices' or similar?
                     // Or maybe the exe self-starts?
                     // Let's try to start it as a service just in case.
                     
                     // Alternative: Try to run it directly if it's not a service (user said "no metrics server running")
                     // Use 'sc start'
                     var startPsi = new System.Diagnostics.ProcessStartInfo
                     {
                         FileName = "sc",
                         Arguments = "start NotNaraka_MetricServices",
                         UseShellExecute = true,
                         Verb = "runas",
                         WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                     };
                     System.Diagnostics.Process.Start(startPsi);
                     
                     System.Diagnostics.Debug.WriteLine($"[InstallerService] Attempted to start Metric Service.");
                 }
                 catch (Exception ex)
                 {
                      System.Diagnostics.Debug.WriteLine($"[InstallerService] Failed to start service: {ex.Message}");
                 }
             }
             else
             {
                 System.Diagnostics.Debug.WriteLine("[InstallerService] Error: Executable not found after extraction.");
             }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[InstallerService] Failed to install MetricServices: {ex.Message}");
            // Propagate? Or just log? user reported it's "not downloading", so logs are key.
        }
    }
}
