# Quick Build Reference

## Create Portable Executable (One Command)

```powershell
.\build-portable.ps1
```

**Output**: `dist\Naraka-Cheat-Detector.exe` (~150-180 MB, fully portable)

---

## Common Build Commands

| Command | Description | Output Size |
|---------|-------------|-------------|
| `.\build-portable.ps1` | Portable, self-contained exe | ~150-180 MB |
| `.\build-portable.ps1 -UltraSize` | IL code optimized | ~140-170 MB |
| `.\build-portable.ps1 -Runtime win-x86` | 32-bit portable exe | ~130-150 MB |
| `.\build-portable.ps1 -Runtime win-x86 -UltraSize` | 32-bit optimized | ~120-140 MB |
| `.\publish.ps1 -FrameworkDependent` | Requires .NET 8 runtime | ~10-15 MB |
| `.\build.ps1` | Development build | ~2 MB |

---

## Build for Distribution

### Recommended for End Users (Standard):
```powershell
.\build-portable.ps1 -Configuration Release -Runtime win-x64
```

Result: Single `.exe` file (~150-180 MB) that works on any Windows 10+ PC

### Recommended for End Users (Smallest Portable):
```powershell
.\build-portable.ps1 -Configuration Release -Runtime win-x86 -UltraSize
```

Result: Single `.exe` file (~120-140 MB) with optimizations  
Works on both 32-bit and 64-bit Windows systems

### All Platforms:
```powershell
# Standard builds
.\build-portable.ps1 -Runtime win-x64 -Output dist\x64
.\build-portable.ps1 -Runtime win-x86 -Output dist\x86
.\build-portable.ps1 -Runtime win-arm64 -Output dist\arm64

# Optimized builds
.\build-portable.ps1 -Runtime win-x64 -UltraSize -Output dist\x64-optimized
.\build-portable.ps1 -Runtime win-x86 -UltraSize -Output dist\x86-optimized
```

---

## Size Optimization Reality Check

### Why is the file so large?

WinUI 3 applications **require the full Windows App SDK runtime** which cannot be trimmed. This is normal:

| Component | Size | Can Remove? |
|-----------|------|-------------|
| .NET 8 Runtime | ~50 MB | ? No |
| Windows App SDK | ~40 MB | ? No |
| WinUI Framework | ~35 MB | ? No |
| Native Libraries | ~20 MB | ? No |
| Your App Code | ~5 MB | Limited |
| Assets | ~5 MB | ? Yes |
| **TOTAL** | **~155 MB** | **~5-15% reduction max** |

### Comparison with Other Apps

| App Type | Size |
|----------|------|
| Chrome Installer | ~90 MB |
| VS Code Portable | ~120 MB |
| Discord Installer | ~80 MB |
| Windows Terminal (WinUI) | ~80 MB (MSIX) |
| **Our App (Portable WinUI)** | **~150-180 MB** |

**Note**: MSIX packages appear smaller because they share Windows runtime. Portable apps include everything.

---

## Size Optimization Comparison

| Build Type | Size (x64) | Size (x86) | Use Case |
|------------|------------|------------|----------|
| Framework-Dependent | ~10 MB | ~10 MB | Users with .NET 8 installed |
| Standard Portable | ~165 MB | ~145 MB | General distribution |
| Optimized Portable | ~150 MB | ~130 MB | Slightly smaller |
| 7-Zip Compressed | ~90 MB | ~75 MB | File sharing (must extract) |

---

## Further Size Reduction Options

### 1. Use 32-bit (win-x86) - Saves ~15-20 MB
```powershell
.\build-portable.ps1 -Runtime win-x86 -UltraSize
```
- **Size**: ~120-140 MB
- **Pro**: Works on all Windows systems
- **Con**: None (unless you need 64-bit specific features)

### 2. Compress Assets - Saves ~2-4 MB
```powershell
pngquant --quality=70-85 Assets\*.png --ext .png --force
.\build-portable.ps1 -UltraSize
```

### 3. External 7-Zip Compression - Saves ~50-70 MB
```powershell
.\build-portable.ps1 -Runtime win-x86 -UltraSize
7z a -t7z -mx=9 Naraka.7z dist\Naraka-Cheat-Detector.exe
```
- **Result**: ~70-90 MB compressed
- **Con**: Users must extract before running

### 4. Framework-Dependent - Saves ~140-160 MB
```powershell
.\publish.ps1 -FrameworkDependent
```
- **Result**: ~10-15 MB
- **Con**: Users must install .NET 8 Desktop Runtime

---

## Requirements

**To Build:**
- .NET 8 SDK
- Windows 10+
- ~500 MB disk space (for build artifacts)

**To Run (Users):**
- Windows 10 1809+ only
- No .NET installation needed (for portable builds)
- ~200 MB disk space

---

## File Locations

| Type | Location |
|------|----------|
| Portable exe | `dist\Naraka-Cheat-Detector.exe` |
| Optimized exe | `dist\Naraka-Cheat-Detector.exe` (with -UltraSize flag) |
| Published exe | `artifacts\publish\Naraka-Cheat-Detector.exe` |
| Dev build | `bin\Release\net8.0-windows10.0.19041.0\win-x64\` |

---

## Troubleshooting

**Problem**: Build fails with "dotnet not found"  
**Solution**: Install .NET 8 SDK from microsoft.com/dotnet

**Problem**: Exe is too large (>200 MB)  
**Solution**: This is normal for WinUI portable apps. Use:
- win-x86 instead of win-x64 (~15 MB smaller)
- 7-Zip compression for distribution (~50% smaller)
- Framework-dependent if users have .NET 8 (~10 MB)

**Problem**: Build fails with trimming errors  
**Solution**: Use latest build script - trimming is now disabled (WinUI doesn't support it)

**Problem**: Exe won't run on target PC  
**Solution**: Ensure Windows 10 1809+ and try running as Administrator

**Problem**: Build takes a very long time  
**Solution**: First build is slower (~5 min), subsequent builds are faster (~1 min)

---

## Understanding the -UltraSize Flag

The `-UltraSize` flag applies **compatible** optimizations:

? **What it does:**
- IL code optimized for size (not speed)
- Debug symbols completely removed
- Debugger support disabled
- Unused runtime features removed
- System resource key compression

? **What it does NOT do:**
- Assembly trimming (breaks WinUI)
- Remove WinRT support (required)
- Aggressive globalization (breaks localization)

**Savings**: ~10-30 MB (5-15% reduction)  
**Trade-off**: Minimal (slightly slower IL execution)

---

## Realistic Size Goals

### What's Possible

| Goal | Achievable? | Method |
|------|-------------|--------|
| Under 200 MB | ? Yes | Standard build |
| Under 150 MB | ? Yes | win-x86 + UltraSize |
| Under 100 MB | ?? Maybe | 7-Zip compression (users extract) |
| Under 50 MB | ? No | Not possible for portable WinUI |

### What's NOT Possible

? Portable WinUI app under 100 MB (without external compression)  
? Assembly trimming with WinUI  
? Removing Windows App SDK runtime  
? Removing WinRT support  

---

## Recommended Build Command

**For most users:**
```powershell
.\build-portable.ps1 -Runtime win-x86 -UltraSize
```

This gives you:
- ~120-140 MB size (smallest portable option)
- Works on all Windows systems (32-bit and 64-bit)
- Full functionality preserved
- No user setup required

---

## For More Information

- See `SIZE-OPTIMIZATION.md` for detailed explanation of WinUI size limitations
- See `BUILD.md` for complete build documentation
- See `WEBHOOK_TROUBLESHOOTING.md` for deployment troubleshooting
