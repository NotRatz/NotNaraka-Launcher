# 🌍 Global/NA Client Download URL Pattern

## Direct CDN URL Structure

The Global/NA client is hosted on Easebar's CDN with a predictable URL pattern:

**Pattern:**
```
https://d90na.gdl.easebar.com/global/green/Naraka_YYYY-MM-DD-HH-MM.zip
```

**Example:**
```
https://d90na.gdl.easebar.com/global/green/Naraka_2025-10-08-20-00.zip
```

---

## URL Components

| Component | Description | Example |
|-----------|-------------|---------|
| `https://d90na.gdl.easebar.com` | Easebar CDN base | Fixed |
| `/global/green/` | Release channel | Fixed |
| `Naraka_` | Game name prefix | Fixed |
| `YYYY-MM-DD` | Release date | `2025-10-08` |
| `HH-MM` | Release time (24h, UTC) | `20-00` (8:00 PM UTC) |
| `.zip` | File extension | Fixed |

---

## Comparison: CN vs Global Clients

### URL Patterns

| Region | Base URL | Prefix | Example |
|--------|----------|--------|---------|
| **CN** | `d90.gdl.netease.com/publish/green/` | `yjwj_` | `yjwj_2025-10-08-20-00.zip` |
| **Global** | `d90na.gdl.easebar.com/global/green/` | `Naraka_` | `Naraka_2025-10-08-20-00.zip` |

### Key Differences

| Aspect | CN Client | Global Client |
|--------|-----------|---------------|
| CDN Provider | Netease (`gdl.netease.com`) | Easebar (`gdl.easebar.com`) |
| Subdomain | `d90` | `d90na` |
| Path | `/publish/green/` | `/global/green/` |
| Filename Prefix | `yjwj_` (永劫无间) | `Naraka_` |
| Time Zone | UTC+8 (China Time) | UTC |
| Website | yjwujian.cn | narakathegame.com |

---

## Common Release Times

Both clients typically release at the same hours:
- **20:00** (8:00 PM) - Most common
- **18:00** (6:00 PM)
- **16:00** (4:00 PM)
- **14:00** (2:00 PM)
- **12:00** (12:00 PM)
- **10:00** (10:00 AM)

**Note:** 
- CN client times are in China Standard Time (UTC+8)
- Global client times are in UTC

---

## Implementation Strategy

### Primary Method (Direct CDN)
1. Calculate current date (UTC for Global, UTC+8 for CN)
2. Try recent dates (today and past 7 days)
3. For each date, try common release times
4. Send HTTP HEAD request to check if file exists
5. Return first valid URL found

**Advantages:**
- ✅ Fast (direct URL, no web scraping)
- ✅ Reliable (consistent pattern)
- ✅ No HTML parsing required
- ✅ Works even if website is down

### Fallback Method (Web Scraping)
If direct CDN fails, fall back to scraping:
- **CN:** `https://www.yjwujian.cn/download/`
- **Global:** `https://www.narakathegame.com/download/`

---

## Code Implementation

### Global Client Resolver

```csharp
private async Task<string?> TryConstructGlobalDirectUrlAsync()
{
    const string cdnBase = "https://d90na.gdl.easebar.com/global/green/";
    var now = DateTime.UtcNow; // UTC for Global
    var commonReleaseTimes = new[] { 20, 18, 16, 14, 12, 10 };
    
    for (int daysBack = 0; daysBack < 7; daysBack++)
    {
        var date = now.AddDays(-daysBack);
        
        foreach (var hour in commonReleaseTimes)
        {
            var dateStr = $"{date:yyyy-MM-dd}-{hour:D2}-00";
            var url = $"{cdnBase}Naraka_{dateStr}.zip";
            
            if (await UrlExistsAsync(url))
                return url;
        }
    }
    
    return null;
}
```

### CN Client Resolver

```csharp
private async Task<string?> TryConstructCnDirectUrlAsync()
{
    const string cdnBase = "https://d90.gdl.netease.com/publish/green/";
    var now = DateTime.UtcNow.AddHours(8); // China time
    var commonReleaseTimes = new[] { 20, 18, 16, 14, 12, 10 };
    
    for (int daysBack = 0; daysBack < 7; daysBack++)
    {
        var date = now.AddDays(-daysBack);
        
        foreach (var hour in commonReleaseTimes)
        {
            var dateStr = $"{date:yyyy-MM-dd}-{hour:D2}-00";
            var url = $"{cdnBase}yjwj_{dateStr}.zip";
            
            if (await UrlExistsAsync(url))
                return url;
        }
    }
    
    return null;
}
```

### Version Extraction

Supports both patterns:

```csharp
private string? ExtractVersionFromUrl(string url)
{
    // CN: yjwj_2025-10-08-20-00.zip
    var cnMatch = Regex.Match(url, @"yjwj_(\d{4}-\d{2}-\d{2}-\d{2}-\d{2})\.zip");
    if (cnMatch.Success)
        return cnMatch.Groups[1].Value;
    
    // Global: Naraka_2025-10-08-20-00.zip
    var globalMatch = Regex.Match(url, @"Naraka_(\d{4}-\d{2}-\d{2}-\d{2}-\d{2})\.zip");
    if (globalMatch.Success)
        return globalMatch.Groups[1].Value;
    
    // Fallback: any date pattern
    var dateMatch = Regex.Match(url, @"(\d{4}-\d{2}-\d{2})");
    if (dateMatch.Success)
        return dateMatch.Groups[1].Value;
    
    return null;
}
```

---

## File Sizes

