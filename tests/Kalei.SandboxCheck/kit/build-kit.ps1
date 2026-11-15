# Builds the kit for the reference VM (ticket 07b) from the repository. Windows PowerShell 5.1 or PowerShell 7. ASCII only.
#     .\tests\Kalei.SandboxCheck\kit\build-kit.ps1                      # -> E:\KaleiVM-07b
#     .\tests\Kalei.SandboxCheck\kit\build-kit.ps1 -Target C:\some\dir
# Replaces worker\, tools\, lib\, avvia.ps1 and LEGGIMI.txt. NEVER touches stato\ or risultati\ (data is never deleted).
param([string]$Target = 'E:\KaleiVM-07b')
$ErrorActionPreference = 'Stop'
$kit = $PSScriptRoot
$repo = (Resolve-Path (Join-Path $kit '..\..\..')).Path

function Publish([string]$Project, [string]$To) {
    & dotnet publish (Join-Path $repo $Project) -c Release -r win-x64 --self-contained -p:TreatWarningsAsErrors=true -o $To
    if ($LASTEXITCODE -ne 0) { throw "publish of $Project failed" }
}

New-Item -ItemType Directory -Force -Path $Target | Out-Null
foreach ($sub in 'worker', 'tools', 'lib') {
    $p = Join-Path $Target $sub
    if (Test-Path -LiteralPath $p) { Remove-Item -LiteralPath $p -Recurse -Force }
}

# The Worker exactly as shipped: trimmed, self-contained. The tools and the test Worker are untrimmed and share one folder.
Publish 'src\Kalei.Worker.Builtin' (Join-Path $Target 'worker')
Publish 'tests\Kalei.SandboxCheck' (Join-Path $Target 'tools')
Publish 'tests\Kalei.FakeWorker' (Join-Path $Target 'tools')

Copy-Item -LiteralPath (Join-Path $kit 'avvia.ps1') -Destination $Target -Force
Copy-Item -LiteralPath (Join-Path $kit 'LEGGIMI.txt') -Destination $Target -Force
Copy-Item -LiteralPath (Join-Path $kit 'lib') -Destination $Target -Recurse -Force
New-Item -ItemType Directory -Force -Path (Join-Path $Target 'risultati') | Out-Null

$size = (Get-ChildItem -LiteralPath $Target -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host ('Kit pronto in {0} ({1:N0} MB)' -f $Target, $size)
