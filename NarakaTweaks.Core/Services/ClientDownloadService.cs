using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace NarakaTweaks.Core.Services;

/// <summary>
/// Manages downloading and installing Naraka game clients.
/// Supports both official global and Chinese client versions.
/// </summary>
public class ClientDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly string _downloadCacheDir;
    
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
    public event EventHandler<string>? StatusChanged;
    
    public ClientDownloadService(string? cacheDirectory = null)
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        _downloadCacheDir = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NarakaTweaks", "Downloads");
        
        Directory.CreateDirectory(_downloadCacheDir);
    }
    
    /// <summary>
    /// Get list of available clients to download
    /// </summary>
    public List<ClientDefinition> GetAvailableClients()
    {
        return new List<ClientDefinition>
        {
            new ClientDefinition
            {
                Id = "global-official",
                Name = "Official Global Client",
                Description = "Official international version (narakathegame.com)",
                DownloadUrl = "https://www.narakathegame.com/download/#/",
                Region = ClientRegion.Global,
                Platform = ClientPlatform.PC,
                IsOfficial = true,
                RequiresExtraction = true,
                AuthenticationServer = "global.narakathegame.com"
            },
            new ClientDefinition
            {
                Id = "china-official",
                Name = "Chinese Official Client",
                Description = "Chinese version (yjwujian.cn)",
                DownloadUrl = "https://www.yjwujian.cn/download/#/",
                Region = ClientRegion.China,
                Platform = ClientPlatform.PC,
                IsOfficial = true,
                RequiresExtraction = true,
                AuthenticationServer = "cn.yjwujian.cn"
            }
        };
    }
    
    /// <summary>
    /// Download a client package with progress tracking
    /// </summary>
    public async Task<DownloadResult> DownloadClientAsync(
        ClientDefinition client,
        string targetDirectory,
        CancellationToken cancellationToken = default)
    {
        var result = new DownloadResult
        {
            ClientId = client.Id,
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            StatusChanged?.Invoke(this, $"Starting download: {client.Name}");
            
            // For actual implementation, you'd need to:
            // 1. Scrape the download page to get the direct ZIP link
            // 2. Or maintain a database of direct download URLs
            
            // Example implementation for demonstration:
            var downloadUrl = await ResolveDownloadUrlAsync(client.DownloadUrl, cancellationToken);
            
            if (string.IsNullOrEmpty(downloadUrl))
            {
                throw new InvalidOperationException("Could not resolve download URL. Please download manually.");
            }
            
            var fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
            var downloadPath = Path.Combine(_downloadCacheDir, fileName);
            
            // Download with progress tracking
            await DownloadFileWithProgressAsync(downloadUrl, downloadPath, cancellationToken);
            
            result.DownloadPath = downloadPath;
            result.FileSize = new FileInfo(downloadPath).Length;
            result.Success = true;
            result.EndTime = DateTime.UtcNow;
            
            StatusChanged?.Invoke(this, $"Download completed: {client.Name}");
            
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            StatusChanged?.Invoke(this, $"Download failed: {ex.Message}");
            return result;
        }
    }
    
    /// <summary>
    /// Extract specific files from downloaded client package
    /// </summary>
    public async Task<ExtractionResult> ExtractClientFilesAsync(
        string zipPath,
        string targetDirectory,
        List<string>? specificFiles = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ExtractionResult { StartTime = DateTime.UtcNow };
        
        try
        {
            StatusChanged?.Invoke(this, "Extracting client files...");
            
            Directory.CreateDirectory(targetDirectory);
            
            await Task.Run(() =>
            {
                using var archive = ZipFile.OpenRead(zipPath);
                var totalEntries = archive.Entries.Count;
                var processedEntries = 0;
                
                foreach (var entry in archive.Entries)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    
                    // Skip if we only want specific files
                    if (specificFiles != null && specificFiles.Count > 0)
                    {
                        if (!specificFiles.Any(f => entry.FullName.Contains(f, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }
                    }
                    
                    var destPath = Path.Combine(targetDirectory, entry.FullName);
                    
                    if (entry.FullName.EndsWith("/"))
                    {
                        // Directory entry
                        Directory.CreateDirectory(destPath);
                    }
                    else
                    {
                        // File entry
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                        entry.ExtractToFile(destPath, overwrite: true);
                        result.ExtractedFiles.Add(destPath);
                    }
                    
                    processedEntries++;
                    var progress = (int)((double)processedEntries / totalEntries * 100);
                    
                    DownloadProgress?.Invoke(this, new DownloadProgressEventArgs
                    {
                        Stage = "Extracting",
                        Progress = progress,
                        BytesReceived = processedEntries,
                        TotalBytes = totalEntries
                    });
                }
            }, cancellationToken);
            
            result.Success = true;
            result.EndTime = DateTime.UtcNow;
            StatusChanged?.Invoke(this, $"Extraction completed: {result.ExtractedFiles.Count} files");
            
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            StatusChanged?.Invoke(this, $"Extraction failed: {ex.Message}");
            return result;
        }
    }
    
    /// <summary>
    /// Extract Naraka game files with smart filtering (excludes Bin and netease folders)
    /// Extracts from archive.zip/Naraka/program/* to the target directory
    /// </summary>
    public async Task<ExtractionResult> ExtractNarakaGameFilesAsync(
        string zipPath,
        string targetDirectory,
        CancellationToken cancellationToken = default)
    {
        var result = new ExtractionResult { StartTime = DateTime.UtcNow };
        
        // Folders to exclude from extraction
        var excludedFolders = new List<string>
        {
            "Bin",
            "netease.mpay.webviewsupport.cef90440"
        };
        
        // Base path in the archive to extract from
        const string requiredBasePath = "Naraka/program/";
        
        try
        {
            StatusChanged?.Invoke(this, "Extracting Naraka game files...");
            
            Directory.CreateDirectory(targetDirectory);
            
            await Task.Run(() =>
            {
                using var archive = ZipFile.OpenRead(zipPath);
                
                // Filter entries that are under Naraka/program/
                var relevantEntries = archive.Entries
                    .Where(e => e.FullName.StartsWith(requiredBasePath, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                var totalEntries = relevantEntries.Count;
                var processedEntries = 0;
                var skippedFolders = 0;
                
                StatusChanged?.Invoke(this, $"Found {totalEntries} files in Naraka/program/ directory");
                
                foreach (var entry in relevantEntries)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    
                    // Get the relative path after "Naraka/program/"
                    var relativePath = entry.FullName.Substring(requiredBasePath.Length);
                    
                    // Check if this entry is in an excluded folder
                    var isExcluded = excludedFolders.Any(excludedFolder =>
                        relativePath.StartsWith(excludedFolder + "/", StringComparison.OrdinalIgnoreCase) ||
                        relativePath.Equals(excludedFolder, StringComparison.OrdinalIgnoreCase));
                    
                    if (isExcluded)
                    {
                        skippedFolders++;
                        processedEntries++;
                        continue;
                    }
                    
                    // Build destination path using relative path only
                    var destPath = Path.Combine(targetDirectory, relativePath);
                    
                    if (entry.FullName.EndsWith("/"))
                    {
                        // Directory entry
                        Directory.CreateDirectory(destPath);
                    }
                    else
                    {
                        // File entry
                        var destDir = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        
                        entry.ExtractToFile(destPath, overwrite: true);
                        result.ExtractedFiles.Add(destPath);
                    }
                    
                    processedEntries++;
                    var progress = (int)((double)processedEntries / totalEntries * 100);
                    
                    DownloadProgress?.Invoke(this, new DownloadProgressEventArgs
                    {
                        Stage = "Extracting",
                        Progress = progress,
                        BytesReceived = processedEntries,
                        TotalBytes = totalEntries,
                        Message = $"Extracted: {relativePath}"
                    });
                }
                
                result.Success = true;
                result.EndTime = DateTime.UtcNow;
                
                var statusMessage = $"Extraction completed: {result.ExtractedFiles.Count} files extracted";
                if (skippedFolders > 0)
                {
                    statusMessage += $", {skippedFolders} files skipped (excluded folders)";
                }
                
                StatusChanged?.Invoke(this, statusMessage);
            }, cancellationToken);
            
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            StatusChanged?.Invoke(this, $"Extraction failed: {ex.Message}");
            return result;
        }
    }
    
    /// <summary>
    /// Validate downloaded file integrity using checksum
    /// </summary>
    public async Task<bool> ValidateFileIntegrityAsync(string filePath, string? expectedHash = null)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;
            
            StatusChanged?.Invoke(this, "Validating file integrity...");
            
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = await sha256.ComputeHashAsync(stream);
            var actualHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            
            if (expectedHash != null)
            {
                return actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
            }
            
            // If no expected hash provided, just verify file is readable
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Clean up old downloaded files from cache
    /// </summary>
    public void CleanupCache(TimeSpan olderThan)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow - olderThan;
            var files = Directory.GetFiles(_downloadCacheDir);
            
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTimeUtc < cutoffDate)
                {
                    File.Delete(file);
                    StatusChanged?.Invoke(this, $"Cleaned up: {Path.GetFileName(file)}");
                }
            }
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Cache cleanup error: {ex.Message}");
        }
    }
    
    private async Task<string?> ResolveDownloadUrlAsync(string pageUrl, CancellationToken cancellationToken)
    {
        // This is a placeholder implementation
        // In production, you would:
        // 1. Fetch the download page HTML
        // 2. Parse it to find the actual ZIP download link
        // 3. Return the direct download URL
        
        // For now, return null to indicate manual download required
        await Task.CompletedTask;
        return null;
    }
    
    private async Task DownloadFileWithProgressAsync(
        string url,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var canReportProgress = totalBytes != -1;
        
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        
        var buffer = new byte[8192];
        long totalBytesRead = 0;
        int bytesRead;
        var lastProgressReport = 0;
        
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalBytesRead += bytesRead;
            
            if (canReportProgress)
            {
                var progress = (int)((double)totalBytesRead / totalBytes * 100);
                
                // Only report every 1% to reduce event spam
                if (progress > lastProgressReport)
                {
                    lastProgressReport = progress;
                    DownloadProgress?.Invoke(this, new DownloadProgressEventArgs
                    {
                        Stage = "Downloading",
                        Progress = progress,
                        BytesReceived = totalBytesRead,
                        TotalBytes = totalBytes
                    });
                }
            }
        }
    }
}

public class ClientDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public ClientRegion Region { get; set; }
    public ClientPlatform Platform { get; set; }
    public bool IsOfficial { get; set; }
    public bool RequiresExtraction { get; set; }
    public string AuthenticationServer { get; set; } = "";
    public long? ExpectedFileSize { get; set; }
    public string? ExpectedChecksum { get; set; }
}

public enum ClientRegion
{
    Global,
    China,
    Japan,
    SEA
}

public enum ClientPlatform
{
    PC,
    Steam,
    EpicGames,
    Console
}

public class DownloadResult
{
    public string ClientId { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DownloadPath { get; set; }
    public long FileSize { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
}

public class ExtractionResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ExtractedFiles { get; set; } = new();
    public TimeSpan Duration => EndTime - StartTime;
}

public class DownloadProgressEventArgs : EventArgs
{
    public string Stage { get; set; } = "";
    public int Progress { get; set; }
    public long BytesReceived { get; set; }
    public long TotalBytes { get; set; }
    public double Speed { get; set; } // bytes per second
    public string Message { get; set; } = "";
}
