# Temporary auto-logon for the W7 breakdown (about 60 reboots at most). Started ONCE by avvia.ps1 with elevation (UAC).
# - password asked here, checked against the local account, stored ONLY as LSA secret "DefaultPassword" (never in clear in the registry);
# - Winlogon values saved first, so they can be restored exactly;
# - a SYSTEM task removes everything when avvia.ps1 writes stato\fine.txt (or after 6 hours, whatever happens).
param(
    [Parameter(Mandatory)][string]$Root,
    [Parameter(Mandatory)][string]$User
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
. (Join-Path $PSScriptRoot 'lsa.ps1')

$taskClean = 'KaleiW7-AutologonCleanup'
$taskDeadline = 'KaleiW7-AutologonDeadline'
$wl = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon'
$dir = Join-Path $env:ProgramData 'KaleiW7-cleanup'
$state = Join-Path $Root 'stato'

try {
    if (-not (Test-Elevated)) { throw 'Serve una PowerShell elevata (questo script viene lanciato da avvia.ps1 con UAC).' }
    if ($env:USERNAME -ne $User) { throw 'L''account elevato non e'' quello che ha lanciato avvia.ps1: interrompo.' }

    Write-Host ''
    Write-Host '=== Accesso automatico temporaneo per le misure W7 ===' -ForegroundColor Cyan
    Write-Host '  - La password viene chiesta solo qui, verificata e salvata SOLO come segreto LSA (cifrato), mai in chiaro nel registro.'
    Write-Host '  - Viene rimosso da solo quando le misure finiscono (task di sistema, controlla ogni minuto) e comunque entro 6 ore.'
    Write-Host '  - Dopo W7 tornerai al checkpoint "aggiornato": nella VM non resta nulla.'
    Write-Host ''

    $props = Get-ItemProperty -Path $wl
    if ($props.PSObject.Properties['DefaultPassword']) { throw 'Nel registro esiste gia'' un DefaultPassword in chiaro: non lo tocco. Interrompo.' }

    $sec = Read-Host ('Password dell''account ' + $User) -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
    try { $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
    if ([string]::IsNullOrEmpty($plain)) { throw 'Password vuota: per questo account serve la password.' }

    Add-Type -AssemblyName System.DirectoryServices.AccountManagement
    $ctx = New-Object System.DirectoryServices.AccountManagement.PrincipalContext('Machine')
    if (-not $ctx.ValidateCredentials($User, $plain)) { $plain = $null; throw 'Password non corretta: non attivo nulla.' }

    # Save previous Winlogon values (no secrets: a clear DefaultPassword was refused above).
    $names = 'AutoAdminLogon', 'DefaultUserName', 'DefaultDomainName', 'AutoLogonCount', 'ForceAutoLogon'
    $prev = [ordered]@{}
    foreach ($n in $names) { $p = $props.PSObject.Properties[$n]; if ($p) { $prev[$n] = $p.Value } else { $prev[$n] = $null } }
    $deadline = (Get-Date).ToUniversalTime().AddHours(6)
    $prev['deadlineUtc'] = $deadline.ToString('o')

    # Private, admin-only folder for what the SYSTEM task runs.
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    & icacls.exe $dir /inheritance:r /grant:r 'SYSTEM:(OI)(CI)F' 'Administrators:(OI)(CI)F' | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'autologon-cleanup.ps1') (Join-Path $dir 'cleanup.ps1') -Force
    Copy-Item (Join-Path $PSScriptRoot 'lsa.ps1') (Join-Path $dir 'lsa.ps1') -Force
    Write-TextFile (Join-Path $dir 'prev.json') (ConvertTo-Json -InputObject $prev -Depth 4)

    # Register the cleanup tasks BEFORE touching the logon settings.
    $cleanup = Join-Path $dir 'cleanup.ps1'
    $marker = Join-Path $state 'fine.txt'
    $psArgs = '-NoProfile -ExecutionPolicy Bypass -File "{0}" -Root "{1}"' -f $cleanup, $Root
    $principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Minutes 5) -MultipleInstances IgnoreNew

    # cmd only starts PowerShell when the marker exists, so the every-minute check costs nothing during the W7 timings.
    $actionClean = New-ScheduledTaskAction -Execute 'cmd.exe' -Argument ('/c if exist "{0}" powershell.exe {1}' -f $marker, $psArgs)
    $t1 = New-ScheduledTaskTrigger -AtStartup
    $t2 = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Minutes 1) -RepetitionDuration (New-TimeSpan -Days 1)
    Register-ScheduledTask -TaskName $taskClean -Action $actionClean -Trigger @($t1, $t2) -Principal $principal -Settings $settings -Force | Out-Null

    $actionDeadline = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument ($psArgs + ' -Force')
    $t3 = New-ScheduledTaskTrigger -Once -At $deadline.ToLocalTime()
    Register-ScheduledTask -TaskName $taskDeadline -Action $actionDeadline -Trigger $t3 -Principal $principal -Settings $settings -Force | Out-Null

    # Now the auto-logon itself.
    [KaleiLsa]::Set($plain)
    $plain = $null
    if (-not [KaleiLsa]::Exists()) { throw 'Il segreto LSA non risulta salvato.' }
    Set-ItemProperty -Path $wl -Name 'AutoAdminLogon' -Value '1' -Type String
    Set-ItemProperty -Path $wl -Name 'DefaultUserName' -Value $User -Type String
    Set-ItemProperty -Path $wl -Name 'DefaultDomainName' -Value $env:COMPUTERNAME -Type String
    Set-ItemProperty -Path $wl -Name 'AutoLogonCount' -Value 70 -Type DWord
    if ((Get-ItemProperty -Path $wl).PSObject.Properties['DefaultPassword']) { throw 'DefaultPassword compare nel registro: non previsto.' }

    New-Item -ItemType Directory -Force -Path $state | Out-Null
    Write-JsonFile ([pscustomobject]@{ ok = $true; whenUtc = (Get-Date).ToUniversalTime().ToString('o'); deadlineUtc = $deadline.ToString('o'); passwordStore = 'LSA secret (DefaultPassword); nothing in clear in the registry'; tasks = @($taskClean, $taskDeadline) }) (Join-Path $state 'autologon-setup.json')

    Write-Host ''
    Write-Host '  Accesso automatico attivo (password solo come segreto LSA). Torno a avvia.ps1.' -ForegroundColor Green
    Start-Sleep -Seconds 3
    exit 0
}
catch {
    $plain = $null
    Write-Host ('  ERRORE: ' + $_.Exception.Message) -ForegroundColor Red
    Write-Host '  Nessun accesso automatico attivato (se erano gia'' stati creati i task di pulizia, si rimuovono da soli).' -ForegroundColor Yellow
    foreach ($t in $taskClean, $taskDeadline) { Unregister-ScheduledTask -TaskName $t -Confirm:$false -ErrorAction SilentlyContinue }
    try { [KaleiLsa]::Delete() } catch { }
    Read-Host 'Premi INVIO per chiudere' | Out-Null
    exit 1
}
