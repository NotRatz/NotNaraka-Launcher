using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NarakaTweaks.Core.Services;

public class AutoUpdateService
{
    private const string GITHUB_API_URL = "https://api.github.com/repos/{0}/{1}/releases/latest";
    private const string CURRENT_VERSION = "1.0.0"; // TODO: Read from assembly
    
    private readonly HttpClient _httpClient;
    private string? _githubOwner;
    private string? _githubRepo;
    
    public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
    public event EventHandler<UpdateDownloadProgressEventArgs>? DownloadProgress;
    public event EventHandler<string>? StatusChanged;
    
    public AutoUpdateService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "NarakaTweaks-Launcher");
    }
    
    public void Configure(string githubOwner, string githubRepo)
    {
        _githubOwner = githubOwner;
        _githubRepo = githubRepo;
    }
    
    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_githubOwner) || string.IsNullOrEmpty(_githubRepo))
            {
                OnStatusChanged("Auto-update not configured");
                return new UpdateCheckResult { UpdateAvailable = false };
            }
            
            OnStatusChanged("Checking for updates...");
            
            var url = string.Format(GITHUB_API_URL, _githubOwner, _githubRepo);
            var response = await _httpClient.GetStringAsync(url);
            var release = JsonDocument.Parse(response);
            
            var root = release.RootElement;
            var latestVersion = root.GetProperty("tag_name").GetString()?.TrimStart('v');
            var releaseNotes = root.GetProperty("body").GetString();
            var publishedAt = root.GetProperty("published_at").GetDateTime();
            
            // Find the Windows executable asset
            string? downloadUrl = null;
            long fileSize = 0;
            
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString();
                    if (name?.Contains("win-x64") == true || name?.EndsWith(".exe") == true)
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString();
                        fileSize = asset.GetProperty("size").GetInt64();
                        break;
                    }
                }
            }
            
            if (latestVersion == null)
            {
                OnStatusChanged("Could not determine latest version");
                return new UpdateCheckResult { UpdateAvailable = false };
            }
            
            var isNewer = IsNewerVersion(CURRENT_VERSION, latestVersion);
            
            if (isNewer && downloadUrl != null)
            {
                OnStatusChanged($"Update available: v{latestVersion}");
                OnUpdateAvailable(new UpdateAvailableEventArgs
                {
                    CurrentVersion = CURRENT_VERSION,
                    LatestVersion = latestVersion,
                    ReleaseNotes = releaseNotes ?? "No release notes available",
                    DownloadUrl = downloadUrl,
                    FileSize = fileSize,
                    PublishedAt = publishedAt
                });
                
                return new UpdateCheckResult
                {
                    UpdateAvailable = true,
                    LatestVersion = latestVersion,
                    DownloadUrl = downloadUrl,
                    ReleaseNotes = releaseNotes
                };
            }
            
            OnStatusChanged("You are running the latest version");
            return new UpdateCheckResult { UpdateAvailable = false, LatestVersion = CURRENT_VERSION };
        }
        catch (Exception ex)
        {
            OnStatusChanged($"Update check failed: {ex.Message}");
            return new UpdateCheckResult { UpdateAvailable = false, Error = ex.Message };
        }
    }
    
    public async Task<bool> DownloadAndInstallUpdateAsync(string downloadUrl)
    {
        try
        {
            OnStatusChanged("Downloading update...");
            
            var tempPath = Path.Combine(Path.GetTempPath(), "NarakaTweaks_Update.exe");
            
            using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var bytesRead = 0L;
                
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    int read;
                    
                    while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        bytesRead += read;
                        
                        var progressPercent = totalBytes > 0 ? (int)((bytesRead * 100) / totalBytes) : 0;
                        OnDownloadProgress(new UpdateDownloadProgressEventArgs
                        {
                            BytesDownloaded = bytesRead,
                            TotalBytes = totalBytes,
                            ProgressPercent = progressPercent
                        });
                    }
                }
            }
            
            OnStatusChanged("Update downloaded. Preparing to install...");
            
            // Create update script that replaces the current executable
            var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
            var updateScript = Path.Combine(Path.GetTempPath(), "update_narakatweaks.bat");
            
            var scriptContent = $@"@echo off
timeout /t 2 /nobreak > nul
move /y ""{tempPath}"" ""{currentExe}""
start """" ""{currentExe}""
del ""%~f0""
";
            
            File.WriteAllText(updateScript, scriptContent);
            
            OnStatusChanged("Restarting to apply update...");
            
            // Start the update script and exit
            Process.Start(new ProcessStartInfo
            {
                FileName = updateScript,
                CreateNoWindow = true,
                UseShellExecute = false
            });
            
            return true;
        }
        catch (Exception ex)
        {
            OnStatusChanged($"Update installation failed: {ex.Message}");
            return false;
        }
    }
    
    private bool IsNewerVersion(string currentVersion, string latestVersion)
    {
        try
        {
            var current = Version.Parse(currentVersion);
            var latest = Version.Parse(latestVersion);
            return latest > current;
        }
        catch
        {
            return string.Compare(currentVersion, latestVersion, StringComparison.OrdinalIgnoreCase) < 0;
        }
    }
    
    protected virtual void OnUpdateAvailable(UpdateAvailableEventArgs e)
    {
        UpdateAvailable?.Invoke(this, e);
    }
    
    protected virtual void OnDownloadProgress(UpdateDownloadProgressEventArgs e)
    {
        DownloadProgress?.Invoke(this, e);
    }
    
    protected virtual void OnStatusChanged(string status)
    {
        StatusChanged?.Invoke(this, status);
    }
}

public class UpdateCheckResult
{
    public bool UpdateAvailable { get; set; }
    public string? LatestVersion { get; set; }
    public string? DownloadUrl { get; set; }
    public string? ReleaseNotes { get; set; }
    public string? Error { get; set; }
}

public class UpdateAvailableEventArgs : EventArgs
{
    public string CurrentVersion { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime PublishedAt { get; set; }
}

public class UpdateDownloadProgressEventArgs : EventArgs
{
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public int ProgressPercent { get; set; }
}
