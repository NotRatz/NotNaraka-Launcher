# ============================================
#  Final Cleanup - Remove Obsolete Projects
# ============================================

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Removing Obsolete Projects" -ForegroundColor Cyan
Write-Host "============================================`n" -ForegroundColor Cyan

$projectRoot = "c:\Users\Admin\Desktop\NotNaraka Launcher"
Set-Location $projectRoot

$sizeBefore = (Get-ChildItem -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum

# Obsolete directories to remove
$obsoleteDirs = @(
    "NarakaLauncher",       # Old launcher project (replaced by NarakaTweaks.Launcher)
    "bin",                  # Root bin directory
    "obj",                  # Root obj directory
    "UTILITY"               # Utility scripts no longer needed
)

$removedSize = 0
$removedCount = 0

foreach ($dir in $obsoleteDirs) {
    $fullPath = Join-Path $projectRoot $dir
    if (Test-Path $fullPath) {
        $dirSize = (Get-ChildItem $fullPath -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
        Remove-Item $fullPath -Recurse -Force -ErrorAction SilentlyContinue
        $removedSize += $dirSize
        $removedCount++
        Write-Host "✓ Removed: $dir ($([math]::Round($dirSize/1MB,2)) MB)" -ForegroundColor Green
    }
}

# Remove old project files from root
$oldProjectFiles = @(
    "Naraka-Cheat-Detector.csproj*",
    "Package.appxmanifest",
    "boot.config",
    "CheatDetectorGUI.cs",
    "BoolToBrushConverter.cs",
    "MainWindow.xaml*",
    "App.xaml*",
    "app.manifest",
    "*.user",
    "global.json",
    "discord_*.json",
    "discord_*.secret"
)

Write-Host "`nRemoving obsolete root files..." -ForegroundColor Yellow
$fileCount = 0
foreach ($pattern in $oldProjectFiles) {
    $files = Get-ChildItem -Path $projectRoot -Filter $pattern -File -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        $removedSize += $file.Length
        Remove-Item $file.FullName -Force -ErrorAction SilentlyContinue
        $fileCount++
        Write-Host "  ✓ Removed: $($file.Name)" -ForegroundColor Gray
    }
}

Write-Host "`n✓ Removed $fileCount obsolete files" -ForegroundColor Green

$sizeAfter = (Get-ChildItem -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
$totalSaved = $sizeBefore - $sizeAfter

Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host " Cleanup Summary" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Directories removed: $removedCount" -ForegroundColor White
Write-Host "Files removed:       $fileCount" -ForegroundColor White
Write-Host "Space saved:         $([math]::Round($totalSaved/1MB,2)) MB" -ForegroundColor Green
Write-Host "Final project size:  $([math]::Round($sizeAfter/1MB,2)) MB" -ForegroundColor White
Write-Host "`nProject is fully optimized! 🎉" -ForegroundColor Cyan
