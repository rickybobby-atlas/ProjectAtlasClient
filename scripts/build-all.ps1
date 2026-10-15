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

# Step A: Build ClassicUO.Bootstrap (Produces ClassicUO.exe plugin host)
$bootstrapProject = "$PSScriptRoot\..\src\ClassicUO\src\ClassicUO.Bootstrap\src\ClassicUO.Bootstrap.csproj"
$cuoProject = "$PSScriptRoot\..\src\ClassicUO\src\ClassicUO.Client\ClassicUO.Client.csproj"

if (Test-Path $bootstrapProject) {
    Write-Host "Building ClassicUO Bootstrap Host (ClassicUO.exe)..." -ForegroundColor Yellow
    dotnet publish $bootstrapProject -c $Configuration -p:OutputType=WinExe -p:ApplicationIcon="C:\UO_Projects\ProjectAtlasClient\assets\ProjectAtlas.ico" -o "$OutputDir\Client"
    Write-Host "[OK] ClassicUO Bootstrap compiled." -ForegroundColor Green
}

# Step B: Build ClassicUO.Client as NativeAOT Shared Library (Produces cuo.dll)
if (Test-Path $cuoProject) {
    Write-Host "Building ClassicUO Engine (cuo.dll NativeAOT)..." -ForegroundColor Yellow
    dotnet publish $cuoProject -c $Configuration -p:NativeLib=Shared -p:OutputType=Library -r win-x64 -o "$OutputDir\Client"
    Write-Host "[OK] ClassicUO Engine (cuo.dll) compiled successfully." -ForegroundColor Green
}

# Step C: Build AtlasLauncher once Phase 5 adds it
$launcherProject = "$PSScriptRoot\..\src\AtlasLauncher\AtlasLauncher.csproj"
if (Test-Path $launcherProject) {
    Write-Host "Building AtlasLauncher..." -ForegroundColor Yellow
    dotnet publish $launcherProject -c $Configuration -o "$OutputDir"
    Write-Host "[OK] AtlasLauncher compiled successfully." -ForegroundColor Green
} else {
    Write-Host "[SKIP] AtlasLauncher not added yet (Scheduled for Phase 5)." -ForegroundColor DarkGray
}

Write-Host "=== Pipeline Finished Successfully ===" -ForegroundColor Cyan
