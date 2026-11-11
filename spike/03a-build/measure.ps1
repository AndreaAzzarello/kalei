# THROWAWAY spike 03a. Measures the published variants of AotWindow: start-up (10 runs) and size (folder and ZIP, without .pdb).
# Usage (PowerShell): .\measure.ps1 [-Variants aot,r2r] [-Runs 10]
# Start-up = from process creation to the second animation frame after the window opened (see MainWindow.axaml.cs).
# The first run after a publish is reported separately; the other runs are warm (file cache not dropped, no reboot).
param([string[]]$Variants = @('aot', 'r2r'), [int]$Runs = 10)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$root = $PSScriptRoot
$out = @()
foreach ($v in $Variants) {
    $dir = Join-Path $root "artifacts\$v"
    $exe = Join-Path $dir 'AotWindow.exe'
    if (-not (Test-Path $exe)) { Write-Host "skip $v (not published)"; continue }
    $times = @()
    for ($i = 0; $i -lt $Runs; $i++) {
        $f = Join-Path $env:TEMP "aotwindow-$v-$i.txt"
        if (Test-Path $f) { Remove-Item $f }
        $p = Start-Process $exe -ArgumentList '--out', $f -PassThru
        if (-not $p.WaitForExit(30000)) { $p.Kill(); throw "$v run $i did not finish" }
        $times += [double](([IO.File]::ReadAllText($f) | ConvertFrom-Json).marks.frame2)
        Remove-Item $f
    }
    $sorted = $times | Sort-Object
    $median = ($sorted[[math]::Floor(($sorted.Count - 1) / 2)] + $sorted[[math]::Ceiling(($sorted.Count - 1) / 2)]) / 2
    $files = Get-ChildItem $dir -Recurse -File | Where-Object { $_.Extension -ne '.pdb' }
    $bytes = ($files | Measure-Object Length -Sum).Sum
    $zip = Join-Path $env:TEMP "aotwindow-$v.zip"
    if (Test-Path $zip) { Remove-Item $zip }
    $stage = Join-Path $env:TEMP "aotwindow-stage-$v"
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
    New-Item -ItemType Directory $stage | Out-Null
    foreach ($f in $files) { $rel = $f.FullName.Substring($dir.Length + 1); $t = Join-Path $stage $rel; New-Item -ItemType Directory (Split-Path $t) -Force | Out-Null; Copy-Item $f.FullName $t }
    [System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $zip, [System.IO.Compression.CompressionLevel]::Optimal, $false)
    $zipBytes = (Get-Item $zip).Length
    Remove-Item $stage -Recurse -Force; Remove-Item $zip
    $out += [pscustomobject]@{
        variant = $v; runs = $Runs; firstRunMs = [math]::Round($times[0]); medianMs = [math]::Round($median); minMs = [math]::Round($sorted[0]); maxMs = [math]::Round($sorted[-1])
        folderMB = [math]::Round($bytes / 1MB, 1); zipMB = [math]::Round($zipBytes / 1MB, 1)
        os = (Get-CimInstance Win32_OperatingSystem).Caption + ' ' + (Get-CimInstance Win32_OperatingSystem).BuildNumber
    }
}
$out | Format-Table -AutoSize
$resultsDir = Join-Path $root 'results'
New-Item -ItemType Directory $resultsDir -Force | Out-Null
$out | ConvertTo-Json | Set-Content -Encoding utf8 (Join-Path $resultsDir 'startup-size.json')
