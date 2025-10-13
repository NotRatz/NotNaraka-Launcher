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
