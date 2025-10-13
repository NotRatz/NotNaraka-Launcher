#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds a portable, self-contained single-file executable of Naraka Cheat Detector.

.DESCRIPTION
    This script creates a fully portable executable that includes:
    - The .NET 8 runtime (no installation required)
    - All dependencies bundled into a single .exe file
    - Native libraries extracted and embedded
    
    The resulting executable can run on any Windows 10+ machine without
    requiring .NET runtime installation.

.PARAMETER Runtime
    Target runtime identifier. Default: win-x64
    Options: win-x64, win-x86, win-arm64

.PARAMETER Configuration
    Build configuration. Default: Release

.PARAMETER Output
    Output directory. Default: dist

.PARAMETER UltraSize
    Enable additional size optimization (IL code optimization).
    Due to WinUI framework limitations, size reduction is limited to ~5-10%.

.EXAMPLE
    .\build-portable.ps1
    # Builds a portable x64 executable in the 'dist' folder

.EXAMPLE
    .\build-portable.ps1 -Runtime win-x86
    # Builds a portable x86 (32-bit) executable (15-20 MB smaller)

.EXAMPLE
    .\build-portable.ps1 -UltraSize
    # Builds with IL code size optimization

.EXAMPLE
    .\build-portable.ps1 -Configuration Debug -Output bin\debug-portable
    # Builds a debug version in a custom output folder
#>

param(
    [ValidateSet('win-x64', 'win-x86', 'win-arm64')]
    [string]$Runtime = "win-x64",
    
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = "Release",
    
    [string]$Output = "dist",
    
    [switch]$UltraSize
)

$ErrorActionPreference = "Stop"
$proj = "Naraka-Cheat-Detector.csproj"
$projFullPath = Join-Path $PSScriptRoot $proj

function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host "???????????????????????????????????????????" -ForegroundColor Cyan
    Write-Host " $Text" -ForegroundColor Cyan
    Write-Host "???????????????????????????????????????????" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Success {
    param([string]$Text)
    Write-Host "? $Text" -ForegroundColor Green
}

function Write-Info {
    param([string]$Text)
    Write-Host "? $Text" -ForegroundColor Yellow
}

function Write-Error-Custom {
    param([string]$Text)
    Write-Host "? $Text" -ForegroundColor Red
}

function Test-DotNetCLI {
    try {
        $version = dotnet --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "dotnet CLI found (version $version)"
            return $true
        }
    } catch {
        # Ignore
    }
    Write-Error-Custom "dotnet CLI not found"
    Write-Host ""
    Write-Host "Please install the .NET 8 SDK from:" -ForegroundColor Yellow
    Write-Host "https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Cyan
    return $false
}

# Main script
Write-Header "Naraka Cheat Detector - Portable Executable Builder"

Write-Info "Validating environment..."
if (-not (Test-DotNetCLI)) {
    exit 1
}

if (-not (Test-Path $projFullPath)) {
    Write-Error-Custom "Project file not found: $projFullPath"
    exit 1
}
Write-Success "Project file found"

Write-Host ""
Write-Info "Build Configuration:"
Write-Host "  Project:       $proj" -ForegroundColor Gray
Write-Host "  Configuration: $Configuration" -ForegroundColor Gray
Write-Host "  Runtime:       $Runtime" -ForegroundColor Gray
Write-Host "  Output:        $Output" -ForegroundColor Gray
if ($UltraSize) {
    Write-Host "  Optimization:  IL Code Size Optimization" -ForegroundColor Magenta
} else {
    Write-Host "  Optimization:  Standard" -ForegroundColor Gray
}
Write-Host ""

if ($Configuration -eq "Release") {
    Write-Host "WinUI Framework Limitations:" -ForegroundColor Yellow
    Write-Host "  • Assembly trimming: Not supported (breaks app)" -ForegroundColor DarkGray
    Write-Host "  • ReadyToRun: Disabled (causes assembly conflicts)" -ForegroundColor DarkGray
    Write-Host "  • Compression: Disabled (.NET 9 SDK bug causes bloat)" -ForegroundColor DarkGray
    Write-Host "  • Expected output size: ~200-230 MB (x64), ~180-210 MB (x86)" -ForegroundColor DarkGray
    Write-Host ""
}

# Clean output directory
if (Test-Path $Output) {
    Write-Info "Cleaning output directory..."
    Remove-Item -Path $Output -Recurse -Force -ErrorAction SilentlyContinue
}

