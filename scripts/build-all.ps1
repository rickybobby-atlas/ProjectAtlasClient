param (
    [string]$Configuration = "Release",
    [string]$OutputDir = "$PSScriptRoot\..\artifacts"
)

$ErrorActionPreference = "Stop"
Write-Host "=== Project Atlas CI/CD Build Pipeline ===" -ForegroundColor Cyan

# Clean & recreate artifacts output folder
if (Test-Path $OutputDir) { Remove-Item -Recurse -Force $OutputDir }
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Copy updatable server config into the build output
Copy-Item "$PSScriptRoot\..\config\atlaslauncher.cfg" -Destination $OutputDir -Force
Write-Host "[OK] Copied atlaslauncher.cfg to artifacts." -ForegroundColor Green

# Build ClassicUO once Phase 2 adds it
$cuoProject = "$PSScriptRoot\..\src\ClassicUO\src\ClassicUO.Client\ClassicUO.Client.csproj"
if (Test-Path $cuoProject) {
    Write-Host "Building ClassicUO Client..." -ForegroundColor Yellow
    dotnet publish $cuoProject -c $Configuration -o "$OutputDir\Client"
    Write-Host "[OK] ClassicUO compiled successfully." -ForegroundColor Green
} else {
    Write-Host "[SKIP] ClassicUO not added yet (Scheduled for Phase 2)." -ForegroundColor DarkGray
}

# Build AtlasLauncher once Phase 5 adds it
$launcherProject = "$PSScriptRoot\..\src\AtlasLauncher\AtlasLauncher.csproj"
if (Test-Path $launcherProject) {
    Write-Host "Building AtlasLauncher..." -ForegroundColor Yellow
    dotnet publish $launcherProject -c $Configuration -o "$OutputDir"
    Write-Host "[OK] AtlasLauncher compiled successfully." -ForegroundColor Green
} else {
    Write-Host "[SKIP] AtlasLauncher not added yet (Scheduled for Phase 5)." -ForegroundColor DarkGray
}

Write-Host "=== Pipeline Finished Successfully ===" -ForegroundColor Cyan
