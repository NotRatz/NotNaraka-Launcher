# Auto-Download URL Resolution - Implementation Complete ✅

## Overview
Successfully implemented **automatic download URL resolution** for CN and Global Naraka clients using HTML scraping with intelligent caching.

## What's New

### 🤖 Automatic URL Resolution
The launcher now **automatically fetches the latest download URLs** from official websites instead of hardcoding them or just opening the webpage.

### How It Works

#### 1. **HTML Scraping**
- Fetches the download page HTML from official websites
- Parses the page using HtmlAgilityPack
- Searches for download buttons and links
- Extracts direct download URLs

#### 2. **Smart URL Detection**
Multiple strategies to find the correct download link:
- **Strategy 1**: Find buttons with text like "Download Game Launcher" or "Download Full Game"
- **Strategy 2**: Look for links containing "naraka", "bladepoint", ".zip", or ".exe"
- **Strategy 3**: Search for Chinese text "下载游戏启动器" (Download Game Launcher)
- **Fallback**: If scraping fails, opens the download page in browser

#### 3. **Intelligent Caching**
- URLs are cached for **6 hours** to reduce server requests
- Cached results are reused for faster response
- Cache can be manually cleared if needed
- Status messages show if result is from cache

#### 4. **Version Detection**
- Attempts to extract version information from the page
- Shows version number to user when found
- Helps confirm you're getting the latest release

## User Experience Flow

### CN Client Download
1. User clicks "Download CN Client" button
2. Launcher shows "Fetching latest CN client download link..."
3. **If successful**:
   - Shows dialog: "Found CN client download link! [cached/fresh]"
   - Displays version if detected
   - Shows URL preview
   - Asks: "Would you like to start the download in your browser?"
   - If YES → Opens direct download URL
4. **If scraping fails**:
   - Shows error message with reason
   - Asks: "Would you like to open the CN download page manually?"
   - If YES → Opens https://www.yjwujian.cn/download/#/ as fallback

### Global Client Download
Same flow as CN client but for Global/NA version:
- Scrapes https://www.narakathegame.com/download/
- Finds direct download links
- Shows version information
- Falls back to opening webpage if needed

## Technical Implementation

### New Service: `DownloadUrlResolver`
**Location**: `NarakaTweaks.Core\Services\DownloadUrlResolver.cs`

**Features**:
- `GetGlobalClientUrlAsync()` - Fetches Global/NA client URL
- `GetCnClientUrlAsync()` - Fetches Chinese client URL
- `ClearCache()` - Manually clear URL cache
- Event: `StatusChanged` - Real-time status updates

**Dependencies**:
- `HtmlAgilityPack 1.12.4` - HTML parsing library
- `System.Net.Http` - HTTP client for fetching pages

### Updated Files

1. **NarakaTweaks.Core.csproj**
   - Added HtmlAgilityPack NuGet package

2. **DownloadUrlResolver.cs** (NEW)
   - 328 lines of URL resolution logic
   - Caching mechanism with 6-hour expiration
   - Multi-strategy URL detection
   - Version extraction logic

3. **App.xaml.cs**
   - Added `DownloadUrlResolver` service initialization
   - Added `UrlResolver` property for global access
   - Added `OnUrlResolverStatusChanged` event handler

4. **MainWindow.xaml.cs**
   - Added `using System.Threading.Tasks`
   - Changed CN/Global client buttons to use async handlers
   - Added `DownloadCnClientAsync()` method (58 lines)
   - Added `DownloadGlobalClientAsync()` method (58 lines)
   - Improved error handling with fallback options

## Benefits

### ✅ Always Current
- Automatically gets latest download URLs
- No need to manually update the launcher when game patches release
- Users always download the most recent version

### ✅ Better UX
- Shows version information when available
- Confirms download URL before opening
- Provides direct download links instead of just webpage
- Graceful fallback if scraping fails

### ✅ Reduced Maintenance
- No hardcoded URLs that become outdated
- Self-updating URL system
- Caching reduces server load

### ✅ Reliable Fallback
- If HTML scraping fails (site redesign, network issue, etc.)
- Automatically falls back to opening the download page
- User can still download manually
- No blocking errors - always provides a path forward

## How URL Scraping Works

### Example: Global Client
```
1. Fetch HTML from https://www.narakathegame.com/download/
2. Parse HTML into document tree
3. Find elements with text "Download Game Launcher" or "Download Full Game"
4. Traverse parent nodes to find <a> tag with href attribute
5. Extract href value (direct download URL)
6. Make URL absolute if it's relative
7. Cache result for 6 hours
8. Return URL to user
```

### Robustness Features
- Multiple detection strategies (button text, link patterns, file extensions)
- Handles both English and Chinese text
- Works with relative or absolute URLs
- Graceful error handling at every step
- Detailed status messages for debugging

## Cache System

### Why Caching?
- **Performance**: Instant response for repeated requests
- **Reliability**: Reduces dependency on website availability
- **Courtesy**: Avoids hammering official servers

