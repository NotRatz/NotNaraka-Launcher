# NotNaraka Launcher - Setup EXE Creator
# Creates a Windows installer using IExpress

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  NotNaraka Launcher - Setup EXE Creator" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ProjectRoot = $PSScriptRoot
$BuildPath = Join-Path $ProjectRoot "NarakaTweaks.Launcher\bin\Release\net8.0-windows\win-x64"
$SetupOutputPath = Join-Path $ProjectRoot "Setup"
$TempPath = Join-Path $SetupOutputPath "temp"

# Check if build exists
if (-not (Test-Path $BuildPath)) {
    Write-Host "❌ Build not found! Please build the project first." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build found!" -ForegroundColor Green
Write-Host ""

# Create setup directory
Write-Host "📁 Creating setup package..." -ForegroundColor Cyan
if (Test-Path $SetupOutputPath) {
    Remove-Item $SetupOutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $SetupOutputPath | Out-Null
New-Item -ItemType Directory -Path $TempPath | Out-Null

# Copy files to temp
Write-Host "📦 Copying files..." -ForegroundColor Cyan
Copy-Item -Path "$BuildPath\*" -Destination $TempPath -Recurse -Force

# Create a simple batch installer
$BatchInstaller = @'
@echo off
echo ========================================
echo   NotNaraka Launcher - Installer
echo ========================================
echo.

REM Check for .NET 8.0 Runtime
echo Checking for .NET Runtime...
dotnet --list-runtimes 2>nul | findstr /C:"Microsoft.WindowsDesktop.App 8." >nul
if %errorlevel% neq 0 (
    echo.
    echo WARNING: .NET 8.0 Runtime not found!
    echo The launcher requires .NET 8.0 Desktop Runtime to run.
    echo.
    echo Would you like to open the download page?
    choice /C YN /M "Open .NET download page"
    if errorlevel 2 goto skip_download
    start https://dotnet.microsoft.com/download/dotnet/8.0/runtime
    echo.
    echo Please install .NET 8.0 Desktop Runtime, then run this installer again.
    pause
    exit /b 1
)
:skip_download

echo .NET Runtime found!
echo.

REM Set default installation path
set "DEFAULT_PATH=%LOCALAPPDATA%\NotNaraka Launcher"
echo Default installation path: %DEFAULT_PATH%
echo.
echo Press ENTER for default, or type custom path:
set /p CUSTOM_PATH=

if "%CUSTOM_PATH%"=="" (
    set "INSTALL_PATH=%DEFAULT_PATH%"
) else (
    set "INSTALL_PATH=%CUSTOM_PATH%"
)

echo.
echo Installing to: %INSTALL_PATH%
echo.

REM Create installation directory
if exist "%INSTALL_PATH%" (
    echo WARNING: Installation directory already exists!
    choice /C YN /M "Overwrite existing installation"
    if errorlevel 2 (
        echo Installation cancelled.
        pause
        exit /b 1
    )
    rmdir /s /q "%INSTALL_PATH%"
)

mkdir "%INSTALL_PATH%"

REM Copy files
echo Copying files...
xcopy /E /I /Y "%~dp0*" "%INSTALL_PATH%" >nul

REM Create desktop shortcut
echo.
choice /C YN /M "Create desktop shortcut"
if errorlevel 2 goto skip_desktop
powershell -Command "$WS = New-Object -ComObject WScript.Shell; $SC = $WS.CreateShortcut('%USERPROFILE%\Desktop\NotNaraka Launcher.lnk'); $SC.TargetPath = '%INSTALL_PATH%\NarakaTweaks.Launcher.exe'; $SC.WorkingDirectory = '%INSTALL_PATH%'; $SC.Description = 'NotNaraka Launcher'; $SC.Save()"
echo Desktop shortcut created!
:skip_desktop

REM Create Start Menu shortcut
echo.
choice /C YN /M "Create Start Menu shortcut"
if errorlevel 2 goto skip_start
powershell -Command "$WS = New-Object -ComObject WScript.Shell; $SC = $WS.CreateShortcut('%APPDATA%\Microsoft\Windows\Start Menu\Programs\NotNaraka Launcher.lnk'); $SC.TargetPath = '%INSTALL_PATH%\NarakaTweaks.Launcher.exe'; $SC.WorkingDirectory = '%INSTALL_PATH%'; $SC.Description = 'NotNaraka Launcher'; $SC.Save()"
echo Start Menu shortcut created!
:skip_start

echo.
echo ========================================
echo   Installation Complete!
echo ========================================
echo.
echo Launcher installed to: %INSTALL_PATH%
echo.
choice /C YN /M "Launch NotNaraka Launcher now"
if errorlevel 2 goto end
start "" "%INSTALL_PATH%\NarakaTweaks.Launcher.exe"
:end
pause
'@

$BatchPath = Join-Path $TempPath "install.bat"
$BatchInstaller | Out-File -FilePath $BatchPath -Encoding ASCII

# Create README
$ReadmeContent = "NotNaraka Launcher v1.0.0`r`n`r`n"
$ReadmeContent += "INSTALLATION INSTRUCTIONS:`r`n`r`n"
$ReadmeContent += "1. Run install.bat`r`n"
$ReadmeContent += "2. Follow the on-screen prompts`r`n`r`n"
$ReadmeContent += "REQUIREMENTS:`r`n`r`n"
$ReadmeContent += "- Windows 10/11 64-bit`r`n"
$ReadmeContent += "- .NET 8.0 Desktop Runtime`r`n`r`n"
$ReadmeContent += "If you don't have .NET installed, the installer will offer to open the download page.`r`n`r`n"
$ReadmeContent += "FEATURES:`r`n`r`n"
$ReadmeContent += "- Launch Naraka: Bladepoint`r`n"
$ReadmeContent += "- Download CN and Global clients`r`n"
$ReadmeContent += "- System performance tweaks`r`n"
$ReadmeContent += "- Steam news feed`r`n"
$ReadmeContent += "- Real-time logging`r`n"

$ReadmePath = Join-Path $TempPath "README.txt"
$ReadmeContent | Out-File -FilePath $ReadmePath -Encoding ASCII

Write-Host "✅ Files prepared!" -ForegroundColor Green
Write-Host ""

# Create self-extracting archive using IExpress
Write-Host "🗜️  Creating self-extracting installer..." -ForegroundColor Cyan

$SedContent = @"
[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=1
HideExtractAnimation=0
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=%InstallPrompt%
DisplayLicense=%DisplayLicense%
FinishMessage=%FinishMessage%
TargetName=%TargetName%
FriendlyName=%FriendlyName%
AppLaunched=%AppLaunched%
PostInstallCmd=%PostInstallCmd%
AdminQuietInstCmd=%AdminQuietInstCmd%
UserQuietInstCmd=%UserQuietInstCmd%
SourceFiles=SourceFiles
[Strings]
InstallPrompt=Install NotNaraka Launcher?
DisplayLicense=
FinishMessage=Installation package extracted. Run install.bat to complete setup.
TargetName=$SetupOutputPath\NotNaraka-Launcher-Setup.exe
FriendlyName=NotNaraka Launcher v1.0.0
AppLaunched=cmd /c install.bat
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
FILE0="install.bat"
FILE1="README.txt"
[SourceFiles]
SourceFiles0=$TempPath\
[SourceFiles0]
%FILE0%=
%FILE1%=
"@

# Note: IExpress can't handle large numbers of files easily
# Let's create a ZIP instead and provide clear instructions

Write-Host "⚠️  IExpress has limitations with many files." -ForegroundColor Yellow
Write-Host "Creating a portable ZIP package instead..." -ForegroundColor Cyan
Write-Host ""

# Create ZIP
$ZipPath = Join-Path $ProjectRoot "NotNaraka-Launcher-Portable.zip"
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

Compress-Archive -Path "$TempPath\*" -DestinationPath $ZipPath -CompressionLevel Optimal

$ZipSize = (Get-Item $ZipPath).Length / 1MB

Write-Host "========================================" -ForegroundColor Green
Write-Host "  ✅ Package Created!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "📦 Portable Package:" -ForegroundColor Cyan
Write-Host "   File: NotNaraka-Launcher-Portable.zip" -ForegroundColor White
Write-Host "   Size: $([math]::Round($ZipSize, 2)) MB" -ForegroundColor White
Write-Host "   Location: $ZipPath" -ForegroundColor White
Write-Host ""
Write-Host "📝 For your friend:" -ForegroundColor Cyan
Write-Host ""
Write-Host "  1. Send them 'NotNaraka-Launcher-Portable.zip'" -ForegroundColor Yellow
Write-Host "  2. They extract it anywhere" -ForegroundColor Yellow
Write-Host "  3. They run 'install.bat' for guided install" -ForegroundColor Yellow
Write-Host "     OR" -ForegroundColor Yellow
Write-Host "  4. They can run 'NarakaTweaks.Launcher.exe' directly (portable mode)" -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "💡 Want a proper .MSI installer?" -ForegroundColor Cyan
Write-Host ""
Write-Host "For a professional .MSI installer, you'll need:" -ForegroundColor White
Write-Host "  - WiX Toolset: https://wixtoolset.org/" -ForegroundColor Gray
Write-Host "  - OR Inno Setup: https://jrsoftware.org/isinfo.php" -ForegroundColor Gray
Write-Host "  - OR Advanced Installer: https://www.advancedinstaller.com/" -ForegroundColor Gray
Write-Host ""
Write-Host "Would you like me to create a WiX installer script?" -ForegroundColor Cyan
Write-Host "(You'll need to install WiX Toolset separately)" -ForegroundColor Gray
Write-Host ""

Write-Host "Open folder?" -ForegroundColor Cyan
$openFolder = Read-Host "Open folder (Y/N)"

if ($openFolder -eq "Y" -or $openFolder -eq "y") {
    explorer $ProjectRoot
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
