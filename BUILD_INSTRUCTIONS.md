# NarakaTweaks Launcher - Build Instructions

## 🏗️ Building the Project

### Prerequisites

1. **Install .NET 8.0 SDK**
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify installation: `dotnet --version`

2. **Install Windows 10/11 SDK**
   - Download from: https://developer.microsoft.com/windows/downloads/windows-sdk/
   - Required version: 10.0.19041.0 or higher

3. **Install Visual Studio 2022 (Optional but Recommended)**
   - Community Edition is free
   - Select workload: ".NET Desktop Development" and "Windows App SDK"

### Quick Build

```powershell
# Navigate to project root
cd "c:\Users\Admin\Desktop\NotNaraka Launcher"

# Restore dependencies
dotnet restore

# Build all projects
dotnet build NarakaTweaks.AntiCheat/NarakaTweaks.AntiCheat.csproj --configuration Release
dotnet build NarakaTweaks.Core/NarakaTweaks.Core.csproj --configuration Release
dotnet build NarakaTweaks.Launcher/NarakaTweaks.Launcher.csproj --configuration Release
```

### Build & Run

```powershell
# Build and run the launcher
cd NarakaTweaks.Launcher
dotnet run
```

### Publish Single-File Executable

```powershell
# Publish for Windows x64
dotnet publish NarakaTweaks.Launcher/NarakaTweaks.Launcher.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:WindowsAppSDKSelfContained=true `
  --output ./publish/win-x64

# Output will be in: ./publish/win-x64/NarakaTweaks.Launcher.exe
```

### Build for Multiple Architectures

```powershell
# x64
dotnet publish -r win-x64 -c Release -o ./publish/x64

# ARM64 (for Surface devices, etc.)
dotnet publish -r win-arm64 -c Release -o ./publish/arm64
```

## 🧪 Testing

### Run Tests

```powershell
# If you create test projects
dotnet test
```

### Manual Testing

```powershell
# Test anti-cheat service
cd NarakaTweaks.AntiCheat
dotnet run --project TestApp.csproj  # If you create a test console app
```

## 🎨 Using Visual Studio

1. **Open Solution**
   ```
   Open "Naraka-Cheat-Detector.sln" in Visual Studio
   ```

2. **Add New Projects** (if needed)
   - Right-click solution → Add → Existing Project
   - Add: `NarakaTweaks.AntiCheat.csproj`
   - Add: `NarakaTweaks.Core.csproj`
   - Add: `NarakaTweaks.Launcher.csproj`

3. **Set Startup Project**
   - Right-click `NarakaTweaks.Launcher` → Set as Startup Project

4. **Build & Run**
   - Press F5 to build and run with debugging
   - Press Ctrl+F5 to run without debugging

## 🔧 Troubleshooting

### Issue: "Could not load file or assembly"

**Solution:** Ensure all project references are correct
```powershell
dotnet restore
dotnet clean
dotnet build
```

### Issue: "Windows App SDK not found"

**Solution:** Install the Windows App SDK
```powershell
dotnet workload install microsoft-windows-sdk-net-ref
```

### Issue: WinUI build errors

**Solution:** Update packages
```powershell
dotnet add package Microsoft.WindowsAppSDK --version 1.8.250916003
dotnet restore
```

### Issue: "Access denied" when applying tweaks

**Solution:** Run as Administrator
```powershell
# Right-click the .exe and select "Run as administrator"
```

## 📦 Distribution

### Create Portable ZIP

```powershell
# After publishing
cd publish/win-x64
Compress-Archive -Path * -DestinationPath NarakaTweaks-Portable.zip
```

### Create Installer (Future)

Options:
1. **MSIX Package** - Built into the project
2. **Inno Setup** - Classic Windows installer
3. **Squirrel.Windows** - Auto-updating installer

## 🚀 Deployment Checklist

Before releasing:

- [ ] Test on clean Windows 10/11 installation
- [ ] Verify all tweaks work correctly
- [ ] Test anti-cheat detection
- [ ] Test client download and switching
- [ ] Check for admin privilege handling
- [ ] Verify rollback functionality
- [ ] Test on different hardware (NVIDIA/AMD)
- [ ] Create user documentation
- [ ] Set up auto-update mechanism
- [ ] Code sign the executable (optional but recommended)

## 📊 Build Sizes

Expected file sizes:
- Debug build: ~80-100 MB
- Release build: ~50-70 MB
- Trimmed release: ~30-50 MB (with proper trimming settings)

## 🔐 Code Signing (Optional)

For production releases:

```powershell
# Sign the executable
signtool sign /f YourCertificate.pfx /p YourPassword /t http://timestamp.digicert.com NarakaTweaks.Launcher.exe
```

## 📝 Version Management

Update version in `.csproj` files:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
</PropertyGroup>
```

## 🌐 Platform Support

Supported platforms:
- ✅ Windows 10 (19041+)
- ✅ Windows 11
- ✅ x64 architecture
- ✅ ARM64 architecture
- ❌ x86 (32-bit) - Not recommended

---

**Ready to build?** Run the Quick Build commands above to get started!
