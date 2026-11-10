# W10 cold runs: starts the spike until N valid cold rows are logged; in each run you drag files over the window ONCE
# and drop them, then the process exits by itself (a new process each time = a cold first drag).
# Usage: .\run-cold.ps1 -Tag win11-100pct-monitor1 -Runs 10
# On a machine without the SDK: publish first (see README) and pass -Exe <path to DonutSpike.exe>.
param(
    [Parameter(Mandatory)][string]$Tag,
    [int]$Runs = 10,
    [int]$Screen = -1,        # open centred on this screen index (0, 1, 2); -1 = default
    [switch]$Straddle,        # with -Screen: put the window centre on the right edge of that screen
    [string]$Exe = '',        # published DonutSpike.exe; empty = build from source
    [int]$MaxAttempts = 0,    # safety stop; 0 = Runs * 2
    [string]$Log = (Join-Path $env:LOCALAPPDATA 'KaleiSpike04\latency-v2.csv')
)
$ErrorActionPreference = 'Stop'
if (-not $Exe) {
    $proj = Join-Path $PSScriptRoot 'DonutSpike\DonutSpike.csproj'
    dotnet build $proj -c Release --nologo -v q | Out-Null
    $Exe = Join-Path $PSScriptRoot 'DonutSpike\bin\Release\net10.0\DonutSpike.exe'
}
if ($MaxAttempts -le 0) { $MaxAttempts = $Runs * 2 }
function ColdRows {
    if (-not (Test-Path $Log)) { return 0 }
    @(Import-Csv $Log | Where-Object { $_.tag -eq $Tag -and $_.enter -eq '1' -and $_.flag -eq 'ok' }).Count
}
$attempt = 0
while ((ColdRows) -lt $Runs -and $attempt -lt $MaxAttempts) {
    $attempt++
    $before = ColdRows
    Write-Host "Valid cold rows $before of $Runs (attempt $attempt) - drag the sample files over the window, drop, wait for the window to close."
    $a = @('--exit-after-drop', '--tag', $Tag, '--log', $Log)
    if ($Screen -ge 0) { $a += $(if ($Straddle) { '--straddle' } else { '--screen' }), $Screen }
    $p = Start-Process -FilePath $Exe -ArgumentList $a -PassThru
    $p.WaitForExit()
    if ((ColdRows) -eq $before) { Write-Warning 'That run left no valid cold row (dropped too early or no frame): it will be repeated.' }
}
& (Join-Path $PSScriptRoot 'summarize.ps1') -Tag $Tag -Log $Log
