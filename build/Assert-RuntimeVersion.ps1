<#
.SYNOPSIS
  Fails if a published runtime or ILCompiler package is older than the minimum (advisory GHSA-p3jp-9v83-76w9).
.DESCRIPTION
  Reads every *.deps.json under -Path (self-contained publishes list the runtime pack there) and every
  project.assets.json under -AssetsPath, and checks the versions of Microsoft.NETCore.App.Runtime.*,
  Microsoft.NETCore.App.Runtime.NativeAOT.* and Microsoft.DotNet.ILCompiler. The check fails when no runtime
  package is found at all, so it can never pass on an empty folder.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Path,
    [string] $AssetsPath,
    [version] $Minimum = '10.0.14'
)

$ErrorActionPreference = 'Stop'
$pattern = '^(?:runtime(?:pack)?\.)?(?:[\w.-]*\.)?(?<id>Microsoft\.NETCore\.App\.Runtime(?:\.NativeAOT)?\.[\w-]+|Microsoft\.DotNet\.ILCompiler)/(?<ver>[^/]+)$'

$files = @(Get-ChildItem -Path $Path -Filter '*.deps.json' -Recurse -File)
if ($AssetsPath) { $files += @(Get-ChildItem -Path $AssetsPath -Filter 'project.assets.json' -Recurse -File) }

$found = @{}
foreach ($file in $files) {
    $json = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    $keys = @()
    if ($json.libraries) { $keys += $json.libraries.PSObject.Properties.Name }
    foreach ($key in $keys) {
        if ($key -match $pattern) {
            $found["$($Matches.id) $($Matches.ver)"] = [pscustomobject]@{ Id = $Matches.id; Version = $Matches.ver }
        }
    }
}

$runtimeSeen = $false
$bad = @()
foreach ($item in $found.Values | Sort-Object Id, Version) {
    Write-Host "$($item.Id) $($item.Version)"
    if ($item.Id -like 'Microsoft.NETCore.App.Runtime*') { $runtimeSeen = $true }
    $core = ($item.Version -split '[-+]')[0]
    if ([version]$core -lt $Minimum) { $bad += "$($item.Id) $($item.Version)" }
}

if (-not $runtimeSeen) { throw "No Microsoft.NETCore.App.Runtime package found under '$Path': nothing was checked." }
if ($bad.Count -gt 0) { throw "Below $Minimum`: $($bad -join ', ')" }
Write-Host "OK: every runtime and ILCompiler package is $Minimum or later."
