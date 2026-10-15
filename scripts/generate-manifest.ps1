param (
    [string]$ReleaseDir = "C:\UO_Projects\AtlasStaging\ReleaseBuild",
    [string]$Version = "1.0.0",
    [switch]$IncludeUOFiles = $true
)

$ErrorActionPreference = "Stop"
Write-Host "=== Generating Project Atlas Patch Manifest (v$Version) ===" -ForegroundColor Cyan

$resolvedRoot = (Resolve-Path $ReleaseDir).Path.TrimEnd('\')
$allFiles = Get-ChildItem -Path $resolvedRoot -File -Recurse

$manifestEntries = New-Object System.Collections.Generic.List[Object]

foreach ($file in $allFiles) {
    $relPath = $file.FullName.Substring($resolvedRoot.Length + 1).Replace('\', '/')
    $lower = $relPath.ToLowerInvariant()

    # 1. Exclude player credentials, dev.mode, profiles, macros, scripts, logs, and running launcher binaries
    #    NOTE: atlaslauncher.cfg IS included so you can remotely push IP, Port, and Link updates!
    if ($lower -eq "manifest.json" -or
        $lower -eq "dev.mode" -or
        $lower.EndsWith("settings.json") -or
        $lower.EndsWith("settings.dev.json") -or
        $lower.EndsWith(".log") -or
        $lower.EndsWith(".pdb") -or
        $lower.Contains("/profiles/") -or
        $lower.Contains("/macros/") -or
        $lower.Contains("/scripts/") -or
        $lower.Contains("/data/profiles/") -or
        $lower -eq "atlaslauncher.exe" -or
        $lower -eq "atlaslauncher.dll" -or
        $lower -eq "atlaslauncher.deps.json" -or
        $lower -eq "atlaslauncher.runtimeconfig.json") {
        continue
    }

    if (-not $IncludeUOFiles -and $lower.StartsWith("uofiles/")) {
        continue
    }

    $hash = (Get-FileHash -Path $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $manifestEntries.Add([PSCustomObject]@{
        Path   = $relPath
        Sha256 = $hash
        Size   = $file.Length
    })
}

$manifestObj = [PSCustomObject]@{
    Version = $Version
    Files   = $manifestEntries
}

$outputPath = Join-Path $resolvedRoot "manifest.json"
$manifestObj | ConvertTo-Json -Depth 5 | Set-Content -Path $outputPath -Encoding UTF8

Write-Host "[OK] Generated $outputPath with $($manifestEntries.Count) tracked files." -ForegroundColor Green
