param(
    [string]$Configuration = "Release"
)

$proj = "Naraka-Cheat-Detector.csproj"

Write-Host "Checking for dotnet CLI..."
try {
    dotnet --info > $null
} catch {
    Write-Error "dotnet CLI not found. Install the .NET SDK to continue."
    exit 1
}

Write-Host "Building project '$proj' (Configuration=$Configuration)..."
$rc = dotnet build $proj -c $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit $LASTEXITCODE
}

Write-Host "Build succeeded."