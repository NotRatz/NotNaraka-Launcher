#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds a portable, self-contained multi-file distribution of Naraka Cheat Detector.

.DESCRIPTION
    This script creates a fully portable application folder that includes:
    - The .NET 8 runtime (no installation required)
    - All dependencies in a folder structure
    - Native libraries
    
    The resulting folder can be zipped and distributed. Users extract and run the .exe.

.PARAMETER Runtime
    Target runtime identifier. Default: win-x64
    Options: win-x64, win-x86, win-arm64

.PARAMETER Configuration
    Build configuration. Default: Release

.PARAMETER Output
    Output directory. Default: dist

.EXAMPLE
    .\build-multifile.ps1
    # Builds a portable x64 distribution in the 'dist' folder (~211 MB)
#>

param(
    [ValidateSet('win-x64', 'win-x86', 'win-arm64')]
    [string]$Runtime = "win-x64",
    
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = "Release",
    
    [string]$Output = "dist"
)

$ErrorActionPreference = "Stop"
$proj = "Naraka-Cheat-Detector.csproj"
$projFullPath = Join-Path $PSScriptRoot $proj

Write-Host ""
Write-Host "???????????????????????????????????????????" -ForegroundColor Cyan
Write-Host " Naraka Cheat Detector - Multi-File Build" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Clean output directory
if (Test-Path $Output) {
    Write-Host "? Cleaning output directory..." -ForegroundColor Yellow
    Remove-Item -Path $Output -Recurse -Force -ErrorAction SilentlyContinue
}

# Build arguments
$publishArgs = @(
    "publish"
    $projFullPath
    "--configuration", $Configuration
    "--runtime", $Runtime
    "--output", $Output
    "--self-contained", "true"
    "/p:PublishSingleFile=false"
    "/p:WindowsAppSDKSelfContained=true"
    "/p:PlatformTarget="
    "/p:Platform=AnyCPU"
    "/p:DebugType=none"
    "/p:DebugSymbols=false"
)

Write-Host "Building..." -ForegroundColor Cyan
& dotnet @publishArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "? Build successful!" -ForegroundColor Green
    Write-Host ""
    
    $totalSize = (Get-ChildItem $Output -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
    $fileCount = (Get-ChildItem $Output -Recurse -File | Measure-Object).Count
    
    Write-Host "  Output:      $Output" -ForegroundColor Cyan
    Write-Host "  Total Size:  $([math]::Round($totalSize, 2)) MB" -ForegroundColor Cyan
    Write-Host "  Files:       $fileCount files" -ForegroundColor Cyan
    Write-Host "  Runtime:     $Runtime" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Deployment:" -ForegroundColor Yellow
    Write-Host "  1. Zip the '$Output' folder" -ForegroundColor Gray
    Write-Host "  2. Users extract the zip" -ForegroundColor Gray
    Write-Host "  3. Users run Naraka-Cheat-Detector.exe" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "? Build failed" -ForegroundColor Red
    exit $LASTEXITCODE
}
