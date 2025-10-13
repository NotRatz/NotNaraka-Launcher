# NarakaTweaks Launcher - Quick Build Script
# Run this script to build the entire project

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host " NarakaTweaks Launcher Builder" -ForegroundColor Yellow
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Check if .NET 8 SDK is installed
Write-Host "[1/6] Checking .NET 8 SDK..." -ForegroundColor Green
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: .NET 8 SDK not found!" -ForegroundColor Red
    Write-Host "Please install from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}
Write-Host "✓ Found .NET SDK version: $dotnetVersion" -ForegroundColor Green
Write-Host ""

# Navigate to project root
$projectRoot = $PSScriptRoot
Set-Location $projectRoot

# Restore dependencies for each project
Write-Host "[2/6] Restoring NuGet packages..." -ForegroundColor Green

Write-Host "  Restoring AntiCheat..." -ForegroundColor Gray
dotnet restore "NarakaTweaks.AntiCheat\NarakaTweaks.AntiCheat.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to restore AntiCheat packages" -ForegroundColor Red
    exit 1
}

Write-Host "  Restoring Core..." -ForegroundColor Gray
dotnet restore "NarakaTweaks.Core\NarakaTweaks.Core.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to restore Core packages" -ForegroundColor Red
    exit 1
}

Write-Host "  Restoring Launcher.Shared..." -ForegroundColor Gray
if (Test-Path "Launcher.Shared\Launcher.Shared.csproj") {
    dotnet restore "Launcher.Shared\Launcher.Shared.csproj"
}

Write-Host "  Restoring Launcher..." -ForegroundColor Gray
dotnet restore "NarakaTweaks.Launcher\NarakaTweaks.Launcher.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to restore Launcher packages" -ForegroundColor Red
    exit 1
}

Write-Host "✓ All packages restored" -ForegroundColor Green
Write-Host ""

# Build AntiCheat project
Write-Host "[3/6] Building NarakaTweaks.AntiCheat..." -ForegroundColor Green
dotnet build "NarakaTweaks.AntiCheat\NarakaTweaks.AntiCheat.csproj" --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to build AntiCheat project" -ForegroundColor Red
    exit 1
}
Write-Host "✓ AntiCheat built successfully" -ForegroundColor Green
Write-Host ""

# Build Core project
Write-Host "[4/6] Building NarakaTweaks.Core..." -ForegroundColor Green
dotnet build "NarakaTweaks.Core\NarakaTweaks.Core.csproj" --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to build Core project" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Core built successfully" -ForegroundColor Green
Write-Host ""

# Build Launcher project
Write-Host "[5/6] Building NarakaTweaks.Launcher..." -ForegroundColor Green
dotnet build "NarakaTweaks.Launcher\NarakaTweaks.Launcher.csproj" --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to build Launcher project" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Launcher built successfully" -ForegroundColor Green
Write-Host ""

# Summary
Write-Host "[6/6] Build Summary" -ForegroundColor Green
Write-Host "==================" -ForegroundColor Cyan
Write-Host "✓ All projects built successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Output locations:" -ForegroundColor Yellow
Write-Host "  - AntiCheat: NarakaTweaks.AntiCheat\bin\Release\" -ForegroundColor Gray
Write-Host "  - Core:      NarakaTweaks.Core\bin\Release\" -ForegroundColor Gray
Write-Host "  - Launcher:  NarakaTweaks.Launcher\bin\Release\" -ForegroundColor Gray
Write-Host ""

# Ask if user wants to run
$response = Read-Host "Would you like to run the launcher now? (y/n)"
if ($response -eq 'y' -or $response -eq 'Y') {
    Write-Host ""
    Write-Host "Starting NarakaTweaks Launcher..." -ForegroundColor Yellow
    Set-Location "NarakaTweaks.Launcher"
    dotnet run --configuration Release --no-build
} else {
    Write-Host ""
    Write-Host "To run the launcher manually:" -ForegroundColor Yellow
    Write-Host "  cd NarakaTweaks.Launcher" -ForegroundColor Gray
    Write-Host "  dotnet run" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Or publish a single-file executable:" -ForegroundColor Yellow
    Write-Host "  .\publish-launcher.ps1" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Build complete! 🎉" -ForegroundColor Green
Write-Host ""
