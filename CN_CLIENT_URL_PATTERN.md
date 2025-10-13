# CN Client Download URL Pattern

## Direct CDN URL Structure

The Chinese (CN) client is hosted on Netease's CDN with a predictable URL pattern:

**Pattern:**
```
https://d90.gdl.netease.com/publish/green/yjwj_YYYY-MM-DD-HH-MM.zip
```

**Example:**
```
https://d90.gdl.netease.com/publish/green/yjwj_2025-10-08-20-00.zip
```

## URL Components

| Component | Description | Example |
|-----------|-------------|---------|
| `https://d90.gdl.netease.com` | Netease CDN base | Fixed |
| `/publish/green/` | Release channel | Fixed |
| `yjwj_` | Game prefix (永劫无间) | Fixed |
| `YYYY-MM-DD` | Release date | `2025-10-08` |
| `HH-MM` | Release time (24h, China Time UTC+8) | `20-00` (8:00 PM) |
| `.zip` | File extension | Fixed |

## Common Release Times

Chinese client updates typically release at:
- **20:00** (8:00 PM) - Most common
- **18:00** (6:00 PM)
- **16:00** (4:00 PM)
- **14:00** (2:00 PM)
- **12:00** (12:00 PM)
- **10:00** (10:00 AM)

All times are in China Standard Time (UTC+8).

## Implementation Strategy

### Primary Method (Direct CDN)
1. Calculate current date in China time (UTC+8)
2. Try recent dates (today and past 7 days)
3. For each date, try common release times
4. Send HTTP HEAD request to check if file exists
5. Return first valid URL found

**Advantages:**
- Fast (direct URL, no web scraping)
- Reliable (consistent pattern)
- No parsing required

### Fallback Method (Web Scraping)
If direct CDN fails, fall back to scraping:
- URL: `https://www.yjwujian.cn/download/`
- Look for download buttons with Chinese text
- Parse HTML to find download links

## Code Implementation

### Resolver Logic

Located in: `NarakaTweaks.Core\Services\DownloadUrlResolver.cs`

```csharp
public async Task<DownloadUrlResult> GetCnClientUrlAsync()
{
    // Strategy 1: Try direct CDN URL (FAST)
    var directUrl = await TryConstructCnDirectUrlAsync();
    if (!string.IsNullOrEmpty(directUrl))
        return success;
    
    // Strategy 2: Fallback to web scraping
    return await ScrapeCnDownloadPageAsync();
}
```

### Direct URL Construction

```csharp
private async Task<string?> TryConstructCnDirectUrlAsync()
{
    const string cdnBase = "https://d90.gdl.netease.com/publish/green/";
    var now = DateTime.UtcNow.AddHours(8); // China time
    
    for (int daysBack = 0; daysBack < 7; daysBack++)
    {
        var date = now.AddDays(-daysBack);
        
        foreach (var hour in commonReleaseTimes)
        {
            var url = $"{cdnBase}yjwj_{date:yyyy-MM-dd}-{hour:D2}-00.zip";
            
            // Check if exists with HEAD request
            if (await UrlExistsAsync(url))
                return url;
        }
    }
    
    return null;
}
```

## Version Extraction

Version is extracted from the URL date:

**Full Version:**
```
yjwj_2025-10-08-20-00.zip → "2025-10-08-20-00"
```

**Short Version:**
```
yjwj_2025-10-08-20-00.zip → "2025-10-08"
```

## File Size

Typical CN client ZIP file size: **2-4 GB**

## Archive Structure

Inside the ZIP file:

```
archive.zip
└── Naraka/
    └── program/
        ├── NarakaBladepoint.exe
        ├── NarakaBladepoint_Data/
        ├── Config/
        ├── Naraka/
        ├── Bin/                              ← EXCLUDE
        └── netease.mpay.webviewsupport.cef90440/  ← EXCLUDE
```

## Usage in Launcher

### Automatic Detection
```csharp
var resolver = new DownloadUrlResolver();
var result = await resolver.GetCnClientUrlAsync();

if (result.Success)
{
    Console.WriteLine($"URL: {result.DownloadUrl}");
    Console.WriteLine($"Version: {result.Version}");
}
```

### Manual URL Input
If automatic detection fails, users can manually input the URL:
```
https://d90.gdl.netease.com/publish/green/yjwj_2025-10-08-20-00.zip
```

## Testing

### Check if URL Exists
```powershell
# PowerShell
$url = "https://d90.gdl.netease.com/publish/green/yjwj_2025-10-08-20-00.zip"
Invoke-WebRequest -Uri $url -Method Head
```

### Download File
```powershell
# PowerShell
$url = "https://d90.gdl.netease.com/publish/green/yjwj_2025-10-08-20-00.zip"
$output = "naraka-cn-client.zip"
Invoke-WebRequest -Uri $url -OutFile $output
```

## Update Frequency

- CN client updates: **Weekly** (usually)
- Hotfixes: **As needed**
- Major patches: **Monthly**

## Troubleshooting

### URL Not Found
**Symptoms:** HTTP 404 when trying direct URL

**Solutions:**
1. Check if newer version exists (try more recent dates)
2. Check official website: https://www.yjwujian.cn/download/
3. Verify date format (must be `YYYY-MM-DD-HH-MM`)
4. Ensure time is in China timezone (UTC+8)

### Slow Download
**Symptoms:** Download speed < 1 MB/s

**Solutions:**
1. CDN may be throttled outside China
2. Try different network/VPN
3. Use download manager with resume capability

### Invalid Archive
**Symptoms:** ZIP extraction fails

**Solutions:**
1. Verify file size matches expected size
2. Re-download (may be corrupted)
3. Check disk space

## Related Files

- **URL Resolver:** `NarakaTweaks.Core\Services\DownloadUrlResolver.cs`
- **Download Service:** `NarakaTweaks.Core\Services\ClientDownloadService.cs`
- **Extraction Logic:** `ClientDownloadService.ExtractNarakaGameFilesAsync()`

## External Resources

- **Official CN Website:** https://www.yjwujian.cn/
- **Download Page:** https://www.yjwujian.cn/download/
- **CDN Base:** https://d90.gdl.netease.com/

---

**Last Updated:** October 12, 2025  
**Pattern Verified:** ✅ Yes  
**Implementation Status:** ✅ Complete
