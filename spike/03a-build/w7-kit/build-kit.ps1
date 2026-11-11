# THROWAWAY spike 03c. Assembles the VM kit: scripts + LEGGIMI + the six published variants (without .pdb, which are ~100 MB).
# Usage (PowerShell, from spike\03a-build\w7-kit, after ..\build-variants.ps1):  .\build-kit.ps1 [-Dest E:\KaleiVM-W7]
# It only copies; it never deletes anything in the destination (a refresh over an old kit keeps stato\ and risultati\).
param([string]$Dest = 'E:\KaleiVM-W7')
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$artifacts = Join-Path (Split-Path $here -Parent) 'artifacts\w7'
foreach ($v in 'a', 'b', 'c', 'd', 'e', 'f') {
    if (-not (Test-Path (Join-Path $artifacts "$v\AotWindow.exe"))) { throw "variant $v not published: run ..\build-variants.ps1 first" }
}
New-Item -ItemType Directory -Force -Path $Dest | Out-Null
Copy-Item (Join-Path $here 'avvia.ps1'), (Join-Path $here 'LEGGIMI.txt') -Destination $Dest -Force
New-Item -ItemType Directory -Force -Path (Join-Path $Dest 'lib') | Out-Null
Copy-Item (Join-Path $here 'lib\*') -Destination (Join-Path $Dest 'lib') -Force
foreach ($v in 'a', 'b', 'c', 'd', 'e', 'f') {
    $target = Join-Path $Dest "varianti\$v"
    & robocopy.exe (Join-Path $artifacts $v) $target /E /XF *.pdb /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed for variant $v (code $LASTEXITCODE)" }
}
$LASTEXITCODE = 0
$size = (Get-ChildItem $Dest -Recurse -File | Measure-Object Length -Sum).Sum
'Kit ready in {0}: {1:N0} MB' -f $Dest, ($size / 1MB)
