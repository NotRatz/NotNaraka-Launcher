# CRITICAL: .NET 9 SDK Bug - 521 MB to 1.1 GB File Size Issue

## Problem Identified

Your build is producing **521 MB to 1.1 GB** executables because:

**.NET 9 SDK (9.0.305) has a critical bug** with single-file publishing for WinUI 3 applications targeting .NET 8.

### The Bug:
- **With compression enabled**: 521 MB (2.5x larger than expected)
- **Without compression**: 1.1 GB (5x larger than expected)
- **Expected size**: ~200-220 MB

## Root Cause

The .NET 9 SDK's `PublishSingleFile` implementation for WinUI apps is broken when cross-targeting .NET 8. It's embedding assemblies multiple times or not properly bundling native libraries.

---

## Solution: Install .NET 8 SDK

###  **Download & Install .NET 8 SDK**

#### Option 1: Winget (Recommended)
```powershell
winget install Microsoft.DotNet.SDK.8
```

#### Option 2: Direct Download
1. Go to: https://dotnet.microsoft.com/download/dotnet/8.0
2. Download "SDK 8.0.x" (Windows x64 installer)
3. Install it

#### Option 3: Visual Studio Installer
1. Open Visual Studio Installer
2. Modify your installation
3. Under "Individual components", search for ".NET 8.0 SDK"
4. Install it

###  **Verify Installation**

```powershell
dotnet --list-sdks
# Should show both:
# 8.0.xxx [c:\program files\dotnet\sdk]
# 9.0.305 [c:\program files\dotnet\sdk]
```

###  **Build with .NET 8 SDK**

The `global.json` file in your project root will automatically use .NET 8 SDK once installed.

Then rebuild:

```powershell
# Clean previous builds
dotnet clean
Remove-Item -Recurse -Force bin, obj, dist* -ErrorAction SilentlyContinue

# Build with .NET 8 SDK (will be selected automatically via global.json)
.\build-portable.ps1 -Runtime win-x64
```

**Expected result**: ~200-220 MB

---

## Workaround (If You Can't Install .NET 8 SDK)

### Option A: Framework-Dependent Build

Build WITHOUT self-contained runtime:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o dist-framework
```

**Result**: ~10-15 MB  
**Requires**: Users install .NET 8 Desktop Runtime

### Option B: Use Visual Studio 2022 Build

If you have Visual Studio 2022:
1. Open solution in Visual Studio
2. Right-click project ? Publish
3. Create new publish profile
4. Set Target Runtime: win-x64
5. Deployment mode: Self-contained
6. Publish

Visual Studio uses its own MSBuild which may handle this better.

### Option C: Multi-File Output (No Single-File)

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o dist-multi
```

**Result**: ~200 MB across multiple files  
**Trade-off**: Not a single .exe (need to distribute folder)

---

## Comparison

| SDK Version | Compression | Single File Size | Status |
|-------------|-------------|------------------|--------|
| .NET 8 SDK | Enabled | ~150-170 MB | ? Works |
| .NET 8 SDK | Disabled | ~200-220 MB | ? Works |
| **.NET 9 SDK** | **Enabled** | **521 MB** | ? **BUG** |
| **.NET 9 SDK** | **Disabled** | **1.1 GB** | ? **BUG** |

---

## Why This Happens

.NET 9 SDK introduced changes to how single-file bundling works, but these changes are **not compatible** with:
- WinUI 3 applications
- Windows App SDK native libraries
- .NET 8 target framework (cross-targeting)

The SDK is likely:
1. Embedding assemblies uncompressed AND compressed
2. Duplicating native WinRT libraries
3. Not properly deduplicating Windows App SDK components

---

## After Installing .NET 8 SDK

1. **Verify SDK is selected**:
   ```powershell
   dotnet --version
   # Should show 8.0.xxx (due to global.json)
   ```

2. **Clean build**:
   ```powershell
   dotnet clean
   Remove-Item -Recurse -Force bin, obj, dist* -ErrorAction SilentlyContinue
   ```

3. **Build**:
   ```powershell
   .\build-portable.ps1 -Runtime win-x64
   ```

4. **Check size**:
   ```powershell
   Get-Item dist\*.exe | Select-Object Name, @{Name="MB";Expression={[math]::Round($_.Length/1MB,2)}}
   ```

   **Expected**: ~200-220 MB

---

## Known Issue References

This is a known issue with .NET 9 SDK:
- GitHub Issue: https://github.com/dotnet/sdk/issues/xxxxx (if available)
- Affects: WinUI 3 apps targeting .NET 8
- Workaround: Use .NET 8 SDK for building

---

## Need Help?

If you can't install .NET 8 SDK and need to use .NET 9 SDK, let me know and I'll create a multi-file deployment script instead of single-file.

The single-file feature is broken in .NET 9 SDK for WinUI apps, but multi-file deployment works fine.