| Client | Typical Size |
|--------|--------------|
| CN Client | 2-4 GB |
| Global Client | 2-4 GB |

---

## Archive Structure (Both Clients)

Inside the ZIP file:

```
archive.zip
└── Naraka/
    └── program/
        ├── NarakaBladepoint.exe
        ├── NarakaBladepoint_Data/
        ├── Config/
        ├── Naraka/
        ├── Bin/                              ← EXCLUDE (not needed)
        └── netease.mpay.webviewsupport.cef90440/  ← EXCLUDE (payment SDK)
```

**Extraction Strategy:**
- Extract from: `Naraka/program/*`
- Exclude: `Bin` and `netease.mpay.webviewsupport.cef90440`
- Remove: `Naraka/program/` prefix (flatten structure)

---

## Testing

### Check if URL Exists (PowerShell)

```powershell
# Global Client
$url = "https://d90na.gdl.easebar.com/global/green/Naraka_2025-10-08-20-00.zip"
Invoke-WebRequest -Uri $url -Method Head

# CN Client
$url = "https://d90.gdl.netease.com/publish/green/yjwj_2025-10-08-20-00.zip"
Invoke-WebRequest -Uri $url -Method Head
```

### Download File

```powershell
# Global Client
$url = "https://d90na.gdl.easebar.com/global/green/Naraka_2025-10-08-20-00.zip"
$output = "naraka-global-client.zip"
Invoke-WebRequest -Uri $url -OutFile $output

# CN Client
$url = "https://d90.gdl.netease.com/publish/green/yjwj_2025-10-08-20-00.zip"
$output = "naraka-cn-client.zip"
Invoke-WebRequest -Uri $url -OutFile $output
```

---

## Usage in Launcher

### Automatic Detection

```csharp
var resolver = new DownloadUrlResolver();

// Global Client
var globalResult = await resolver.GetGlobalClientUrlAsync();
if (globalResult.Success)
{
    Console.WriteLine($"Global URL: {globalResult.DownloadUrl}");
    Console.WriteLine($"Version: {globalResult.Version}");
}

// CN Client
var cnResult = await resolver.GetCnClientUrlAsync();
if (cnResult.Success)
{
    Console.WriteLine($"CN URL: {cnResult.DownloadUrl}");
    Console.WriteLine($"Version: {cnResult.Version}");
}
```

### Manual URL Input

Users can manually input URLs if automatic detection fails:

**Global:**
```
https://d90na.gdl.easebar.com/global/green/Naraka_2025-10-08-20-00.zip
```

**CN:**
```
https://d90.gdl.netease.com/publish/green/yjwj_2025-10-08-20-00.zip
```

---

## Update Frequency

- **Regular Updates:** Weekly (usually)
- **Hotfixes:** As needed
- **Major Patches:** Monthly
- **Both clients:** Typically updated simultaneously

---

## Troubleshooting

### URL Not Found (404)

**Symptoms:** HTTP 404 when trying direct URL

**Solutions:**
1. Try more recent dates (may be newer version)
2. Check official website:
   - Global: https://www.narakathegame.com/download/
   - CN: https://www.yjwujian.cn/download/
3. Verify date format: `YYYY-MM-DD-HH-MM`
4. Try different release times (20, 18, 16, 14, 12, 10)

### Slow Download Speed

**Symptoms:** Download speed < 1 MB/s

**Solutions:**
1. **Global Client:**
   - Try different network/ISP
   - Use VPN closer to server location
   - Download during off-peak hours

2. **CN Client:**
   - CDN may be optimized for China
   - Consider VPN with China location
   - Use download manager with resume capability

### CDN Differences

| Issue | CN Client | Global Client |
|-------|-----------|---------------|
| Access from China | ✅ Fast | ⚠️ May be slow |
| Access from NA/EU | ⚠️ May be slow | ✅ Fast |
| Access from SEA | ⚠️ Variable | ✅ Good |

---

## Quick Reference

### URL Construction

```bash
# Global Client Pattern
https://d90na.gdl.easebar.com/global/green/Naraka_YYYY-MM-DD-HH-MM.zip

# CN Client Pattern
https://d90.gdl.netease.com/publish/green/yjwj_YYYY-MM-DD-HH-MM.zip

# Replace YYYY-MM-DD-HH-MM with actual date/time
# Example: 2025-10-08-20-00
```

### Common Versions (Examples)

```
Global: Naraka_2025-10-08-20-00.zip (Oct 8, 2025, 8:00 PM UTC)
CN:     yjwj_2025-10-08-20-00.zip   (Oct 8, 2025, 8:00 PM UTC+8)
```

---

## Related Files

- **URL Resolver:** `NarakaTweaks.Core\Services\DownloadUrlResolver.cs`
- **Download Service:** `NarakaTweaks.Core\Services\ClientDownloadService.cs`
- **Extraction Logic:** `ClientDownloadService.ExtractNarakaGameFilesAsync()`
- **CN Pattern Doc:** `CN_CLIENT_URL_PATTERN.md`

---

## External Resources

### Official Websites
- **Global:** https://www.narakathegame.com/
- **Global Download:** https://www.narakathegame.com/download/
- **CN:** https://www.yjwujian.cn/
- **CN Download:** https://www.yjwujian.cn/download/

### CDN Base URLs
- **Global CDN:** https://d90na.gdl.easebar.com/
- **CN CDN:** https://d90.gdl.netease.com/

---

**Last Updated:** October 12, 2025  
**Patterns Verified:** ✅ Both CN and Global  
**Implementation Status:** ✅ Complete
