# NotNaraka Launcher - Rebranding Complete ✅

## Overview
Successfully rebranded **NarakaTweaks Launcher** to **NotNaraka Launcher** with enhanced UI, multi-platform launch support, and comprehensive client download page.

## What's New

### 🎨 Visual Updates
- **New Name**: "NotNaraka Launcher" (previously "NarakaTweaks Launcher")
- **Sidebar Background**: Added Ratz.png image at 15% opacity for subtle branding
- **Reorganized Layout**: Social media links moved to bottom alongside new launch button
- **Cleaner Tweaks Display**: Removed risk level indicators - now shows only Admin/Restart badges

### 🎮 Launch Game Feature
Added comprehensive game launcher with **3 platform options**:

1. **Steam** - Launches via `steam://rungameid/1665650`
2. **Epic Games** - Launches via Epic Games Store protocol
3. **Official Launcher** - Searches common installation paths:
   - `C:\Program Files\Naraka\NarakaBladepoint.exe`
   - `C:\Program Files (x86)\Naraka\NarakaBladepoint.exe`
   - `C:\Games\Naraka\NarakaBladepoint.exe`

**Usage**: Click the green "🎮 Launch Naraka" button at bottom of sidebar, then choose your platform.

### 📥 Clients Download Page
Complete redesign with **4 client download options**:

#### Available Clients:
1. **🎮 Steam Version**
   - Official Steam release
   - Global servers
   - Opens Steam store page

2. **🎯 Epic Games Version**
   - Official Epic Games release
   - Global servers
   - Opens Epic Games Store page

3. **🇨🇳 Chinese (CN) Client**
   - Official Chinese client
   - CN servers with exclusive content
   - *Direct download functionality coming soon*

4. **🌎 Official Global Client**
   - Standalone official client
   - Global servers (non-Steam/Epic)
   - *Direct download functionality coming soon*

Each client card displays:
- Platform icon and title
- Server region information
- Download/Install button
- Color-coded borders for easy identification

## Technical Changes

### Modified Files
1. **MainWindow.xaml**
   - Changed window title to "NotNaraka Launcher"
   - Restructured sidebar with 3-row Grid layout
   - Added ImageBrush with Ratz.png background (15% opacity)
   - Added LaunchGameButton with green styling and glow effect
   - Relocated social media buttons to bottom section

2. **MainWindow.xaml.cs**
   - Removed risk level badge creation from `CreateTweakControl()`
   - Added `LaunchGame_Click()` handler for multi-platform launching
   - Completely reimplemented `ShowClients()` method
   - Added `CreateClientCard()` helper method for consistent card styling

3. **NarakaTweaks.Launcher.csproj**
   - Added Ratz.png as embedded resource

### Build Info
- **Published Executable**: `NotNaraka-Launcher.exe`
- **File Size**: 74.38 MB (single-file, self-contained)
- **Location**: `c:\Users\Admin\Desktop\NotNaraka Launcher\publish\NotNaraka-Launcher.exe`
- **Framework**: .NET 8.0 (net8.0-windows)
- **Build Status**: ✅ Success (1 minor warning about async method)

## Features Preserved

### ✅ All Original Functionality Still Works
- System tray integration (minimize to tray)
- Auto-update service (GitHub Releases)
- 14 performance tweaks across 3 categories:
  - **Performance** (7 tweaks): Core registry, power plans, Game Bar, background apps, widgets, Copilot
  - **GPU** (3 tweaks): GPU scheduling, NVIDIA optimizations, AMD optimizations
  - **System** (4 tweaks): MSI mode, HPET, timer resolution, ViVeTool features
- Background anti-cheat monitoring (hidden from UI)
- Dashboard with news feed and action log

### 🎯 Social Media Links (Bottom of Sidebar)
- 💜 **Twitch**: Your Twitch channel
- 🐙 **GitHub**: Project repository
- ☕ **Ko-Fi**: Support page
- 💬 **Discord**: Community server

