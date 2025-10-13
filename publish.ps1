param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Output = "artifacts\publish",
    [switch]$FrameworkDependent
)

$proj = "Naraka-Cheat-Detector.csproj"

function Fail([string]$message, [int]$code = 1) {
    Write-Host $message -ForegroundColor Red
    exit $code
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Portable Executable Publisher" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Checking for dotnet CLI..."
try {
    dotnet --info > $null 2>&1
} catch {
    Fail "dotnet CLI not found. Install the .NET SDK to continue." 1
}

Write-Host "? dotnet CLI found" -ForegroundColor Green
Write-Host ""

# Determine if we're building self-contained (default) or framework-dependent
$isSelfContained = -not $FrameworkDependent.IsPresent

if ($isSelfContained) {
    Write-Host "Mode: Self-Contained (portable, no .NET runtime required)" -ForegroundColor Yellow
} else {
    Write-Host "Mode: Framework-Dependent (requires .NET 8 runtime installed)" -ForegroundColor Yellow
}

Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Runtime: $Runtime" -ForegroundColor Yellow
Write-Host "Output: $Output" -ForegroundColor Yellow
Write-Host ""

# Build the arguments
$arguments = @(
    "publish"
    "`"$proj`""
    "-c", $Configuration
    "-r", $Runtime
    "-o", "`"$Output`""
    "-p:PublishSingleFile=true"
    "-p:IncludeNativeLibrariesForSelfExtract=true"
    "-p:IncludeAllContentForSelfExtract=true"
    "-p:EnableCompressionInSingleFile=true"
)

if ($isSelfContained) {
    $arguments += "--self-contained", "true"
    $arguments += "-p:PublishTrimmed=true"
    $arguments += "-p:PublishReadyToRun=true"
} else {
    $arguments += "--self-contained", "false"
    $arguments += "-p:PublishTrimmed=false"
    $arguments += "-p:PublishReadyToRun=false"
}

Write-Host "Publishing project..." -ForegroundColor Cyan
Write-Host "Command: dotnet $($arguments -join ' ')" -ForegroundColor Gray
Write-Host ""

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "dotnet"
$psi.Arguments = $arguments -join ' '
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $false

$proc = New-Object System.Diagnostics.Process
$proc.StartInfo = $psi

if (-not $proc.Start()) {
    Fail "Failed to start dotnet process." 1
}

$stdout = $proc.StandardOutput.ReadToEnd()
$stderr = $proc.StandardError.ReadToEnd()
$proc.WaitForExit()

if ($stdout) { Write-Host $stdout }
if ($stderr -and $stderr.Trim()) { Write-Host $stderr -ForegroundColor Yellow }

if ($proc.ExitCode -ne 0) {
    Fail "Publish failed. ExitCode=$($proc.ExitCode)" $proc.ExitCode
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "? Publish succeeded!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Find the executable
$exePath = Join-Path $Output "Naraka-Cheat-Detector.exe"
if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    $sizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
    
    Write-Host "Executable: $exePath" -ForegroundColor Cyan
    Write-Host "Size: $sizeMB MB" -ForegroundColor Cyan
    Write-Host ""
    
    if ($isSelfContained) {
        Write-Host "This is a PORTABLE executable that includes the .NET runtime." -ForegroundColor Green
        Write-Host "It can run on any Windows 10+ machine without installing .NET." -ForegroundColor Green
    } else {
        Write-Host "This executable requires .NET 8 runtime to be installed." -ForegroundColor Yellow
    }
} else {
    Write-Host "Warning: Executable not found at expected path: $exePath" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "To create framework-dependent build (smaller size), use:" -ForegroundColor Gray
Write-Host "  .\publish.ps1 -FrameworkDependent" -ForegroundColor Gray