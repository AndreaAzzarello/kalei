# Removes the temporary auto-logon of the W7 breakdown. Runs as SYSTEM from C:\ProgramData\KaleiW7-cleanup\cleanup.ps1
# (copied there by autologon-setup.ps1). Without -Force it does nothing until stato\fine.txt exists.
# On success it deletes the LSA secret, restores the Winlogon values, verifies, writes stato\autologon-cleanup.json,
# unregisters its tasks and removes its own folder. On failure it keeps the task, which retries every minute.
param(
    [Parameter(Mandatory)][string]$Root,
    [switch]$Force
)
$ErrorActionPreference = 'Continue'
$here = $PSScriptRoot
$marker = Join-Path $Root 'stato\fine.txt'
$resultFile = Join-Path $Root 'stato\autologon-cleanup.json'
if (-not $Force -and -not (Test-Path -LiteralPath $marker)) { exit 0 }

. (Join-Path $here 'lsa.ps1')
$wl = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon'
$names = 'AutoAdminLogon', 'DefaultUserName', 'DefaultDomainName', 'AutoLogonCount', 'ForceAutoLogon'
$notes = New-Object System.Collections.ArrayList
$ok = $true

$prev = Get-Content -LiteralPath (Join-Path $here 'prev.json') -Raw | ConvertFrom-Json

# 1. LSA secret
try {
    [KaleiLsa]::Delete()
    if ([KaleiLsa]::Exists()) { $ok = $false; [void]$notes.Add('LSA secret still present') } else { [void]$notes.Add('LSA secret removed') }
} catch { $ok = $false; [void]$notes.Add('LSA: ' + $_.Exception.Message) }

# 2. Registry: never a clear DefaultPassword; restore the saved values
Remove-ItemProperty -Path $wl -Name 'DefaultPassword' -ErrorAction SilentlyContinue
foreach ($n in $names) {
    $v = $prev.$n
    try {
        if ($null -eq $v) { Remove-ItemProperty -Path $wl -Name $n -ErrorAction SilentlyContinue }
        elseif ($v -is [string]) { Set-ItemProperty -Path $wl -Name $n -Value $v -Type String }
        else { Set-ItemProperty -Path $wl -Name $n -Value ([int]$v) -Type DWord }
    } catch { $ok = $false; [void]$notes.Add('registry ' + $n + ': ' + $_.Exception.Message) }
}

# 3. Verify
$now = Get-ItemProperty -Path $wl
if ($now.PSObject.Properties['DefaultPassword']) { $ok = $false; [void]$notes.Add('DefaultPassword present in registry') }
foreach ($n in $names) {
    $cur = $now.PSObject.Properties[$n]
    $want = $prev.$n
    if ($null -eq $want) { if ($cur) { $ok = $false; [void]$notes.Add($n + ' should be absent') } }
    else { if (-not $cur -or "$($cur.Value)" -ne "$want") { $ok = $false; [void]$notes.Add($n + ' not restored') } }
}
if ($ok) { [void]$notes.Add('Winlogon values restored and verified') }

$result = [pscustomobject]@{ ok = $ok; whenUtc = (Get-Date).ToUniversalTime().ToString('o'); forced = [bool]$Force; notes = @($notes) }
try {
    New-Item -ItemType Directory -Force -Path (Split-Path $resultFile) | Out-Null
    [IO.File]::WriteAllText($resultFile, (ConvertTo-Json -InputObject $result -Depth 4), (New-Object Text.UTF8Encoding($false)))
} catch { }

if ($ok) {
    foreach ($t in 'KaleiW7-AutologonCleanup', 'KaleiW7-AutologonDeadline') { Unregister-ScheduledTask -TaskName $t -Confirm:$false -ErrorAction SilentlyContinue }
    Remove-Item -LiteralPath $here -Recurse -Force -ErrorAction SilentlyContinue
}
exit 0
