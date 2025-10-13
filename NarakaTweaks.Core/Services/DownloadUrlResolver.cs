using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NarakaTweaks.Core.Services;

/// <summary>
/// Resolves and caches download URLs from official Naraka websites
/// </summary>
public class DownloadUrlResolver
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, CachedUrl> _urlCache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(6);
    
    public event EventHandler<string>? StatusChanged;
    
    public DownloadUrlResolver()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        
        _urlCache = new Dictionary<string, CachedUrl>();
    }
    
    /// <summary>
    /// Get the latest download URL for Global/NA client
    /// </summary>
    public async Task<DownloadUrlResult> GetGlobalClientUrlAsync()
    {
        const string cacheKey = "global-client";
        
        // Check cache first
        if (_urlCache.TryGetValue(cacheKey, out var cached) && 
            DateTime.UtcNow - cached.Timestamp < _cacheExpiration)
        {
            StatusChanged?.Invoke(this, "Using cached Global client URL");
            return new DownloadUrlResult
            {
                Success = true,
                DownloadUrl = cached.Url,
                Version = cached.Version,
                FromCache = true
            };
        }
        
        try
        {
            StatusChanged?.Invoke(this, "Fetching Global client download URL...");
            
            // STRATEGY 1: Try direct CDN URL construction (most reliable)
            var directUrl = await TryConstructGlobalDirectUrlAsync();
            if (!string.IsNullOrEmpty(directUrl))
            {
                var version = ExtractVersionFromUrl(directUrl);
                
                // Cache the result
                _urlCache[cacheKey] = new CachedUrl
                {
                    Url = directUrl,
                    Version = version,
                    Timestamp = DateTime.UtcNow
                };
                
                StatusChanged?.Invoke(this, $"Found Global client URL (direct CDN): {directUrl}");
                
                return new DownloadUrlResult
                {
                    Success = true,
                    DownloadUrl = directUrl,
                    Version = version,
                    FromCache = false
                };
            }
            
            // STRATEGY 2: Fallback to web scraping
            var pageUrl = "https://www.narakathegame.com/download/";
            var html = await _httpClient.GetStringAsync(pageUrl);
            
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            
            // Look for download links in the page
            var downloadLinks = doc.DocumentNode.Descendants("a")
                .Where(a => a.GetAttributeValue("href", "").Contains("download") ||
                           a.GetAttributeValue("href", "").Contains(".zip") ||
                           a.GetAttributeValue("href", "").Contains(".exe") ||
                           a.GetAttributeValue("href", "").Contains("easebar.com") ||
                           a.GetAttributeValue("class", "").Contains("download"))
                .ToList();
            
            // Look for specific download button classes/IDs
            var launcherButton = doc.DocumentNode.Descendants()
                .FirstOrDefault(n => 
                    (n.InnerText?.Contains("Download Game Launcher") ?? false) ||
                    (n.InnerText?.Contains("Download Full Game") ?? false));
            
            string? downloadUrl = null;
            string? scrapedVersion = null;
            
            // Try to extract URL from button's parent or nearby elements
            if (launcherButton != null)
            {
                var parent = launcherButton.ParentNode;
                while (parent != null && downloadUrl == null)
                {
                    var link = parent.Descendants("a").FirstOrDefault();
                    if (link != null)
                    {
                        var href = link.GetAttributeValue("href", string.Empty);
                        if (!string.IsNullOrEmpty(href))
                            downloadUrl = href;
                        break;
                    }
                    parent = parent.ParentNode;
                }
            }
            
            // Fallback: get first download link that looks like a game client
            if (downloadUrl == null)
            {
                downloadUrl = downloadLinks
                    .Select(a => a.GetAttributeValue("href", ""))
                    .FirstOrDefault(url => 
                        url.Contains("naraka", StringComparison.OrdinalIgnoreCase) ||
                        url.Contains("bladepoint", StringComparison.OrdinalIgnoreCase) ||
                        url.Contains("Naraka_") ||
                        url.Contains("easebar.com") ||
                        url.EndsWith(".zip") ||
                        url.EndsWith(".exe"));
            }
            
            // Try to extract version information
            var versionNode = doc.DocumentNode.Descendants()
                .FirstOrDefault(n => n.GetAttributeValue("class", "").Contains("version") ||
                                    n.InnerText?.Contains("Version") == true ||
                                    n.InnerText?.Contains("2025-") == true ||
                                    n.InnerText?.Contains("2024-") == true);
            
            if (versionNode != null)
            {
                scrapedVersion = versionNode.InnerText.Trim();
            }
            else if (!string.IsNullOrEmpty(downloadUrl))
            {
                scrapedVersion = ExtractVersionFromUrl(downloadUrl);
            }
            
            if (!string.IsNullOrEmpty(downloadUrl))
            {
                // Make URL absolute if it's relative
                if (!downloadUrl.StartsWith("http"))
                {
                    downloadUrl = new Uri(new Uri(pageUrl), downloadUrl).ToString();
                }
                
                // Cache the result
                _urlCache[cacheKey] = new CachedUrl
                {
                    Url = downloadUrl,
                    Version = scrapedVersion,
                    Timestamp = DateTime.UtcNow
                };
                
                StatusChanged?.Invoke(this, $"Found Global client URL: {downloadUrl}");
                
                return new DownloadUrlResult
                {
                    Success = true,
                    DownloadUrl = downloadUrl,
                    Version = scrapedVersion,
                    FromCache = false
                };
            }
            
            StatusChanged?.Invoke(this, "Could not find Global client download URL");
            return new DownloadUrlResult
            {
                Success = false,
                ErrorMessage = "Could not locate download link on the page"
            };
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Error fetching Global client URL: {ex.Message}");
            return new DownloadUrlResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    /// <summary>
    /// Get the latest download URL for Chinese (CN) client
    /// Uses direct CDN URL pattern: https://d90.gdl.netease.com/publish/green/yjwj_YYYY-MM-DD-HH-MM.zip
    /// </summary>
    public async Task<DownloadUrlResult> GetCnClientUrlAsync()
    {
        const string cacheKey = "cn-client";
        
        // Check cache first
        if (_urlCache.TryGetValue(cacheKey, out var cached) && 
            DateTime.UtcNow - cached.Timestamp < _cacheExpiration)
        {
            StatusChanged?.Invoke(this, "Using cached CN client URL");
            return new DownloadUrlResult
            {
                Success = true,
                DownloadUrl = cached.Url,
                Version = cached.Version,
                FromCache = true
            };
        }
        
        try
        {
            StatusChanged?.Invoke(this, "Fetching CN client download URL...");
            
            // STRATEGY 1: Try direct CDN URL construction (most reliable)
            var directUrl = await TryConstructCnDirectUrlAsync();
            if (!string.IsNullOrEmpty(directUrl))
            {
                var version = ExtractVersionFromUrl(directUrl);
                
                // Cache the result
                _urlCache[cacheKey] = new CachedUrl
                {
                    Url = directUrl,
                    Version = version,
                    Timestamp = DateTime.UtcNow
                };
                
                StatusChanged?.Invoke(this, $"Found CN client URL (direct CDN): {directUrl}");
                
                return new DownloadUrlResult
                {
                    Success = true,
                    DownloadUrl = directUrl,
                    Version = version,
                    FromCache = false
                };
            }
            
            // STRATEGY 2: Fallback to web scraping
            var pageUrl = "https://www.yjwujian.cn/download/";
            var html = await _httpClient.GetStringAsync(pageUrl);
            
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            
            // Look for download links
            var downloadLinks = doc.DocumentNode.Descendants("a")
                .Where(a => a.GetAttributeValue("href", "").Contains("download") ||
                           a.GetAttributeValue("href", "").Contains(".zip") ||
                           a.GetAttributeValue("href", "").Contains(".exe") ||
                           a.GetAttributeValue("href", "").Contains("gdl.netease.com") ||
                           a.GetAttributeValue("class", "").Contains("download"))
                .ToList();
            
            // Look for Chinese download buttons
            var launcherButton = doc.DocumentNode.Descendants()
                .FirstOrDefault(n => 
                    (n.InnerText?.Contains("下载游戏启动器") ?? false) || // Download Game Launcher
                    (n.InnerText?.Contains("下载完整游戏") ?? false) ||   // Download Full Game
                    (n.InnerText?.Contains("Download Game Launcher") ?? false) ||
                    (n.InnerText?.Contains("Download Full Game") ?? false));
            
            string? downloadUrl = null;
            string? scrapedVersion = null;
            
            // Try to extract URL from button's parent or nearby elements
            if (launcherButton != null)
            {
                var parent = launcherButton.ParentNode;
                while (parent != null && downloadUrl == null)
                {
                    var link = parent.Descendants("a").FirstOrDefault();
                    if (link != null)
                    {
                        var href = link.GetAttributeValue("href", string.Empty);
                        if (!string.IsNullOrEmpty(href))
                            downloadUrl = href;
                        break;
                    }
                    parent = parent.ParentNode;
                }
            }
            
            // Fallback: get first download link that looks like a game client
            if (downloadUrl == null)
            {
                downloadUrl = downloadLinks
                    .Select(a => a.GetAttributeValue("href", ""))
                    .FirstOrDefault(url => 
                        url.Contains("naraka", StringComparison.OrdinalIgnoreCase) ||
                        url.Contains("bladepoint", StringComparison.OrdinalIgnoreCase) ||
                        url.Contains("永劫无间") || // Naraka in Chinese
                        url.Contains("yjwj_") || // CN client pattern
                        url.Contains("gdl.netease.com") ||
                        url.EndsWith(".zip") ||
                        url.EndsWith(".exe"));
            }
            
            // Try to extract version information
            var versionNode = doc.DocumentNode.Descendants()
                .FirstOrDefault(n => n.GetAttributeValue("class", "").Contains("version") ||
                                    n.InnerText?.Contains("版本") == true || // Version in Chinese
                                    n.InnerText?.Contains("2025-") == true ||
                                    n.InnerText?.Contains("2024-") == true);
            
            if (versionNode != null)
            {
                scrapedVersion = versionNode.InnerText.Trim();
            }
            else if (!string.IsNullOrEmpty(downloadUrl))
            {
                scrapedVersion = ExtractVersionFromUrl(downloadUrl);
            }
            
            if (!string.IsNullOrEmpty(downloadUrl))
            {
                // Make URL absolute if it's relative
                if (!downloadUrl.StartsWith("http"))
                {
                    downloadUrl = new Uri(new Uri(pageUrl), downloadUrl).ToString();
                }
                
                // Cache the result
                _urlCache[cacheKey] = new CachedUrl
                {
                    Url = downloadUrl,
                    Version = scrapedVersion,
                    Timestamp = DateTime.UtcNow
                };
                
                StatusChanged?.Invoke(this, $"Found CN client URL: {downloadUrl}");
                
                return new DownloadUrlResult
                {
                    Success = true,
                    DownloadUrl = downloadUrl,
                    Version = scrapedVersion,
                    FromCache = false
                };
            }
            
            StatusChanged?.Invoke(this, "Could not find CN client download URL");
            return new DownloadUrlResult
            {
                Success = false,
                ErrorMessage = "Could not locate download link on the page"
            };
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Error fetching CN client URL: {ex.Message}");
            return new DownloadUrlResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    /// <summary>
    /// Clear the URL cache
    /// </summary>
    public void ClearCache()
    {
        _urlCache.Clear();
        StatusChanged?.Invoke(this, "URL cache cleared");
    }
    
    /// <summary>
    /// Try to construct direct CDN URL for Global/NA client
    /// Pattern: https://d90na.gdl.easebar.com/global/green/Naraka_YYYY-MM-DD-HH-MM.zip
    /// Note: Patches typically release on Wednesdays, so we prioritize those days
    /// </summary>
    private async Task<string?> TryConstructGlobalDirectUrlAsync()
    {
        const string cdnBase = "https://d90na.gdl.easebar.com/global/green/";
        
        var now = DateTime.UtcNow;
        var urlsToCheck = new List<string>();
        
        // SMART PATCH DETECTION: Patches typically release on Wednesdays
        // Check last 5 weekdays, prioritizing Wednesdays
        var datesToCheck = GetRecentWeekdays(now, 5, prioritizeWednesday: true);
        
        foreach (var date in datesToCheck)
        {
            // Try a few common release times throughout the day
            // Using wildcard pattern: check multiple times in parallel
            foreach (var hour in new[] { 18, 16, 20, 14, 12 }) // Peak hours first
            {
                var dateStr = $"{date.Year:D4}-{date.Month:D2}-{date.Day:D2}-{hour:D2}-00";
                var url = $"{cdnBase}Naraka_{dateStr}.zip";
                urlsToCheck.Add(url);
            }
        }
        
        StatusChanged?.Invoke(this, $"Checking {urlsToCheck.Count} potential Global client URLs (prioritizing Wednesdays)...");
        
        // PARALLEL CHECK: Check all URLs concurrently for much faster resolution
        var tasks = urlsToCheck.Select(async url =>
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                request.Headers.ConnectionClose = true;
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(3));
                var response = await _httpClient.SendAsync(request, cts.Token);
                
                if (response.IsSuccessStatusCode)
                {
                    return url;
                }
            }
            catch
            {
                // Ignore failures
            }
            return null;
        }).ToList();
        
        // Wait for first success or all to complete
        while (tasks.Any())
        {
            var completed = await Task.WhenAny(tasks);
            var result = await completed;
            
            if (result != null)
            {
                StatusChanged?.Invoke(this, $"Found Global client at: {result}");
                return result;
            }
            
            tasks.Remove(completed);
        }
        
        StatusChanged?.Invoke(this, "Could not find Global client via direct CDN URL");
        return null;
    }
    
    /// <summary>
    /// Try to construct direct CDN URL for CN client
    /// Pattern: https://d90.gdl.netease.com/publish/green/yjwj_YYYY-MM-DD-HH-MM.zip
    /// Note: Patches typically release on Wednesdays, so we prioritize those days
    /// </summary>
    private async Task<string?> TryConstructCnDirectUrlAsync()
    {
        const string cdnBase = "https://d90.gdl.netease.com/publish/green/";
        
        var now = DateTime.UtcNow.AddHours(8); // Convert to China time (UTC+8)
        var urlsToCheck = new List<string>();
        
        // SMART PATCH DETECTION: Patches typically release on Wednesdays
        // Check last 5 weekdays, prioritizing Wednesdays
        var datesToCheck = GetRecentWeekdays(now, 5, prioritizeWednesday: true);
        
        foreach (var date in datesToCheck)
        {
            // Try a few common release times throughout the day
            foreach (var hour in new[] { 20, 18, 16, 14, 12 }) // Peak hours first
            {
                var dateStr = $"{date.Year:D4}-{date.Month:D2}-{date.Day:D2}-{hour:D2}-00";
                var url = $"{cdnBase}yjwj_{dateStr}.zip";
                urlsToCheck.Add(url);
            }
        }
        
        StatusChanged?.Invoke(this, $"Checking {urlsToCheck.Count} potential CN client URLs (prioritizing Wednesdays)...");
        
        // PARALLEL CHECK: Check all URLs concurrently for much faster resolution
        var tasks = urlsToCheck.Select(async url =>
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                request.Headers.ConnectionClose = true;
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(3));
                var response = await _httpClient.SendAsync(request, cts.Token);
                
                if (response.IsSuccessStatusCode)
                {
                    return url;
                }
            }
            catch
            {
                // Ignore failures
            }
            return null;
        }).ToList();
        
        // Wait for first success or all to complete
        while (tasks.Any())
        {
            var completed = await Task.WhenAny(tasks);
            var result = await completed;
            
            if (result != null)
            {
                StatusChanged?.Invoke(this, $"Found CN client at: {result}");
                return result;
            }
            
            tasks.Remove(completed);
        }
        
        StatusChanged?.Invoke(this, "Could not find CN client via direct CDN URL");
        return null;
    }
    
    /// <summary>
    /// Extract version from URL (date-based version for both CN and Global clients)
    /// </summary>
    private string? ExtractVersionFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;
        
        // Pattern 1: CN client - yjwj_2025-10-08-20-00.zip
        var match = System.Text.RegularExpressions.Regex.Match(
            url, 
            @"yjwj_(\d{4}-\d{2}-\d{2}-\d{2}-\d{2})\.zip");
        
        if (match.Success)
        {
            return match.Groups[1].Value; // Returns "2025-10-08-20-00"
        }
        
        // Pattern 2: Global client - Naraka_2025-10-08-20-00.zip
        match = System.Text.RegularExpressions.Regex.Match(
            url, 
            @"Naraka_(\d{4}-\d{2}-\d{2}-\d{2}-\d{2})\.zip");
        
        if (match.Success)
        {
            return match.Groups[1].Value; // Returns "2025-10-08-20-00"
        }
        
        // Pattern 3: Any date-time pattern
        match = System.Text.RegularExpressions.Regex.Match(
            url, 
            @"(\d{4}-\d{2}-\d{2}-\d{2}-\d{2})");
        
        if (match.Success)
        {
            return match.Groups[1].Value; // Returns "2025-10-08-20-00"
        }
        
        // Pattern 4: Just date (fallback)
        match = System.Text.RegularExpressions.Regex.Match(
            url, 
            @"(\d{4}-\d{2}-\d{2})");
        
        if (match.Success)
        {
            return match.Groups[1].Value; // Returns "2025-10-08"
        }
        
        return null;
    }
    
    /// <summary>
    /// Get recent weekdays (Monday-Friday), prioritizing Wednesdays since patches typically release then
    /// </summary>
    private List<DateTime> GetRecentWeekdays(DateTime startDate, int count, bool prioritizeWednesday = true)
    {
        var dates = new List<DateTime>();
        var currentDate = startDate.Date;
        
        // First, check if today is Wednesday or if there's a recent Wednesday
        if (prioritizeWednesday)
        {
            // Find the most recent Wednesday
            var daysUntilWednesday = ((int)currentDate.DayOfWeek - (int)DayOfWeek.Wednesday + 7) % 7;
            var lastWednesday = currentDate.AddDays(-daysUntilWednesday);
            
            // If last Wednesday was within the last 7 days, add it first
            if ((currentDate - lastWednesday).TotalDays <= 7)
            {
                dates.Add(lastWednesday);
            }
        }
        
        // Now add other recent weekdays (Monday-Friday only)
        int daysBack = 0;
        while (dates.Count < count && daysBack < 14) // Look back up to 2 weeks
        {
            var checkDate = currentDate.AddDays(-daysBack);
            
            // Skip weekends (Saturday = 6, Sunday = 0)
            if (checkDate.DayOfWeek != DayOfWeek.Saturday && 
                checkDate.DayOfWeek != DayOfWeek.Sunday)
            {
                // Don't add duplicates (Wednesday might already be in the list)
                if (!dates.Any(d => d.Date == checkDate.Date))
                {
                    dates.Add(checkDate);
                }
            }
            
            daysBack++;
        }
        
        return dates.Take(count).ToList();
    }
    
    private class CachedUrl
    {
        public string Url { get; set; } = "";
        public string? Version { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

public class DownloadUrlResult
{
    public bool Success { get; set; }
    public string? DownloadUrl { get; set; }
    public string? Version { get; set; }
    public string? ErrorMessage { get; set; }
    public bool FromCache { get; set; }
}