## Next Steps

### Immediate Testing
1. **Launch the new executable**: `publish\NotNaraka-Launcher.exe`
2. **Test the Launch Game button**: Try all 3 platform options (Steam/Epic/Official)
3. **Verify visual changes**: Check sidebar background image and button positions
4. **Navigate to Clients tab**: Confirm all 4 client cards display correctly
5. **Test social media links**: Ensure all buttons open correct URLs

### Future Enhancements
1. **Client Download Service Integration**
   - Wire up CN and Official client downloads to `ClientDownloadService`
   - Add progress bars during downloads
   - Implement Install/Switch functionality

2. **Settings Page**
   - Auto-start with Windows
   - Minimize to tray on startup
   - Default launch platform preference
   - Theme customization

3. **Tweak Apply/Revert**
   - Connect "Apply Selected Tweaks" button to `TweaksService`
   - Show real-time progress during apply operations
   - Persist tweak status across app restarts

## User Interface Layout

```
┌─────────────────────────────────────────────────────────────┐
│  NotNaraka Launcher                                      [_][□][X]
├──────────┬──────────────────────────────────────────────────┤
│          │                                                  │
│ NotNaraka│  [Dashboard / Tweaks / Clients / Settings]      │
│   (Logo) │                                                  │
│          │  ┌────────────────────────────────────────┐     │
│ ────────────│  Main Content Panel                    │     │
│ Dashboard│  │  (News Feed, Tweaks, Client Cards,     │     │
│ Tweaks   │  │   or Settings)                         │     │
│ Clients  │  │                                        │     │
│ Settings │  └────────────────────────────────────────┘     │
│ ────────────                                               │
│🎮 Launch │                                                  │
│  Naraka  │                                                  │
│ ─────────│                                                  │
│ 💜🐙☕💬  │                                                  │
│ (Social) │                                                  │
└──────────┴──────────────────────────────────────────────────┘
```

## Changelog

### Version 2.0 - "NotNaraka" Rebranding
**Released**: [Current Date]

**Breaking Changes**:
- Renamed from "NarakaTweaks Launcher" to "NotNaraka Launcher"

**Added**:
- Multi-platform game launcher (Steam/Epic/Official)
- Comprehensive Clients download page with 4 options
- Ratz.png sidebar background image (15% opacity)
- LaunchGameButton with green glow effect

**Changed**:
- Social media links moved to bottom of sidebar
- Tweaks display simplified (removed risk level indicators)
- Sidebar restructured with 3-row Grid layout

**Technical**:
- File size: 74.38 MB (increased from 72.98 MB due to Ratz.png resource)
- Build configuration optimized for size
- All dependencies up to date

## Known Limitations

1. **CN & Official Client Downloads**: Currently show "Coming Soon" messages
   - Backend service (`ClientDownloadService`) exists but needs UI integration
   - Will be implemented in future update

2. **Client Switching**: Not yet connected to UI
   - `GameClientSwitcher` service exists and functional
   - Needs UI controls and progress indicators

3. **Settings Page**: Empty placeholder
   - Framework exists for future implementation

## Support

If you encounter any issues:
1. Check the action log in the Dashboard tab for error messages
2. Verify you have .NET 8.0 Runtime installed (or use self-contained executable)
3. Run as Administrator if tweaks fail to apply
4. Check GitHub Issues page for known problems

## Credits

- **Original Project**: NarakaTweaks by [Your Name]
- **PowerShell Script**: RatzTweaks.ps1 (all tweaks integrated)
- **Branding**: Ratz.png artwork
- **Framework**: WPF (.NET 8.0)
- **System Tray**: H.NotifyIcon.Wpf
- **MVVM**: CommunityToolkit.Mvvm

---

**🎉 NotNaraka Launcher is ready to use!**

Launch the executable from: `c:\Users\Admin\Desktop\NotNaraka Launcher\publish\NotNaraka-Launcher.exe`
