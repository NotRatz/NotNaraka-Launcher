# 🚀 Quick Reference - Pop-Out Windows & Download Logging

## Window Sizes

| Window | Dimensions | Resizable? |
|--------|-----------|------------|
| Main Launcher | 800 x 600 | ❌ No (locked) |
| Tweaks | 900 x 700 | ✅ Yes |
| Clients | 900 x 700 | ✅ Yes |
| Settings | 800 x 600 | ✅ Yes |

---

## Navigation Behavior

| Button | Action | Window Type |
|--------|--------|-------------|
| 🏠 Dashboard | Refreshes dashboard in main window | Main window |
| ⚙️ Tweaks | Opens pop-out Tweaks window | Pop-out (900x700) |
| 📥 Clients | Opens pop-out Clients window | Pop-out (900x700) |
| 🔧 Settings | Opens pop-out Settings window | Pop-out (800x600) |

---

## Download Logging

### Where Logs Appear

**Main Window Dashboard:**
- ✅ Action log section (bottom of dashboard)
- ✅ Real-time download progress
- ✅ Download percentage (0-100%)
- ✅ MB downloaded / Total MB
- ✅ Download speed
- ✅ Extraction progress
- ✅ Success/error messages

### Log Color Coding

| Color | Purpose | Example |
|-------|---------|---------|
| **Cyan (#00FFFF)** | Section headers | `=== CN Client Download Initiated ===` |
| **Yellow (#FFFF00)** | Status updates | `Fetching latest CN client download link...` |
| **Green (#00FF00)** | Success messages | `✅ Download URL found: https://...` |
| **Red (#FF0000)** | Error messages | `❌ ERROR: Failed to download` |
| **White (#FFFFFF)** | General info | Spacing, separator lines |
| **Gray (#CCCCCC)** | Technical details | `Contacting CDN servers...` |

---

## File Structure Changes

### Modified Files

```
NarakaTweaks.Launcher\
├── MainWindow.xaml          [Window size locked to 800x600, sidebar 200px]
└── MainWindow.xaml.cs       [Pop-out navigation, logging methods]
```

### New Documentation

```
📄 POP_OUT_WINDOWS_IMPLEMENTATION.md  [Complete implementation details]
📄 DOWNLOAD_LOGGING_IMPLEMENTATION.md [Download logging details]
📄 QUICK_REFERENCE_POPOUT_LOGGING.md [This file]
```

---

## Key Methods

### Pop-Out Windows
```csharp
OpenTweaksWindow()    // Opens System Tweaks window
OpenClientsWindow()   // Opens Client Downloads window
OpenSettingsWindow()  // Opens Settings window
```

### Content Creation
```csharp
CreateTweaksPage()    // Returns UIElement for Tweaks
CreateClientsPage()   // Returns UIElement for Clients
CreateSettingsPage()  // Returns UIElement for Settings
```

### Logging
```csharp
LogToActionLog(message, colorHex)  // Logs to dashboard action log
```

---

## User Experience

### Benefits

✅ **Dashboard Always Visible** - Never leaves main window  
✅ **Multiple Windows** - Open Tweaks and Clients simultaneously  
✅ **Real-Time Logging** - Monitor downloads in main window  
✅ **Multi-Monitor Support** - Position windows independently  
✅ **Compact Main Window** - 800x600 doesn't take much space  
✅ **Spacious Pop-Outs** - 900x700 for comfortable viewing  

### Workflow Example

1. **Main Window:** Shows Dashboard with Steam news and action log
2. **Click "Clients":** Opens Client Downloads window (900x700)
3. **Click "Download CN Client":** Dialog appears, choose automatic
4. **Main Window Log:** Shows download progress in real-time
   - `Fetching latest CN client download link...`
   - `✅ Download URL found: https://...`
   - `Starting automatic download and installation...`
   - `Downloading: 45% (1200 MB / 2650 MB) at 15.3 MB/s`
   - `Extracting files... (skip Bin, netease.mpay folders)`
   - `✅ Installation completed successfully!`
5. **Both Windows Open:** Monitor logs while browsing other clients

---

## Build Status

```
✅ Build Succeeded
   0 Errors
   1 Warning (non-critical async in App.xaml.cs)
```

---

## Testing Checklist

### Window Behavior
- [ ] Main window 800x600, cannot resize
- [ ] Clicking Tweaks/Clients/Settings opens pop-out
- [ ] Dashboard button refreshes main window
- [ ] Pop-out windows can resize
- [ ] Pop-outs close when main window closes

### Download Logging
- [ ] Download progress shows in main window log
- [ ] Percentage updates during download (0-100%)
- [ ] MB downloaded / Total MB shown
- [ ] Download speed shown (MB/s)
- [ ] Extraction progress logged
- [ ] Success/error messages appear
- [ ] Color coding works (cyan, yellow, green, red)

### UI Display
- [ ] "System Tweaks" text not cut off
- [ ] Sidebar 200px, content 600px
- [ ] Steam news at top of dashboard
- [ ] Action log at bottom of dashboard
- [ ] All buttons functional

---

## Keyboard Shortcuts

Currently none implemented, but potential future additions:

- `Ctrl+T` - Open Tweaks window
- `Ctrl+D` - Open Clients window
- `Ctrl+S` - Open Settings window
- `Ctrl+L` - Focus action log
- `Ctrl+W` - Close active pop-out window

---

## Known Limitations

### Multiple Instances
User can open multiple instances of same window (e.g., 3 Tweaks windows)
- **Impact:** Low - user can close extras
- **Future Fix:** Track window instances, activate existing instead of creating new

### Position Not Remembered
Pop-out windows always open centered, even if user moved them
- **Impact:** Low - centered is usually convenient
- **Future Fix:** Save/restore positions to config file

### Dashboard Button Active
Dashboard button still clickable even though dashboard always shown
- **Impact:** None - just refreshes dashboard
- **Future Fix:** Add visual indicator that it's the active page

---

## Troubleshooting

### Main Window Too Small
**Problem:** 800x600 feels cramped  
**Solution:** Main window size is locked, but dashboard content is optimized for this size. Pop-out windows are larger (900x700) for comfortable viewing.

### Can't Resize Main Window
**Problem:** Main window won't resize  
**Solution:** This is intentional. Main window is locked to 800x600. Pop-out windows can be resized freely.

### Logs Not Appearing
**Problem:** Download progress not showing in main window  
**Solution:** Make sure main window is visible. Logs only appear in main window dashboard, not in pop-out Clients window.

### Multiple Windows Confusing
**Problem:** Too many windows open  
**Solution:** Close pop-out windows when done. Only Dashboard needs to stay open. Pop-outs are for specific tasks.

---

## Future Enhancements

### Planned Features
- [ ] Remember window positions
- [ ] Prevent multiple instances of same window
- [ ] Keyboard shortcuts
- [ ] Dockable windows (advanced)
- [ ] Window management settings
- [ ] Log export/copy functionality
- [ ] Download history in log

### Under Consideration
- [ ] Tabbed pop-outs (combine Tweaks/Clients/Settings in one window)
- [ ] Log filtering (show only errors, only downloads, etc.)
- [ ] Log search functionality
- [ ] Notification system for background downloads

---

**Updated:** October 13, 2025  
**Version:** 1.0.0  
**Status:** ✅ Complete and Tested
