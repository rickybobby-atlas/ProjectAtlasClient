param (
    [string]$Version = "1.0.0",
    [switch]$IncludeUOFilesInManifest = $false,
    [switch]$BuildInstaller = $true
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
$releaseDir = "C:\UO_Projects\AtlasStaging\ReleaseBuild"

# 1. Build all C# projects
& "$PSScriptRoot\build-all.ps1" -Configuration Release

# 2. Sync fresh binaries & config to ReleaseBuild
Copy-Item "$repoRoot\artifacts\Client\*" -Destination "$releaseDir\Client" -Recurse -Force
Copy-Item "$repoRoot\artifacts\AtlasLauncher*" -Destination $releaseDir -Force
Copy-Item "$repoRoot\artifacts\launcher-bg.jpg" -Destination $releaseDir -Force
Copy-Item "$repoRoot\artifacts\ProjectAtlas.ico" -Destination $releaseDir -Force
Copy-Item "$repoRoot\config\atlaslauncher.cfg" -Destination "$releaseDir\atlaslauncher.cfg" -Force

# 3. Generate SHA-256 Patch Manifest
& "$PSScriptRoot\generate-manifest.ps1" -ReleaseDir $releaseDir -Version $Version -IncludeUOFiles:$IncludeUOFilesInManifest

# 4. Compile Inno Setup Installer if requested
if ($BuildInstaller) {
    Write-Host "=== Compiling Inno Setup Installer (ProjectAtlasSetup.exe) ===" -ForegroundColor Cyan
    $iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    & $iscc "$repoRoot\installer\ProjectAtlas.iss"
    Write-Host "[OK] Installer compiled at C:\UO_Projects\AtlasStaging\InstallerOutput\ProjectAtlasSetup.exe" -ForegroundColor Green
}
