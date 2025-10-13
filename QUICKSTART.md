# NarakaTweaks Launcher - Quick Start Guide

## 🚀 Getting Started

This guide will help you complete and run the NarakaTweaks Launcher.

## ✅ What's Already Built

The following core services are **complete and ready to use**:

### 1. **AntiCheatService** (`NarakaTweaks.AntiCheat/`)
- Background monitoring service
- Process, file, registry, and memory scanning
- Configurable scan intervals
- Event-driven threat detection

### 2. **ClientDownloadService** (`NarakaTweaks.Core/Services/`)
- Download client packages with progress tracking
- Extract specific files from ZIP archives
- Checksum validation
- Cache management

### 3. **GameClientSwitcher** (`NarakaTweaks.Core/Services/`)
- Switch between Global and Chinese clients
- Auto-detect Steam/Epic Games installations
- Automatic backup and rollback
- Force game validation

### 4. **TweaksService** (`NarakaTweaks.Core/Services/`)
- 14+ system optimizations
- Performance, GPU, Network, and Privacy tweaks
- Automatic backup before applying
- One-click revert functionality

## 🔨 What Needs to Be Built

### Priority 1: Main Launcher UI

Create a new WinUI3 project or modify the existing `NarakaLauncher`:

1. **Create Main Window**
   ```
   NarakaTweaks.Launcher/
   ├── MainWindow.xaml          # TabView with 4 tabs
   ├── MainWindow.xaml.cs
   └── Views/
       ├── DashboardView.xaml   # Overview & status
       ├── TweaksView.xaml      # Tweak toggles
       ├── ClientsView.xaml     # Download manager
       └── SettingsView.xaml    # Settings & logs
   ```

2. **Wire Up Services**
   ```csharp
   // In App.xaml.cs
   var antiCheat = new AntiCheatService();
   var tweaks = new TweaksService();
   var downloads = new ClientDownloadService();
   var switcher = new GameClientSwitcher();
   
   antiCheat.Start(); // Background monitoring
   ```

3. **Create ViewModels**
   - `MainViewModel` - Coordinates all tabs
   - `DashboardViewModel` - Shows anti-cheat status
   - `TweaksViewModel` - Lists available tweaks with toggles
   - `ClientsViewModel` - Download progress and switching
   - `SettingsViewModel` - Configuration and logs

### Priority 2: System Tray

Add `H.NotifyIcon.WinUI` package for system tray:

```powershell
dotnet add package H.NotifyIcon.WinUI
```

Implement:
- Tray icon with right-click menu
- Minimize to tray instead of closing
- Toast notifications for threats
- Quick actions menu

### Priority 3: Testing

Create test project to validate services:

```powershell
# Test anti-cheat
dotnet new xunit -n NarakaTweaks.Tests
cd NarakaTweaks.Tests
dotnet add reference ../NarakaTweaks.AntiCheat/NarakaTweaks.AntiCheat.csproj
```

## 💻 Development Workflow

### Step 1: Set Up Solution

```powershell
# Navigate to project root
cd "c:\Users\Admin\Desktop\NotNaraka Launcher"

# Add projects to solution (if not already added)
dotnet sln add NarakaTweaks.AntiCheat/NarakaTweaks.AntiCheat.csproj
dotnet sln add NarakaTweaks.Core/NarakaTweaks.Core.csproj
```

### Step 2: Build Core Services

```powershell
# Build all projects
dotnet build NarakaTweaks.AntiCheat/NarakaTweaks.AntiCheat.csproj
dotnet build NarakaTweaks.Core/NarakaTweaks.Core.csproj
```

### Step 3: Create Launcher Project

Option A: Modify existing NarakaLauncher (WPF)
```powershell
cd NarakaLauncher
dotnet add reference ../NarakaTweaks.AntiCheat/NarakaTweaks.AntiCheat.csproj
dotnet add reference ../NarakaTweaks.Core/NarakaTweaks.Core.csproj
```

Option B: Create new WinUI3 project
```powershell
# Create new WinUI3 project
dotnet new winui -n NarakaTweaks.Launcher
cd NarakaTweaks.Launcher
dotnet add reference ../NarakaTweaks.AntiCheat/NarakaTweaks.AntiCheat.csproj
dotnet add reference ../NarakaTweaks.Core/NarakaTweaks.Core.csproj
```

### Step 4: Integrate Services in UI

**Example: Dashboard with Anti-Cheat Status**

```csharp
// DashboardViewModel.cs
public class DashboardViewModel : ObservableObject
{
    private readonly AntiCheatService _antiCheat;
    private string _status = "Initializing...";
    private int _threatsDetected = 0;
    
    public DashboardViewModel(AntiCheatService antiCheat)
    {
        _antiCheat = antiCheat;
        _antiCheat.StatusChanged += OnStatusChanged;
        _antiCheat.ThreatDetected += OnThreatDetected;
        _antiCheat.Start();
    }
    
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
    
    public int ThreatsDetected
    {
        get => _threatsDetected;
        set => SetProperty(ref _threatsDetected, value);
    }
    
    private void OnStatusChanged(object? sender, string status)
    {
        Status = status;
    }
    
    private void OnThreatDetected(object? sender, DetectionEventArgs e)
    {
        ThreatsDetected++;
        // Show notification
    }
    
    public async Task ScanNowAsync()
    {
        Status = "Scanning...";
        var result = await _antiCheat.ScanNowAsync();
        Status = $"Scan complete: {result.ThreatsDetected} threats";
    }
}
```

