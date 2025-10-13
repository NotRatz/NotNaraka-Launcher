# NarakaTweaks Launcher - Quick Reference

## 🎯 Current Status

✅ **System Tray** - Minimize to tray, context menu, notifications  
✅ **Auto-Update** - GitHub Releases integration, automatic updates  
✅ **Anti-Cheat** - Running silently in background (hidden from UI)  
✅ **Core Services** - Tweaks, Downloads, Client Switcher ready  
🔄 **UI Pages** - Tweaks & Clients pages need implementation  

---

## 📦 Published Output

**Location**: `publish\win-x64\NarakaTweaks.Launcher.exe`  
**Size**: 73.14 MB  
**Type**: Single-file, self-contained, portable  
**Requirements**: Windows 7+ (no runtime needed)

---

## 🚀 Quick Start

```powershell
# Run the launcher
.\publish\win-x64\NarakaTweaks.Launcher.exe

# Build from source
dotnet build NarakaTweaks.Launcher\NarakaTweaks.Launcher.csproj -c Release

# Publish
dotnet publish NarakaTweaks.Launcher\NarakaTweaks.Launcher.csproj -c Release -r win-x64 --self-contained -o publish\win-x64 -p:PublishSingleFile=true
```

---

## 🔧 Configure Auto-Update

In `App.xaml.cs` line ~60:

```csharp
// Initialize auto-update service
_updateService = new AutoUpdateService();
_updateService.Configure("YourGitHubUsername", "YourRepoName");
```

Then create GitHub Release with tag `v1.0.0` and attach `.exe` file.

---

## 📋 Next Implementation Tasks

### 1. Tweaks UI Page (HIGH PRIORITY)
- Category tabs: Performance, GPU, Network, Privacy
- Checkboxes for each of 14+ tweaks
- "Apply Selected" and "Revert All" buttons
- Status indicators (Applied/Not Applied)
- Link to `TweaksService.GetAvailableTweaks()`

### 2. Clients UI Page (HIGH PRIORITY)
- Region selector dropdown (Global/Chinese)
- Download progress bars
- "Install" and "Switch" buttons
- Current region detection display
- Link to `ClientDownloadService` and `GameClientSwitcher`

### 3. Settings Page (MEDIUM PRIORITY)
- Auto-start with Windows checkbox
- Minimize to tray on startup
- Scan interval slider
- Update check frequency
- Theme selector

---

## 🎨 UI Implementation Guide

### Tweaks Page Template

```xaml
<TabControl>
    <TabItem Header="Performance">
        <ScrollViewer>
            <StackPanel>
                <CheckBox Content="Disable Hibernation"/>
                <CheckBox Content="High Performance Power Plan"/>
                <!-- More tweaks -->
                <Button Content="Apply Selected" Click="ApplyTweaks"/>
            </StackPanel>
        </ScrollViewer>
    </TabItem>
    <!-- More tabs -->
</TabControl>
```

```csharp
private async void ApplyTweaks(object sender, RoutedEventArgs e)
{
    var selectedTweaks = GetSelectedTweaks();
    foreach (var tweak in selectedTweaks)
    {
        await App.Current.TweaksService.ApplyTweakAsync(tweak.Id);
    }
}
```

### Clients Page Template

```xaml
<StackPanel>
    <ComboBox x:Name="RegionSelector">
        <ComboBoxItem Content="Global (Official)" Tag="global"/>
        <ComboBoxItem Content="Chinese (CN)" Tag="chinese"/>
    </ComboBox>
    
    <ProgressBar x:Name="DownloadProgress" Height="30"/>
    <TextBlock x:Name="DownloadStatus"/>
    
    <Button Content="Download Client" Click="DownloadClient"/>
    <Button Content="Switch Region" Click="SwitchRegion"/>
</StackPanel>
```

```csharp
private async void DownloadClient(object sender, RoutedEventArgs e)
{
    var region = RegionSelector.SelectedItem.Tag.ToString();
    var service = App.Current.DownloadService;
    
    service.ProgressChanged += (s, progress) => {
        DownloadProgress.Value = progress.ProgressPercent;
    };
    
    await service.DownloadClientAsync(region);
}
```

---

## 🔍 Service Access Pattern

```csharp
// From any Window or UserControl
var tweaks = App.Current.TweaksService;
var downloads = App.Current.DownloadService;
var switcher = App.Current.ClientSwitcher;
var updates = App.Current.UpdateService;

// Example: Get available tweaks
var categories = await tweaks.GetAvailableTweaks();

// Example: Apply a tweak
await tweaks.ApplyTweakAsync("disable-hibernation");

// Example: Download client
await downloads.DownloadClientAsync("global");
```

---

## 📁 Project Structure

```
NarakaTweaks.Launcher/        # WPF UI
├── App.xaml/cs               # Application entry, services, tray
├── MainWindow.xaml/cs        # Main window, navigation
└── Assets/                   # Icons, images

NarakaTweaks.Core/            # Core services
├── TweaksService.cs          # 14+ system tweaks
├── ClientDownloadService.cs  # Multi-region downloads
├── GameClientSwitcher.cs     # Platform detection, switching
└── AutoUpdateService.cs      # GitHub Releases integration

NarakaTweaks.AntiCheat/       # Background monitoring
└── AntiCheatService.cs       # Silent detection (hidden)
```

---

## 🎯 Key Design Decisions

1. **Anti-Cheat Hidden** - Runs silently, no UI exposure
2. **Minimize to Tray** - Close button doesn't exit
3. **Auto-Update** - Checks on startup, downloads in background
4. **Single File** - 73 MB portable .exe
5. **No Runtime** - Works on Windows 7+ without .NET install

---

## 🐛 Troubleshooting

**App doesn't show window:**
- Check `C:\Users\Admin\Desktop\launcher_error.txt`
- Verify all XAML resources compile correctly

**Tray icon not appearing:**
- Check Windows notification area settings
- Try restarting Explorer.exe

**Update not detecting:**
- Verify GitHub repo is configured
- Check GitHub Release has `.exe` asset
- Ensure version tag starts with `v`

---

## 📞 Implementation Priority

**Immediate (This Session)**:
1. ✅ System tray integration
2. ✅ Auto-update system
3. ✅ Hide anti-cheat from UI

**Next Session**:
4. Build Tweaks UI page
5. Build Clients UI page
6. Settings page

**Future**:
7. Installer (Inno Setup)
8. Code signing
9. Telemetry/analytics

---

**Status**: Ready for UI implementation! 🚀