# Build arguments for truly portable single-file executable
# Use full path to project file to avoid solution file conflicts
$publishArgs = @(
    "publish"
    $projFullPath
    "--configuration", $Configuration
    "--runtime", $Runtime
    "--output", $Output
    "--self-contained", "true"
    "/p:PublishSingleFile=true"
    "/p:IncludeNativeLibrariesForSelfExtract=true"
    "/p:IncludeAllContentForSelfExtract=true"
    "/p:EnableCompressionInSingleFile=false"
    "/p:DebugType=none"
    "/p:DebugSymbols=false"
    "/p:WindowsAppSDKSelfContained=true"
    "/p:PublishReadyToRun=false"
    "/p:PublishTrimmed=false"
    "/p:PlatformTarget="
    "/p:Platform=AnyCPU"
)

# Add IL optimization for ultra-size
if ($Configuration -eq "Release" -and $UltraSize) {
    Write-Info "Applying IL Code Size optimizations..."
    Write-Host "  • Optimizing IL code for size over speed" -ForegroundColor DarkGray
    Write-Host "  • Removing debugger support" -ForegroundColor DarkGray
    Write-Host "  • Disabling unused runtime features" -ForegroundColor DarkGray
    Write-Host ""
    
    $publishArgs += "/p:IlcOptimizationPreference=Size"
    $publishArgs += "/p:DebuggerSupport=false"
    $publishArgs += "/p:EnableUnsafeBinaryFormatterSerialization=false"
    $publishArgs += "/p:EnableUnsafeUTF7Encoding=false"
    $publishArgs += "/p:HttpActivityPropagationSupport=false"
    $publishArgs += "/p:MetadataUpdaterSupport=false"
    $publishArgs += "/p:UseSystemResourceKeys=true"
}

Write-Header "Building Portable Executable"

Write-Host "Executing: dotnet $($publishArgs -join ' ')" -ForegroundColor DarkGray
Write-Host ""

# Execute the publish command
& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error-Custom "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Header "Build Complete"

# Verify and report on output
$exeName = "Naraka-Cheat-Detector.exe"
$exePath = Join-Path $Output $exeName

if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    $sizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
    $sizeKB = [math]::Round($fileInfo.Length / 1KB, 0)
    
    Write-Success "Portable executable created successfully!"
    Write-Host ""
    Write-Host "  File:     $exePath" -ForegroundColor Cyan
    Write-Host "  Size:     $sizeMB MB ($sizeKB KB)" -ForegroundColor Cyan
    Write-Host "  Runtime:  $Runtime" -ForegroundColor Cyan
    if ($UltraSize) {
        Write-Host "  Mode:     IL Code Optimized" -ForegroundColor Magenta
    }
    Write-Host ""
    
    # Size guidance
    Write-Host "Size Information:" -ForegroundColor Yellow
    Write-Host "  • WinUI apps require full Windows App SDK runtime" -ForegroundColor Gray
    Write-Host "  • 200-230 MB is normal for portable WinUI 3 apps" -ForegroundColor Gray
    if ($Runtime -eq "win-x86") {
        Write-Host "  • win-x86 builds are ~15-20 MB smaller than x64" -ForegroundColor Gray
    } else {
        Write-Host "  • Use win-x86 for ~15-20 MB smaller builds" -ForegroundColor Gray
    }
    Write-Host "  • Compression disabled (causes 2.5x size increase bug)" -ForegroundColor Gray
    Write-Host "  • See SIZE-OPTIMIZATION.md for details" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "Deployment Instructions:" -ForegroundColor Yellow
    Write-Host "  1. Copy the executable to any Windows 10+ machine" -ForegroundColor Gray
    Write-Host "  2. Run directly - no installation needed!" -ForegroundColor Gray
    Write-Host "  3. The .NET runtime is included in the executable" -ForegroundColor Gray
    Write-Host ""
    
    # Check for additional files
    $allFiles = Get-ChildItem -Path $Output -File
    if ($allFiles.Count -gt 1) {
        Write-Host "Additional files in output:" -ForegroundColor Yellow
        $allFiles | Where-Object { $_.Name -ne $exeName } | ForEach-Object {
            Write-Host "  - $($_.Name)" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
} else {
    Write-Error-Custom "Executable not found at expected location: $exePath"
    Write-Host ""
    Write-Host "Output directory contents:" -ForegroundColor Yellow
    Get-ChildItem -Path $Output | ForEach-Object {
        Write-Host "  $($_.Name)" -ForegroundColor Gray
    }
    exit 1
}

Write-Host ""
Write-Host "???????????????????????????????????????????" -ForegroundColor Green
Write-Host " Build Successful! " -ForegroundColor Green
Write-Host "???????????????????????????????????????????" -ForegroundColor Green
Write-Host ""
