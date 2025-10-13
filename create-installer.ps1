# NotNaraka Launcher - Installer Creator
# This script packages the launcher into an easy-to-distribute installer

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  NotNaraka Launcher - Installer Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$ProjectRoot = $PSScriptRoot
$BuildPath = Join-Path $ProjectRoot "NarakaTweaks.Launcher\bin\Release\net8.0-windows\win-x64"
$InstallerOutputPath = Join-Path $ProjectRoot "Installer"
$InstallerZipPath = Join-Path $ProjectRoot "NotNaraka-Launcher-Installer.zip"

# Check if build exists
if (-not (Test-Path $BuildPath)) {
    Write-Host "❌ Build not found! Please build the project first." -ForegroundColor Red
    Write-Host "   Run: dotnet build NarakaTweaks.Launcher\NarakaTweaks.Launcher.csproj --configuration Release" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Build found at: $BuildPath" -ForegroundColor Green
Write-Host ""

# Create installer directory
Write-Host "📁 Creating installer package..." -ForegroundColor Cyan
if (Test-Path $InstallerOutputPath) {
    Remove-Item $InstallerOutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $InstallerOutputPath | Out-Null

# Copy all files
Write-Host "📦 Copying launcher files..." -ForegroundColor Cyan
Copy-Item -Path "$BuildPath\*" -Destination $InstallerOutputPath -Recurse -Force

# Copy assets if they exist
$AssetsPath = Join-Path $ProjectRoot "Assets"
if (Test-Path $AssetsPath) {
    Write-Host "🎨 Copying assets..." -ForegroundColor Cyan
    $InstallerAssetsPath = Join-Path $InstallerOutputPath "Assets"
    New-Item -ItemType Directory -Path $InstallerAssetsPath -Force | Out-Null
    Copy-Item -Path "$AssetsPath\*" -Destination $InstallerAssetsPath -Recurse -Force
}

# Create installer script
Write-Host "📝 Creating installer script..." -ForegroundColor Cyan
$InstallerScript = @'
# NotNaraka Launcher - Simple Installer
# Run this script to install the launcher

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  NotNaraka Launcher - Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check for .NET 8.0 Runtime
Write-Host "🔍 Checking for .NET 8.0 Runtime..." -ForegroundColor Cyan
$dotnetVersion = $null
try {
    $dotnetVersionOutput = dotnet --list-runtimes 2>$null
    if ($dotnetVersionOutput -match "Microsoft\.WindowsDesktop\.App 8\.") {
        Write-Host "✅ .NET 8.0 Runtime found!" -ForegroundColor Green
        $dotnetVersion = "8.0"
    }
} catch {
    # dotnet command not found
}

if (-not $dotnetVersion) {
    Write-Host "⚠️  .NET 8.0 Runtime not found!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "The launcher requires .NET 8.0 Desktop Runtime to run." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Would you like to:" -ForegroundColor Cyan
    Write-Host "  1. Download .NET 8.0 Runtime (opens browser)" -ForegroundColor White
    Write-Host "  2. Continue installation anyway (launcher won't run)" -ForegroundColor White
    Write-Host "  3. Cancel installation" -ForegroundColor White
    Write-Host ""
    $choice = Read-Host "Enter choice (1-3)"
    
    switch ($choice) {
        "1" {
            Write-Host "🌐 Opening .NET download page..." -ForegroundColor Cyan
            Start-Process "https://dotnet.microsoft.com/download/dotnet/8.0/runtime"
            Write-Host ""
            Write-Host "Please install .NET 8.0 Desktop Runtime, then run this installer again." -ForegroundColor Yellow
            Write-Host "Press any key to exit..."
            $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
            exit 0
        }
        "2" {
            Write-Host "⚠️  Continuing without .NET Runtime..." -ForegroundColor Yellow
        }
        "3" {
            Write-Host "❌ Installation cancelled." -ForegroundColor Red
            exit 0
        }
        default {
            Write-Host "❌ Invalid choice. Installation cancelled." -ForegroundColor Red
            exit 0
        }
    }
}

Write-Host ""

# Choose installation location
$DefaultPath = Join-Path $env:LOCALAPPDATA "NotNaraka Launcher"
Write-Host "📁 Choose installation location:" -ForegroundColor Cyan
Write-Host "   Default: $DefaultPath" -ForegroundColor Gray
Write-Host ""
Write-Host "Press ENTER for default, or type custom path:" -ForegroundColor Cyan
$CustomPath = Read-Host

if ([string]::IsNullOrWhiteSpace($CustomPath)) {
    $InstallPath = $DefaultPath
} else {
    $InstallPath = $CustomPath
}

Write-Host ""
Write-Host "📂 Installing to: $InstallPath" -ForegroundColor Green

# Create installation directory
if (Test-Path $InstallPath) {
    Write-Host "⚠️  Installation directory already exists." -ForegroundColor Yellow
    $overwrite = Read-Host "Overwrite existing installation? (Y/N)"
    if ($overwrite -ne "Y" -and $overwrite -ne "y") {
        Write-Host "❌ Installation cancelled." -ForegroundColor Red
        exit 0
    }
    Remove-Item $InstallPath -Recurse -Force
}

New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

# Copy files
Write-Host "📦 Copying files..." -ForegroundColor Cyan
$SourcePath = $PSScriptRoot
Copy-Item -Path "$SourcePath\*" -Destination $InstallPath -Recurse -Force -Exclude "install.ps1"

Write-Host "✅ Files copied successfully!" -ForegroundColor Green
Write-Host ""

# Create desktop shortcut
Write-Host "🔗 Create desktop shortcut?" -ForegroundColor Cyan
$createShortcut = Read-Host "Create shortcut? (Y/N)"

if ($createShortcut -eq "Y" -or $createShortcut -eq "y") {
    $WScriptShell = New-Object -ComObject WScript.Shell
    $DesktopPath = [System.Environment]::GetFolderPath('Desktop')
    $ShortcutPath = Join-Path $DesktopPath "NotNaraka Launcher.lnk"
    $Shortcut = $WScriptShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = Join-Path $InstallPath "NarakaTweaks.Launcher.exe"
    $Shortcut.WorkingDirectory = $InstallPath
    $Shortcut.Description = "NotNaraka Launcher - Launch and optimize Naraka: Bladepoint"
    $Shortcut.Save()
    Write-Host "✅ Desktop shortcut created!" -ForegroundColor Green
}

Write-Host ""

# Create Start Menu shortcut
Write-Host "🔗 Create Start Menu shortcut?" -ForegroundColor Cyan
$createStartMenu = Read-Host "Create Start Menu shortcut? (Y/N)"

if ($createStartMenu -eq "Y" -or $createStartMenu -eq "y") {
    $StartMenuPath = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
    $StartMenuShortcut = Join-Path $StartMenuPath "NotNaraka Launcher.lnk"
    $WScriptShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WScriptShell.CreateShortcut($StartMenuShortcut)
    $Shortcut.TargetPath = Join-Path $InstallPath "NarakaTweaks.Launcher.exe"
    $Shortcut.WorkingDirectory = $InstallPath
    $Shortcut.Description = "NotNaraka Launcher - Launch and optimize Naraka: Bladepoint"
    $Shortcut.Save()
    Write-Host "✅ Start Menu shortcut created!" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  ✅ Installation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Launcher installed to: $InstallPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "🚀 Launch now?" -ForegroundColor Cyan
$launch = Read-Host "Launch NotNaraka Launcher? (Y/N)"

if ($launch -eq "Y" -or $launch -eq "y") {
    Write-Host "🎮 Starting launcher..." -ForegroundColor Green
    Start-Process (Join-Path $InstallPath "NarakaTweaks.Launcher.exe")
}

Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
'@

$InstallerScriptPath = Join-Path $InstallerOutputPath "install.ps1"
$InstallerScript | Out-File -FilePath $InstallerScriptPath -Encoding UTF8

# Create README
Write-Host "📄 Creating README..." -ForegroundColor Cyan
$nl = "`r`n"
$ReadmeContent = "# NotNaraka Launcher - Installation Guide$nl$nl"
$ReadmeContent += "## Quick Install$nl$nl"
$ReadmeContent += "1. Right-click on install.ps1$nl"
$ReadmeContent += "2. Select 'Run with PowerShell'$nl"
$ReadmeContent += "3. Follow the on-screen instructions$nl$nl"
$ReadmeContent += "## Manual Install$nl$nl"
$ReadmeContent += "If the installer does not work:$nl$nl"
$ReadmeContent += "1. Copy all files to a folder of your choice$nl"
$ReadmeContent += "2. Double-click NarakaTweaks.Launcher.exe to run$nl$nl"
$ReadmeContent += "## Requirements$nl$nl"
$ReadmeContent += "- Windows 10 or 11 64-bit$nl"
$ReadmeContent += "- .NET 8.0 Desktop Runtime$nl"
$ReadmeContent += "  The installer will prompt you to download if needed$nl"
$ReadmeContent += "  Download: https://dotnet.microsoft.com/download/dotnet/8.0/runtime$nl$nl"
$ReadmeContent += "## Features$nl$nl"
$ReadmeContent += "Main Features:$nl"
$ReadmeContent += "- Quick launch Naraka: Bladepoint$nl"
$ReadmeContent += "- Download CN and Global clients automatically$nl"
$ReadmeContent += "- System performance tweaks$nl"
$ReadmeContent += "- Steam news feed$nl"
$ReadmeContent += "- YouTube promotional content$nl"
$ReadmeContent += "- Real-time download logging$nl$nl"
$ReadmeContent += "Window Management:$nl"
$ReadmeContent += "- Main launcher: 800x600 (locked size)$nl"
$ReadmeContent += "- Pop-out windows for Tweaks, Clients, Settings$nl"
$ReadmeContent += "- Side-by-side layout for easy monitoring$nl$nl"
$ReadmeContent += "## Troubleshooting$nl$nl"
$ReadmeContent += "Windows protected your PC warning:$nl"
$ReadmeContent += "This is normal for unsigned applications.$nl"
$ReadmeContent += "Click 'More info' then 'Run anyway'$nl$nl"
$ReadmeContent += ".NET Runtime not found:$nl"
$ReadmeContent += "Download and install .NET 8.0 Desktop Runtime from:$nl"
$ReadmeContent += "https://dotnet.microsoft.com/download/dotnet/8.0/runtime$nl$nl"
$ReadmeContent += "Launcher will not start:$nl"
$ReadmeContent += "1. Make sure .NET 8.0 Runtime is installed$nl"
$ReadmeContent += "2. Try running as Administrator$nl"
$ReadmeContent += "3. Check Windows Defender is not blocking it$nl$nl"
$ReadmeContent += "## Version$nl$nl"
$ReadmeContent += "Version: 1.0.0$nl"
$ReadmeContent += "Build Date: October 13, 2025$nl"
$ReadmeContent += "Platform: Windows x64$nl$nl"
$ReadmeContent += "Enjoy the launcher!$nl"

$ReadmePath = Join-Path $InstallerOutputPath "README.txt"
$ReadmeContent | Out-File -FilePath $ReadmePath -Encoding UTF8

Write-Host "✅ README created!" -ForegroundColor Green
Write-Host ""

# Create ZIP archive
Write-Host "🗜️  Creating ZIP archive..." -ForegroundColor Cyan
if (Test-Path $InstallerZipPath) {
    Remove-Item $InstallerZipPath -Force
}

Compress-Archive -Path "$InstallerOutputPath\*" -DestinationPath $InstallerZipPath -CompressionLevel Optimal

Write-Host "✅ ZIP archive created!" -ForegroundColor Green
Write-Host ""

# Calculate size
$ZipSize = (Get-Item $InstallerZipPath).Length / 1MB
$FolderSize = (Get-ChildItem $InstallerOutputPath -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB

Write-Host "========================================" -ForegroundColor Green
Write-Host "  ✅ Installer Package Created!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "📦 Package Details:" -ForegroundColor Cyan
Write-Host "   ZIP File: $InstallerZipPath" -ForegroundColor White
Write-Host "   ZIP Size: $([math]::Round($ZipSize, 2)) MB" -ForegroundColor White
Write-Host "   Extracted Size: $([math]::Round($FolderSize, 2)) MB" -ForegroundColor White
Write-Host ""
Write-Host "📁 Installer Folder:" -ForegroundColor Cyan
Write-Host "   $InstallerOutputPath" -ForegroundColor White
Write-Host ""
Write-Host "🚀 Distribution Options:" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Option 1: Share ZIP File (Recommended)" -ForegroundColor Yellow
Write-Host "    1. Send 'NotNaraka-Launcher-Installer.zip' to your friend" -ForegroundColor White
Write-Host "    2. They extract it" -ForegroundColor White
Write-Host "    3. They run 'install.ps1'" -ForegroundColor White
Write-Host ""
Write-Host "  Option 2: Share Installer Folder" -ForegroundColor Yellow
Write-Host "    1. Share the entire 'Installer' folder" -ForegroundColor White
Write-Host "    2. They run 'install.ps1'" -ForegroundColor White
Write-Host ""
Write-Host "  Option 3: Direct Run (No Install)" -ForegroundColor Yellow
Write-Host "    1. Extract ZIP" -ForegroundColor White
Write-Host "    2. Run 'NarakaTweaks.Launcher.exe' directly" -ForegroundColor White
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "📂 Open installer folder?" -ForegroundColor Cyan
$openFolder = Read-Host "Open folder? (Y/N)"

if ($openFolder -eq "Y" -or $openFolder -eq "y") {
    explorer $ProjectRoot
}

Write-Host ""
Write-Host "✅ Done! Ready to share with your friend!" -ForegroundColor Green
Write-Host ""