**Example: Tweaks View**

```xml
<!-- TweaksView.xaml -->
<Page>
    <ScrollViewer>
        <StackPanel Spacing="12">
            <!-- Performance Tweaks -->
            <TextBlock Text="Performance" FontSize="20" FontWeight="Bold"/>
            <ItemsRepeater ItemsSource="{x:Bind ViewModel.PerformanceTweaks}">
                <DataTemplate>
                    <ToggleSwitch 
                        Header="{Binding Name}"
                        IsOn="{Binding IsEnabled, Mode=TwoWay}"
                        OnContent="Enabled"
                        OffContent="Disabled"
                        Command="{Binding ToggleCommand}"/>
                </DataTemplate>
            </ItemsRepeater>
            
            <!-- GPU Tweaks -->
            <TextBlock Text="GPU" FontSize="20" FontWeight="Bold"/>
            <!-- ... -->
        </StackPanel>
    </ScrollViewer>
</Page>
```

## 🧪 Testing the Services

### Test Anti-Cheat Service

```csharp
// Program.cs or test file
using NarakaTweaks.AntiCheat;

var service = new AntiCheatService();

service.StatusChanged += (s, status) => Console.WriteLine($"Status: {status}");
service.ThreatDetected += (s, e) => 
{
    Console.WriteLine($"⚠️ Threat: {e.Detection.Description}");
    Console.WriteLine($"   Type: {e.Detection.Type}");
    Console.WriteLine($"   Severity: {e.Detection.Severity}");
};

service.Start();
Console.WriteLine("Anti-cheat service started. Press any key to scan...");
Console.ReadKey();

var result = await service.ScanNowAsync();
Console.WriteLine($"\nScan completed in {result.Duration.TotalSeconds:F2}s");
Console.WriteLine($"Threats detected: {result.ThreatsDetected}");

foreach (var detection in result.Detections)
{
    Console.WriteLine($"  - {detection.Description}");
}

await service.StopAsync();
```

### Test Tweaks Service

```csharp
using NarakaTweaks.Core.Services;

var tweaks = new TweaksService();

tweaks.StatusChanged += (s, status) => Console.WriteLine(status);

// Get all available tweaks
var allTweaks = tweaks.GetAvailableTweaks();

foreach (var category in allTweaks)
{
    Console.WriteLine($"\n{category.Key}:");
    foreach (var tweak in category.Value)
    {
        Console.WriteLine($"  - {tweak.Name}");
        Console.WriteLine($"    {tweak.Description}");
    }
}

// Apply a tweak
var highPerf = allTweaks[TweakCategory.Performance]
    .First(t => t.Id == "high-performance-power");

var result = await tweaks.ApplyTweakAsync(highPerf);
Console.WriteLine($"Apply result: {(result.Success ? "✓" : "✗")}");
```

### Test Client Downloader

```csharp
using NarakaTweaks.Core.Services;

var downloader = new ClientDownloadService();

downloader.StatusChanged += (s, status) => Console.WriteLine(status);
downloader.DownloadProgress += (s, e) => 
{
    Console.WriteLine($"{e.Stage}: {e.Progress}%");
};

var clients = downloader.GetAvailableClients();
foreach (var client in clients)
{
    Console.WriteLine($"{client.Name}: {client.Description}");
}
```

## 📦 Publishing

### Single-File Executable

```powershell
dotnet publish NarakaTweaks.Launcher/NarakaTweaks.Launcher.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:WindowsAppSDKSelfContained=true `
  -o ./publish
```

### MSIX Package

```powershell
# Add to .csproj
<PropertyGroup>
  <WindowsPackageType>MSIX</WindowsPackageType>
  <GenerateAppInstallerFile>True</GenerateAppInstallerFile>
</PropertyGroup>

# Build and package
msbuild /t:Publish /p:Configuration=Release
```

## 🎨 UI Design Tips

### Color Scheme (Dark Theme)
- Background: `#1E1E1E`
- Primary: `#0078D7` (Blue)
- Success: `#107C10` (Green)
- Warning: `#FFC107` (Yellow)
- Danger: `#D13438` (Red)
- Text: `#FFFFFF` / `#E0E0E0`

### Icons
Use **Segoe MDL2 Assets** font for icons:
- `` - Shield (Anti-cheat)
- `` - Settings (Tweaks)
- `` - Download
- `` - Globe (Clients)

## ❓ Troubleshooting

### Issue: Build Errors

```powershell
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### Issue: WinUI3 Package Issues

```powershell
# Update WinAppSDK
dotnet add package Microsoft.WindowsAppSDK --version 1.8.250916003
```

### Issue: Admin Privileges Required

Some tweaks require administrator privileges. Handle with:

```csharp
public static bool IsAdministrator()
{
    var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
}
```

## 📚 Next Steps

1. ✅ Review the implementation document: `LAUNCHER_IMPLEMENTATION.md`
2. 🔨 Build the main launcher UI
3. 🎨 Design the interface (reference PSO2Tweaks)
4. 🧪 Test all services thoroughly
5. 📦 Create installer package
6. 🚀 Release v1.0!

---

**Need Help?** Check `LAUNCHER_IMPLEMENTATION.md` for detailed architecture and API documentation.
