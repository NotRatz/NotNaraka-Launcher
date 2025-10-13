# NarakaTweaks Launcher - Publish Script
# Creates a single-file executable for distribution

Write-Host ""
Write-Host "====================================" -ForegroundColor Cyan
Write-Host " NarakaTweaks Launcher Publisher" -ForegroundColor Yellow
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

$projectRoot = $PSScriptRoot
$publishDir = Join-Path $projectRoot "publish"

# Create publish directory
if (-not (Test-Path $publishDir)) {
    New-Item -Path $publishDir -ItemType Directory | Out-Null
}

# Ask for architecture
Write-Host "Select target architecture:" -ForegroundColor Yellow
Write-Host "  1. x64 (64-bit Intel/AMD)" -ForegroundColor Gray
Write-Host "  2. ARM64 (Surface, etc.)" -ForegroundColor Gray
Write-Host "  3. Both" -ForegroundColor Gray
$choice = Read-Host "Enter choice (1-3)"

$architectures = @()
switch ($choice) {
    "1" { $architectures = @("win-x64") }
    "2" { $architectures = @("win-arm64") }
    "3" { $architectures = @("win-x64", "win-arm64") }
    default { 
        Write-Host "Invalid choice. Defaulting to x64." -ForegroundColor Yellow
        $architectures = @("win-x64")
    }
}

Write-Host ""

foreach ($arch in $architectures) {
    $outputDir = Join-Path $publishDir $arch
    
    Write-Host "Publishing for $arch..." -ForegroundColor Green
    Write-Host "Output: $outputDir" -ForegroundColor Gray
    
    dotnet publish "NarakaTweaks.Launcher\NarakaTweaks.Launcher.csproj" `
        --configuration Release `
        --runtime $arch `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:WindowsAppSDKSelfContained=true `
        -p:DebugType=none `
        -p:DebugSymbols=false `
        --output $outputDir
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to publish for $arch" -ForegroundColor Red
        exit 1
    }
    
    # Get file size
    $exePath = Join-Path $outputDir "NarakaTweaks.Launcher.exe"
    if (Test-Path $exePath) {
        $fileSize = (Get-Item $exePath).Length / 1MB
        Write-Host "✓ Published successfully! Size: $($fileSize.ToString('F2')) MB" -ForegroundColor Green
    }
    
    Write-Host ""
}

# Create portable ZIP packages
Write-Host "Creating portable ZIP packages..." -ForegroundColor Green
foreach ($arch in $architectures) {
    $sourceDir = Join-Path $publishDir $arch
    $zipPath = Join-Path $publishDir "NarakaTweaks-Portable-$arch.zip"
    
    if (Test-Path $sourceDir) {
        Compress-Archive -Path "$sourceDir\*" -DestinationPath $zipPath -Force
        $zipSize = (Get-Item $zipPath).Length / 1MB
        Write-Host "✓ Created $zipPath ($($zipSize.ToString('F2')) MB)" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host " Publishing Complete! 🎉" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output directory: $publishDir" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Test the executable in: $publishDir\win-x64\" -ForegroundColor Gray
Write-Host "  2. Distribute the ZIP file: NarakaTweaks-Portable-*.zip" -ForegroundColor Gray
Write-Host "  3. (Optional) Code sign the executable" -ForegroundColor Gray
Write-Host ""

# Ask if user wants to open the folder
$response = Read-Host "Open publish folder? (y/n)"
if ($response -eq 'y' -or $response -eq 'Y') {
    Start-Process explorer.exe $publishDir
}
