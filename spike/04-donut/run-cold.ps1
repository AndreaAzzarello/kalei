# W10 cold runs: starts the spike N times; in each run you drag files over the window ONCE and drop them,
# then the process exits by itself (a new process each time = a cold first drag).
# Usage: .\run-cold.ps1 -Tag win11-100pct-monitor1 -Runs 10
param(
    [Parameter(Mandatory)][string]$Tag,
    [int]$Runs = 10,
    [int]$Screen = -1,        # open centred on this screen index (0, 1, 2); -1 = default
    [switch]$Straddle,        # with -Screen: put the window centre on the right edge of that screen

    [string]$Log = (Join-Path $env:LOCALAPPDATA 'KaleiSpike04\latency.csv')
)
$ErrorActionPreference = 'Stop'
$proj = Join-Path $PSScriptRoot 'DonutSpike\DonutSpike.csproj'
dotnet build $proj -c Release --nologo -v q | Out-Null
$exe = Join-Path $PSScriptRoot 'DonutSpike\bin\Release\net10.0\DonutSpike.exe'
for ($i = 1; $i -le $Runs; $i++) {
    Write-Host "Run $i of $Runs - drag the sample files over the window, drop, wait for the window to close."
    $a = @('--exit-after-drop', '--tag', $Tag, '--log', $Log)
    if ($Screen -ge 0) { $a += $(if ($Straddle) { '--straddle' } else { '--screen' }), $Screen }
    $p = Start-Process -FilePath $exe -ArgumentList $a -PassThru
    $p.WaitForExit()
}
& (Join-Path $PSScriptRoot 'summarize.ps1') -Tag $Tag -Log $Log
