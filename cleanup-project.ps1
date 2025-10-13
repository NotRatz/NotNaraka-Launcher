# ============================================
#  NarakaTweaks Project Cleanup Script
# ============================================
# Removes unnecessary files to reduce package size

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " NarakaTweaks Project Cleanup" -ForegroundColor Cyan
Write-Host "============================================`n" -ForegroundColor Cyan

$projectRoot = "c:\Users\Admin\Desktop\NotNaraka Launcher"
Set-Location $projectRoot

# Calculate initial size
Write-Host "[1/7] Calculating current project size..." -ForegroundColor Yellow
$initialSize = (Get-ChildItem -Recurse -File | Measure-Object -Property Length -Sum).Sum
Write-Host "Current project size: $([math]::Round($initialSize/1MB,2)) MB`n" -ForegroundColor White

# 1. Remove bin directories
Write-Host "[2/7] Removing bin directories..." -ForegroundColor Yellow
$binDirs = Get-ChildItem -Directory -Recurse -Include bin
$binCount = $binDirs.Count
$binSize = ($binDirs | Get-ChildItem -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
foreach ($dir in $binDirs) {
    Remove-Item $dir.FullName -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "✓ Removed $binCount bin directories ($([math]::Round($binSize/1MB,2)) MB)`n" -ForegroundColor Green

# 2. Remove obj directories
Write-Host "[3/7] Removing obj directories..." -ForegroundColor Yellow
$objDirs = Get-ChildItem -Directory -Recurse -Include obj
$objCount = $objDirs.Count
$objSize = ($objDirs | Get-ChildItem -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
foreach ($dir in $objDirs) {
    Remove-Item $dir.FullName -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "✓ Removed $objCount obj directories ($([math]::Round($objSize/1MB,2)) MB)`n" -ForegroundColor Green

# 3. Remove backup files
Write-Host "[4/7] Removing backup and temporary files..." -ForegroundColor Yellow
$backupFiles = Get-ChildItem -Recurse -Include *.bak,*.tmp,*.old,*.backup,*_WinUI3*,*.Backup.tmp -File
$backupCount = $backupFiles.Count
$backupSize = ($backupFiles | Measure-Object -Property Length -Sum).Sum
foreach ($file in $backupFiles) {
    Remove-Item $file.FullName -Force -ErrorAction SilentlyContinue
}
Write-Host "✓ Removed $backupCount backup files ($([math]::Round($backupSize/1MB,2)) MB)`n" -ForegroundColor Green

# 4. Remove old publish artifacts (keep only latest)
Write-Host "[5/7] Cleaning old publish artifacts..." -ForegroundColor Yellow
$oldPublishFiles = @(
    "$projectRoot\publish\NarakaTweaks.Launcher.exe"  # Old WinUI3 version
)
$oldPublishSize = 0
foreach ($file in $oldPublishFiles) {
    if (Test-Path $file) {
        $oldPublishSize += (Get-Item $file).Length
        Remove-Item $file -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed: $(Split-Path $file -Leaf)" -ForegroundColor Gray
    }
}
Write-Host "✓ Removed old publish files ($([math]::Round($oldPublishSize/1MB,2)) MB)`n" -ForegroundColor Green

# 5. Remove documentation media files (optional - comment out if you want to keep)
Write-Host "[6/7] Removing large documentation files..." -ForegroundColor Yellow
$docFiles = @(
    "$projectRoot\RatzTweaks_Explained.mp4",
    "$projectRoot\case1.png",
    "$projectRoot\ratznaked.jpg"
)
$docSize = 0
$docCount = 0
foreach ($file in $docFiles) {
    if (Test-Path $file) {
        $docSize += (Get-Item $file).Length
        Remove-Item $file -Force -ErrorAction SilentlyContinue
        $docCount++
        Write-Host "  Removed: $(Split-Path $file -Leaf)" -ForegroundColor Gray
    }
}
Write-Host "✓ Removed $docCount documentation files ($([math]::Round($docSize/1MB,2)) MB)`n" -ForegroundColor Green

# 6. Remove NuGet cache files
Write-Host "[7/7] Removing NuGet cache files..." -ForegroundColor Yellow
$nugetFiles = Get-ChildItem -Recurse -Include *.nuget.dgspec.json,*.nuget.g.props,*.nuget.g.targets,project.assets.json,project.nuget.cache -File
$nugetCount = $nugetFiles.Count
$nugetSize = ($nugetFiles | Measure-Object -Property Length -Sum).Sum
foreach ($file in $nugetFiles) {
    Remove-Item $file.FullName -Force -ErrorAction SilentlyContinue
}
Write-Host "✓ Removed $nugetCount NuGet cache files ($([math]::Round($nugetSize/1MB,2)) MB)`n" -ForegroundColor Green

# Calculate final size
Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Cleanup Complete!" -ForegroundColor Cyan
Write-Host "============================================`n" -ForegroundColor Cyan

$finalSize = (Get-ChildItem -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
$savedSize = $initialSize - $finalSize

Write-Host "Initial size:  $([math]::Round($initialSize/1MB,2)) MB" -ForegroundColor White
Write-Host "Final size:    $([math]::Round($finalSize/1MB,2)) MB" -ForegroundColor White
Write-Host "Space saved:   $([math]::Round($savedSize/1MB,2)) MB ($([math]::Round(($savedSize/$initialSize)*100,1))%)" -ForegroundColor Green

Write-Host "`nProject is now clean and ready for distribution! 🎉" -ForegroundColor Cyan