### Cache Details
- **Duration**: 6 hours (configurable)
- **Storage**: In-memory (cleared on app restart)
- **Keys**: "global-client", "cn-client"
- **Data Stored**: URL, version, timestamp

### When Cache is Used
- Subsequent requests within 6 hours
- Status message shows "(cached)"
- Instant response, no network request

### When Cache is Bypassed
- First request after app launch
- 6+ hours since last fetch
- Manual cache clear (if implemented in UI)

## Error Handling

### Graceful Degradation
Every step has error handling:

1. **Network Error**: Falls back to opening webpage
2. **Parsing Error**: Falls back to opening webpage  
3. **No Link Found**: Falls back to opening webpage
4. **Invalid URL**: Falls back to opening webpage
5. **User Cancels**: No action taken

### User-Friendly Messages
- "Fetching latest download link..." (loading)
- "Found download link! Version: X.X.X" (success)
- "Could not find download link. Error: [reason]" (failure)
- Always offers manual download option

## Testing Recommendations

### Test Scenarios

1. **Fresh Fetch** (no cache):
   - Click "Download CN Client"
   - Should show "Fetching..." message
   - Should find and display URL
   - Should show version if detected

2. **Cached Fetch** (within 6 hours):
   - Click same client button again
   - Should be instant (no "Fetching..." delay)
   - Should show "(cached)" in message

3. **Network Failure**:
   - Disconnect internet
   - Click client button
   - Should show error and offer to open webpage
   - Should not crash

4. **User Cancellation**:
   - Click client button
   - Click "No" when asked to download
   - Should do nothing (no error)

5. **Fallback Flow**:
   - If scraping fails (simulate by modifying URL)
   - Should offer to open download page
   - User can still download manually

## Build Information

- **Executable**: `NotNaraka-Launcher.exe`
- **Size**: 74.45 MB (increased from 74.38 MB due to HtmlAgilityPack)
- **Location**: `c:\Users\Admin\Desktop\NotNaraka Launcher\publish\`
- **Dependencies**: HtmlAgilityPack 1.12.4 (embedded in executable)
- **Status**: ✅ Build successful, ready to test

## Configuration

### Adjustable Settings
You can modify these values in `DownloadUrlResolver.cs`:

```csharp
// Cache expiration (currently 6 hours)
private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(6);

// HTTP timeout (currently 30 seconds)
_httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

// User agent (for website compatibility)
_httpClient.DefaultRequestHeaders.Add("User-Agent", 
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ...");
```

## Limitations & Notes

### Known Limitations

1. **Site Redesigns**: If official sites change their HTML structure significantly, scraping may fail
   - **Mitigation**: Fallback to opening webpage always available

2. **JavaScript-Rendered Content**: If download links are generated by JavaScript
   - **Current**: May not detect JS-generated links
   - **Future**: Could use Selenium for full browser automation if needed

3. **Rate Limiting**: Official sites may rate-limit requests
   - **Mitigation**: 6-hour cache significantly reduces requests

4. **Version Detection**: May not always find version information
   - **Impact**: Minor - download still works, just no version shown

### Future Enhancements

- **API Integration**: If Naraka provides official API endpoints
- **Manual URL Override**: Allow users to specify custom download URLs
- **Cache Management UI**: Settings page option to view/clear cache
- **Download Progress**: Show progress bar for actual file download
- **Automatic Installation**: Extract and install after download completes

## Comparison: Before vs After

### Before (Simple Approach)
```
User clicks "Download CN Client"
  ↓
Opens https://www.yjwujian.cn/download/#/ in browser
  ↓
User manually finds and clicks download button
  ↓
Download starts
```

### After (Auto-Resolution)
```
User clicks "Download CN Client"
  ↓
Launcher fetches page and finds direct download URL
  ↓
Shows: "Found link! Version: 2025-10-08. Download?"
  ↓
User clicks YES
  ↓
Direct download starts immediately (no navigation needed)
```

### Benefits
- ⏱️ **Faster**: Skips webpage navigation
- 🎯 **Direct**: Goes straight to download
- 📊 **Informative**: Shows version before downloading
- 🔄 **Automatic**: Updates with new game versions
- 🛡️ **Safe**: Always falls back if needed

## Status Summary

### ✅ Completed
- HTML scraping implementation
- Caching system with 6-hour expiration
- Multi-strategy URL detection
- Version extraction logic
- Error handling with fallback
- User-friendly dialog messages
- Integration with existing services
- Build and publish successful

### 🎯 Ready to Test
- Click "Download CN Client" button
- Click "Download Global Client" button
- Test with/without internet connection
- Test cache behavior (click twice within 6 hours)
- Verify fallback to webpage works

### 🚀 Future Enhancements
- Add Settings page option to clear cache
- Show last successful URL fetch time
- Add manual URL override option
- Implement actual file download with progress bar
- Add automatic installation after download

---

**The launcher now intelligently fetches the latest download URLs automatically!** 🎉

Test it out: `c:\Users\Admin\Desktop\NotNaraka Launcher\publish\NotNaraka-Launcher.exe`
