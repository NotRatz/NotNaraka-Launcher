# Building Naraka Cheat Detector as a Portable Executable

This guide explains how to compile the Naraka Cheat Detector into a portable, self-contained executable that can run on any Windows 10+ machine without requiring .NET installation.

## Quick Start

### Option 1: Using the Portable Build Script (Recommended)

```powershell
.\build-portable.ps1
```

This creates a fully portable executable in the `dist` folder.

### Option 2: Using the Publish Script

```powershell
.\publish.ps1
```

This creates a self-contained executable in `artifacts\publish`.

### Option 3: Manual dotnet Command

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Build Options

### Target Platforms

Build for different Windows architectures:

```powershell
# 64-bit Windows (default, recommended)
.\build-portable.ps1 -Runtime win-x64

# 32-bit Windows
.\build-portable.ps1 -Runtime win-x86

# ARM64 Windows
.\build-portable.ps1 -Runtime win-arm64
```

### Build Configurations

```powershell
# Release build (optimized, smaller size)
.\build-portable.ps1 -Configuration Release

# Debug build (includes debugging symbols)
.\build-portable.ps1 -Configuration Debug
```

### Custom Output Directory

```powershell
.\build-portable.ps1 -Output "C:\MyBuilds\NarakaDetector"
```

## What Makes it Portable?

The build process creates a truly portable executable by:

1. **Self-Contained Deployment**: Bundles the .NET 8 runtime with the executable
2. **Single-File Publishing**: All dependencies packaged into one .exe file
3. **Native Library Embedding**: Windows-specific libraries included
4. **No Installation Required**: Users can run the .exe directly
5. **Compression**: Reduces file size while maintaining performance

## Build Outputs

After building, you'll find:

- **Naraka-Cheat-Detector.exe** - The main portable executable (~100-150 MB)
- **Assets/** folder - Application resources (icon, images)
- **scan_log.txt** - Created at runtime for diagnostics

## Distribution

To distribute your portable application:

1. **Single Executable**: Just share `Naraka-Cheat-Detector.exe`
   - Users can run it immediately on Windows 10+
   - No .NET installation needed

2. **With Assets**: Include the Assets folder if runtime resources are needed
   ```
   MyApp/
   ??? Naraka-Cheat-Detector.exe
   ??? Assets/
       ??? icon.png
       ??? icon.ico
       ??? background.png
   ```

## System Requirements

### For Building
- Windows 10 or later
- .NET 8 SDK installed
- PowerShell 5.1 or PowerShell Core 7+

### For Running (End Users)
- Windows 10 version 1809 or later
- No additional software required

## Build Scripts Comparison

| Script | Purpose | Output Size | Best For |
|--------|---------|-------------|----------|
| `build-portable.ps1` | Create portable exe | Large (~150MB) | Distribution to users |
| `publish.ps1` | Flexible publishing | Configurable | Advanced scenarios |
| `build.ps1` | Quick development build | Small | Development/testing |

## Troubleshooting

### "dotnet command not found"

Install the .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0

### Large File Size

The self-contained executable is larger because it includes the .NET runtime. To reduce size:

```powershell
# Use framework-dependent mode (requires .NET 8 on target machine)
.\publish.ps1 -FrameworkDependent
```

### Build Errors

1. Ensure you're in the project directory
2. Restore packages first:
   ```powershell
   dotnet restore
   ```
3. Check the build log in the console output

### Runtime Errors

If the executable fails to run:

1. Check `scan_log.txt` for error details
2. Ensure Windows 10 version 1809+ is installed
3. Try running as Administrator (required for some detection methods)

## Advanced Options

### Trimming and Optimization

The Release build automatically includes:
- **Assembly Trimming**: Removes unused code
- **ReadyToRun**: Pre-compiles for faster startup
- **Compression**: Reduces file size

To disable trimming (may fix compatibility issues):

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false
```

### Creating Multiple Builds

Build all platforms at once:

```powershell
@('win-x64', 'win-x86', 'win-arm64') | ForEach-Object {
    .\build-portable.ps1 -Runtime $_ -Output "dist\$_"
}
```

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Build Portable Executable
  run: |
    .\build-portable.ps1 -Configuration Release -Runtime win-x64
    
- name: Upload Artifact
  uses: actions/upload-artifact@v3
  with:
    name: portable-exe
    path: dist/Naraka-Cheat-Detector.exe
```

## Project Configuration

The portable build is configured in `Naraka-Cheat-Detector.csproj`:

```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```

## Support

For build issues:
1. Check this documentation
2. Review `scan_log.txt` for runtime errors
3. Ensure .NET 8 SDK is properly installed
4. Try cleaning and rebuilding:
   ```powershell
   dotnet clean
   .\build-portable.ps1
   ```
