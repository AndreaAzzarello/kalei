# THROWAWAY spike 03c. Publishes the W7 variants of AotWindow, all self-contained, ReadyToRun + trimming, into artifacts\w7\<variant>.
# Usage (PowerShell, from spike\03a-build): .\build-variants.ps1 [-Variants a,b,c,d,e,f]
# Each variant has its own obj folder (the package set and the defined constants differ). Warnings are kept in artifacts\w7\<variant>-publish.txt.
param([string[]]$Variants = @('a', 'b', 'c', 'd', 'e', 'f'))
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
foreach ($v in $Variants) {
    $out = Join-Path $root "artifacts\w7\$v"
    $log = Join-Path $root "artifacts\w7\$v-publish.txt"
    New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
    if (Test-Path $out) { Remove-Item $out -Recurse -Force }
    Write-Host "== publish variant $v"
    $obj = (Join-Path $root "obj\$v") + '\'
    & dotnet publish (Join-Path $root 'AotWindow') -c Release -r win-x64 --self-contained `
        -p:PublishReadyToRun=true -p:PublishTrimmed=true "-p:Variant=$v" "-p:BaseIntermediateOutputPath=$obj" `
        -o $out 2>&1 | Tee-Object -FilePath $log | Select-Object -Last 3
    if ($LASTEXITCODE -ne 0) { throw "publish of variant $v failed, see $log" }
    $files = Get-ChildItem $out -Recurse -File | Where-Object { $_.Extension -ne '.pdb' }
    '{0}: {1} files, {2:N1} MB (without .pdb)' -f $v, $files.Count, (($files | Measure-Object Length -Sum).Sum / 1MB) | Write-Host
}
