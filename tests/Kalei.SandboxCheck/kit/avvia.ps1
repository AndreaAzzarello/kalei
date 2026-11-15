# Kalei - ticket 07b: sandbox proofs and cold Hello of the Worker on the reference VM. Windows PowerShell 5.1, NOT elevated. ASCII only.
# Usage (normal PowerShell, from the kit folder copied to C:\KaleiSandbox):
#     Set-ExecutionPolicy -Scope Process Bypass
#     .\avvia.ps1
# It resumes by itself after every reboot (RunOnce at the next sign-in). Other modes:  .\avvia.ps1 -Report   |   .\avvia.ps1 -Annulla
#
# Design rules (lessons of the kits of #47 and 03c):
#  - the state is written BEFORE every step that can fail or reboot the machine;
#  - measurements are only appended to stato\misure.jsonl (one JSON object per line); progress is recomputed by counting rows;
#  - nothing deletes stato\ or risultati\;
#  - lines are read with [IO.File]::ReadAllLines and cast to [string] (never Get-Content into ConvertTo-Json);
#  - no automatic sign-in, no LSA secret, no password: the VM signs in without one.
param(
    [switch]$Resume,        # used by the automatic resume (RunOnce) after each reboot
    [switch]$Annulla,       # stop the measures, clean up and write the report with what exists
    [switch]$Report,        # only write the report from the data collected so far
    [switch]$DryRun,        # host test only: no waiting, no reboot; the measures are warm and NOT valid
    [int]$Cycles = 10,      # cold starts (reboot, settle, one start)
    [int]$SettleSeconds = 150,   # uptime to wait for before the start (method W7)
    [switch]$NoReboot,      # host test only: prints REBOOT and exits instead of restarting the machine
    [int]$FakeUptime = -1,  # host test only: pretends the machine has been up this many seconds
    [string]$DataDir = ''   # where stato\ and risultati\ go (default: the kit folder)
)
$ErrorActionPreference = 'Stop'
$Root = $PSScriptRoot
if (-not $DataDir) { $DataDir = $Root }
$State = Join-Path $DataDir 'stato'
$Out = Join-Path $DataDir 'risultati'
. (Join-Path $Root 'lib\common.ps1')
foreach ($d in $State, $Out) { New-Item -ItemType Directory -Force -Path $d | Out-Null }
$inv = [Globalization.CultureInfo]::InvariantCulture

$StatoFile = Join-Path $State 'stato.json'
$Misure = Join-Path $State 'misure.jsonl'
$EnvFile = Join-Path $State 'env.json'
$ProveFile = Join-Path $State 'prove.json'
$RunOnceKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\RunOnce'
$Profile07b = 'Kalei.Kit.07b'
$WorkerDir = Join-Path $Root 'worker'
$ToolsDir = Join-Path $Root 'tools'
$WorkerExe = Join-Path $WorkerDir 'Kalei.Worker.Builtin.exe'
$Tool = Join-Path $ToolsDir 'Kalei.SandboxCheck.exe'
$LimitMs = 2500          # half of the 5 s handshake timeout: above it the 5 s is revised (ipc-spec, Watchdog)
$MaxFailuresInARow = 3

# ---------------------------------------------------------------------------------------------------- state
function Get-State {
    $s = Read-JsonFile $StatoFile
    if ($s) { return $s }
    return [pscustomobject]@{ schema = 1; fase = 'ambiente'; failures = 0; attempts = 0; inCorso = $false; rebootRequestedUnix = 0; startedUtc = (Get-Date).ToUniversalTime().ToString('o') }
}
function Save-State($s) { Write-JsonAtomic $s $StatoFile }
function Get-UnixNow { return [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() }
function Get-Rows { return @(Read-JsonlRows $Misure) }
function Get-UptimeSeconds {
    if ($FakeUptime -ge 0) { return [double]$FakeUptime }
    return ((Get-Date) - (Get-CimInstance Win32_OperatingSystem).LastBootUpTime).TotalSeconds
}

function Save-RunOnce {
    $cmd = 'powershell.exe -NoProfile -ExecutionPolicy Bypass -File "{0}" -Resume -Cycles {1} -SettleSeconds {2}' -f (Join-Path $Root 'avvia.ps1'), $Cycles, $SettleSeconds
    if ($DataDir -ne $Root) { $cmd += (' -DataDir "{0}"' -f $DataDir) }
    if (-not (Test-Path $RunOnceKey)) { New-Item -Path $RunOnceKey -Force | Out-Null }
    Set-ItemProperty -Path $RunOnceKey -Name 'KaleiSandbox07b' -Value $cmd
}
function Remove-RunOnce { Remove-ItemProperty -Path $RunOnceKey -Name 'KaleiSandbox07b' -ErrorAction SilentlyContinue }

# ---------------------------------------------------------------------------------------------------- one start of the Worker
# Windows PowerShell 5.1 gives a JSON array to the pipeline as ONE object: ConvertFrom-Json | ForEach-Object { $_ } unrolls it.
# Runs "hello" and returns the parsed JSON line, or an object with Ok = false and the reason.
function Invoke-Hello {
    if (-not (Test-Path -LiteralPath $WorkerExe)) { return [pscustomobject]@{ Ok = $false; Detail = 'Worker mancante' } }
    $lines = @(& $Tool hello $WorkerExe $Profile07b)
    $text = ($lines | ForEach-Object { [string]$_ }) -join "`n"
    try { return ($text | ConvertFrom-Json) } catch { return [pscustomobject]@{ Ok = $false; Detail = 'uscita non valida' } }
}

function New-Row([string]$Kind, [int]$N, $Hello, [double]$UptimeS) {
    $row = [ordered]@{ kind = $Kind; n = $N; whenUtc = (Get-Date).ToUniversalTime().ToString('o'); uptimeS = [math]::Round($UptimeS); dryRun = [bool]($DryRun -or $NoReboot -or $FakeUptime -ge 0) }
    foreach ($p in 'Ok', 'Setup', 'Detail', 'PrepareMs', 'StartMs', 'HelloAfterStartMs', 'HelloTotalMs') {
        if ($Hello.PSObject.Properties[$p]) { $row[$p] = $Hello.$p }
    }
    return [pscustomobject]$row
}

# ---------------------------------------------------------------------------------------------------- environment
function Get-EnvInfo {
    $cv = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion'
    $cs = Get-CimInstance Win32_ComputerSystem
    $def = 'non letto'
    try { $m = Get-MpComputerStatus -ErrorAction Stop; $def = ('protezione in tempo reale {0}, servizio {1}' -f $m.RealTimeProtectionEnabled, $m.AMServiceEnabled) } catch { }
    $svc = @{}
    foreach ($name in 'BFE', 'mpssvc') {
        $x = Get-Service -Name $name -ErrorAction SilentlyContinue
        $svc[$name] = $(if ($x) { [string]$x.Status } else { 'assente' })
    }
    $displayVersion = ''
    if ($cv.PSObject.Properties['DisplayVersion']) { $displayVersion = [string]$cv.DisplayVersion }
    return [pscustomobject]@{
        os = [string]$cv.ProductName
        osVersione = $displayVersion
        build = ('{0}.{1}' -f $cv.CurrentBuild, $cv.UBR)
        cpuLogici = [int]$cs.NumberOfLogicalProcessors
        ramGB = [math]::Round($cs.TotalPhysicalMemory / 1GB, 1)
        defender = $def
        serviziFirewall = [pscustomobject]$svc
        psVersione = [string]$PSVersionTable.PSVersion
        elevata = (Test-Elevated)
        dryRun = [bool]$DryRun
    }
}

# ---------------------------------------------------------------------------------------------------- clean-up of what the kit made
function Invoke-Cleanup {
    $result = [pscustomobject]@{ ok = $false; text = 'non eseguita' }
    try {
        $lines = @(& $Tool remove $Profile07b $WorkerDir $ToolsDir)
        $exit = $LASTEXITCODE
        $mappings = 'HKCU:\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Mappings'
        $left = $false
        if (Test-Path $mappings) {
            foreach ($k in Get-ChildItem $mappings) {
                $moniker = [string](Get-ItemProperty $k.PSPath).Moniker
                if ($moniker -like ($Profile07b + '*')) { $left = $true }
            }
        }
        $result = [pscustomobject]@{ ok = (($exit -eq 0) -and (-not $left)); text = (($lines | ForEach-Object { [string]$_ }) -join ' ') }
    } catch { $result = [pscustomobject]@{ ok = $false; text = 'errore durante la pulizia' } }
    return $result
}

# ---------------------------------------------------------------------------------------------------- report
function Get-Stats([double[]]$Values) {
    if ($Values.Count -eq 0) { return $null }
    return [pscustomobject]@{ n = $Values.Count; min = [math]::Round(($Values | Measure-Object -Minimum).Minimum, 1); mediana = [math]::Round((Get-Median $Values), 1); max = [math]::Round(($Values | Measure-Object -Maximum).Maximum, 1) }
}

function Write-Report($Cleanup) {
    $rows = Get-Rows
    $env0 = Read-JsonFile $EnvFile
    $prove = @()
    $proveText = ''
    if (Test-Path -LiteralPath $ProveFile) { $proveText = [IO.File]::ReadAllText($ProveFile) }
    if ($proveText) { try { $prove = @($proveText | ConvertFrom-Json | ForEach-Object { $_ }) } catch { } }
    $first = @($rows | Where-Object { $_.kind -eq 'dopo-copia' -and $_.Ok })
    $cold = @($rows | Where-Object { $_.kind -eq 'freddo' -and $_.Ok })
    $failed = @($rows | Where-Object { -not $_.Ok })
    $all = @($first) + @($cold)
    $afterStart = Get-Stats ([double[]]@($all | ForEach-Object { [double]$_.HelloAfterStartMs }))
    $total = Get-Stats ([double[]]@($all | ForEach-Object { [double]$_.HelloTotalMs }))
    $coldAfterStart = Get-Stats ([double[]]@($cold | ForEach-Object { [double]$_.HelloAfterStartMs }))
    $dry = ($rows | Where-Object { $_.dryRun }).Count -gt 0
    $worst = $(if ($afterStart) { $afterStart.max } else { $null })
    $worstTotal = $(if ($total) { $total.max } else { $null })
    $over = $(if ($null -ne $worst) { ($worst -gt $LimitMs) -or ($worstTotal -gt $LimitMs) } else { $null })
    $counts = @{ PASS = 0; FAIL = 0; INCONCLUSIVE = 0 }
    foreach ($p in $prove) { if ($counts.ContainsKey([string]$p.Verdict)) { $counts[[string]$p.Verdict]++ } }

    $data = [pscustomobject]@{
        schema = 1
        generatoUtc = (Get-Date).ToUniversalTime().ToString('o')
        validita = $(if ($dry) { 'NON VALIDO: prova a secco, misure a caldo' } else { 'valido' })
        ambiente = $env0
        primoAvvioDopoCopia = $(if ($first.Count -gt 0) { $first[0] } else { $null })
        avviiAFreddo = $cold
        tentativiFalliti = $failed
        statisticheHelloDopoStart = $afterStart
        statisticheHelloTotale = $total
        statisticheFreddoDopoStart = $coldAfterStart
        soglia2500ms = [pscustomobject]@{ casoPeggioreDopoStartMs = $worst; casoPeggioreTotaleMs = $worstTotal; superata = $over }
        prove = $prove
        provePassate = $counts.PASS; proveFallite = $counts.FAIL; proveInconcludenti = $counts.INCONCLUSIVE
        pulizia = $Cleanup
    }

    $t = New-Object System.Text.StringBuilder
    [void]$t.AppendLine('KALEI - ticket 07b: prove della sandbox e Hello a freddo del Worker')
    [void]$t.AppendLine('Validita: ' + $data.validita)
    if ($env0) {
        [void]$t.AppendLine(('Macchina: {0} {1} (build {2}), {3} CPU logiche, {4} GB; {5}; BFE {6}, firewall {7}; PowerShell elevata: {8}' -f $env0.os, $env0.osVersione, $env0.build, $env0.cpuLogici, $env0.ramGB, $env0.defender, $env0.serviziFirewall.BFE, $env0.serviziFirewall.mpssvc, $env0.elevata))
    }
    [void]$t.AppendLine('')
    [void]$t.AppendLine(('PROVE DELLA SANDBOX: {0} passate, {1} fallite, {2} inconcludenti (controllo fuori sandbox non riuscito)' -f $counts.PASS, $counts.FAIL, $counts.INCONCLUSIVE))
    foreach ($p in $prove) { [void]$t.AppendLine(('  {0,-13} {1,-20} {2}' -f $p.Verdict, $p.Id, $(if ($p.Inside) { $p.Inside } else { $p.Note }))) }
    [void]$t.AppendLine('')
    [void]$t.AppendLine('HELLO DEL WORKER (Worker pubblicato trimmed, non ReadyToRun), in ms')
    [void]$t.AppendLine('  "dopo Start" = dal ritorno del launcher alla lettura dell''Hello: e'' cio'' che misura il timer di 5 s. "totale" = dalla chiamata a Start (creazione del processo inclusa).')
    if ($first.Count -gt 0) { [void]$t.AppendLine(('  primo avvio dopo la copia: set-up {0} ms, Start {1} ms, dopo Start {2} ms, totale {3} ms' -f $first[0].PrepareMs, $first[0].StartMs, $first[0].HelloAfterStartMs, $first[0].HelloTotalMs)) }
    foreach ($r in $cold) { [void]$t.AppendLine(('  freddo {0,2}: uptime {1,4} s, Start {2,6} ms, dopo Start {3,7} ms, totale {4,7} ms' -f $r.n, $r.uptimeS, $r.StartMs, $r.HelloAfterStartMs, $r.HelloTotalMs)) }
    if ($coldAfterStart) { [void]$t.AppendLine(('  a freddo, dopo Start: {0} avvii, min {1}, mediana {2}, max {3}' -f $coldAfterStart.n, $coldAfterStart.min, $coldAfterStart.mediana, $coldAfterStart.max)) }
    if ($afterStart) { [void]$t.AppendLine(('  tutti (con il primo dopo la copia), dopo Start: min {0}, mediana {1}, max {2}; totale: min {3}, mediana {4}, max {5}' -f $afterStart.min, $afterStart.mediana, $afterStart.max, $total.min, $total.mediana, $total.max)) }
    if ($failed.Count -gt 0) { [void]$t.AppendLine(('  tentativi falliti: {0}' -f $failed.Count)) }
    [void]$t.AppendLine('')
    if ($null -eq $over) { [void]$t.AppendLine('SOGLIA 2,5 s: nessuna misura valida.') }
    elseif ($over) { [void]$t.AppendLine(('SOGLIA 2,5 s: SUPERATA (caso peggiore dopo Start {0} ms, totale {1} ms). Proponi la revisione dei 5 s con questi numeri (ipc-spec, Watchdog).' -f $worst, $worstTotal)) }
    else { [void]$t.AppendLine(('SOGLIA 2,5 s: non superata (caso peggiore dopo Start {0} ms, totale {1} ms): i 5 s restano.' -f $worst, $worstTotal)) }
    [void]$t.AppendLine('')
    [void]$t.AppendLine(('Pulizia (profilo AppContainer e accesso alle cartelle del kit): {0}' -f $(if ($Cleanup.ok) { 'ok' } else { 'NON confermata' })))

    $textOut = Protect-Text $t.ToString() $Root
    $jsonOut = Protect-Text (ConvertTo-Json -InputObject $data -Depth 8) $Root
    if (-not ((Test-TextClean $textOut) -and (Test-TextClean $jsonOut))) {
        Write-Bad 'Il report contiene ancora nome utente, nome del computer o un percorso personale: NON scritto. Dati grezzi in stato\.'
        return $false
    }
    Write-TextFile (Join-Path $Out 'report-07b.txt') $textOut
    Write-TextFile (Join-Path $Out 'report-07b.json') $jsonOut
    Copy-Item -LiteralPath $Misure -Destination (Join-Path $Out 'misure-07b.jsonl') -Force -ErrorAction SilentlyContinue
    Write-Good ('Report scritto in ' + $Out)
    return $true
}

function Copy-ToHost {
    $dest = '\\tsclient\E\KaleiVM-07b\risultati'
    if (Test-Path '\\tsclient\E') {
        try {
            New-Item -ItemType Directory -Force -Path $dest | Out-Null
            Copy-Item -Path (Join-Path $Out '*') -Destination $dest -Force
            Write-Good ('Report copiato nel disco E: del PC (' + $dest + ').')
            return
        } catch { Write-Warn 'Copia automatica non riuscita.' }
    }
    Write-Warn 'Il disco E: del PC non e'' condiviso (serve la sessione avanzata). Copia a mano dalla VM:'
    Write-Info ('  ' + $Out + '\report-07b.json, report-07b.txt, misure-07b.jsonl   ->   E:\KaleiVM-07b\risultati\')
}

function Finish([bool]$Cancelled) {
    Write-Step 'Fine: pulizia e report'
    Remove-RunOnce
    $cleanup = Invoke-Cleanup
    $s = Get-State
    $s.fase = 'fine'
    $s.inCorso = $false
    Save-State $s
    $ok = Write-Report $cleanup
    if ($ok) { Copy-ToHost }
    if (-not $cleanup.ok) { Write-Warn 'La pulizia non e'' confermata: dopo aver messo al sicuro il report torna al checkpoint della VM.' }
    Write-Info 'Quando i file sono sul PC, scrivi a Claude: "Ho il report 07b, leggi E:\KaleiVM-07b\risultati\report-07b.json"'
}

# ---------------------------------------------------------------------------------------------------- main
if (Test-Elevated) { Write-Bad 'Questa PowerShell e'' ELEVATA: chiudila e apri una PowerShell normale.'; exit 1 }
foreach ($f in $WorkerExe, $Tool, (Join-Path $ToolsDir 'Kalei.FakeWorker.exe')) {
    if (-not (Test-Path -LiteralPath $f)) { Write-Bad ('File del kit mancante: ' + (Split-Path -Leaf $f)); exit 1 }
}
if ($Report) { $c = Invoke-Cleanup; [void](Write-Report $c); exit 0 }
if ($Annulla) { Finish $true; exit 0 }

$s = Get-State
Write-Info ('Fase: ' + $s.fase)
while ($true) {
    $s = Get-State
    switch ($s.fase) {
        'ambiente' {
            Write-Step 'Ambiente'
            $envInfo = Get-EnvInfo
            Write-JsonAtomic $envInfo $EnvFile
            $s.fase = 'dopo-copia'; Save-State $s
        }
        'dopo-copia' {
            Write-Step 'Primo avvio del Worker dopo la copia (file nuovi: Defender li scansiona) e set-up della sandbox'
            $s.fase = 'prove'; Save-State $s     # written before: a crash here does not repeat the "first start"
            $h = Invoke-Hello
            Add-JsonlRow (New-Row 'dopo-copia' 0 $h (Get-UptimeSeconds)) $Misure
            if ($h.Ok) { Write-Info ('Hello: dopo Start {0} ms, totale {1} ms' -f $h.HelloAfterStartMs, $h.HelloTotalMs) } else { Write-Warn ('Primo avvio non riuscito: ' + $h.Detail) }
        }
        'prove' {
            Write-Step 'Prove della sandbox (20 prove, ognuna con il controllo fuori sandbox; circa 40 s)'
            $s.fase = 'freddo'; Save-State $s
            $raw = (@(& $Tool proofs $Profile07b) | ForEach-Object { [string]$_ }) -join "`n"
            Write-TextFile $ProveFile $raw
            try { $p = @($raw | ConvertFrom-Json | ForEach-Object { $_ }); Write-Info ('Prove: {0} passate, {1} fallite, {2} inconcludenti' -f @($p | Where-Object { $_.Verdict -eq 'PASS' }).Count, @($p | Where-Object { $_.Verdict -eq 'FAIL' }).Count, @($p | Where-Object { $_.Verdict -eq 'INCONCLUSIVE' }).Count) } catch { Write-Warn 'Uscita delle prove non leggibile.' }
        }
        'freddo' {
            $rows = Get-Rows
            $done = @($rows | Where-Object { $_.kind -eq 'freddo' -and $_.Ok }).Count
            if ($done -ge $Cycles) { $s.fase = 'fine'; Save-State $s; continue }
            if ($s.failures -ge $MaxFailuresInARow -or $s.attempts -ge ($Cycles + 8)) { Write-Warn 'Troppi tentativi senza una misura valida: mi fermo.'; $s.fase = 'fine'; Save-State $s; continue }

            if ($DryRun) {
                Write-Step ('Avvio a caldo {0} di {1} (prova a secco: NON valido)' -f ($done + 1), $Cycles)
                $h = Invoke-Hello
                Add-JsonlRow (New-Row 'freddo' ($done + 1) $h (Get-UptimeSeconds)) $Misure
                $s.attempts++; $s.failures = $(if ($h.Ok) { 0 } else { $s.failures + 1 }); Save-State $s
                continue
            }

            $bootUnix = (Get-UnixNow) - [long](Get-UptimeSeconds)
            if ($s.inCorso -and $bootUnix -ge ($s.rebootRequestedUnix - 5)) {
                # We are after the reboot that was asked for: wait for the desktop to settle, then ONE start.
                $wait = $SettleSeconds - (Get-UptimeSeconds)
                Write-Step ('Avvio a freddo {0} di {1}: assestamento ({2:N0} s ancora)' -f ($done + 1), $Cycles, [math]::Max(0, $wait))
                if ($wait -gt 0) { Start-Sleep -Seconds ([int][math]::Ceiling($wait)) }
                $s.attempts++; $s.inCorso = $false; Save-State $s     # written before the start
                $uptime = Get-UptimeSeconds
                $h = Invoke-Hello
                Add-JsonlRow (New-Row 'freddo' ($done + 1) $h $uptime) $Misure
                if ($h.Ok) { $s.failures = 0; Write-Info ('Hello: dopo Start {0} ms, totale {1} ms (uptime {2:N0} s)' -f $h.HelloAfterStartMs, $h.HelloTotalMs, $uptime) } else { $s.failures++; Write-Warn ('Avvio non riuscito: ' + $h.Detail) }
                Save-State $s
                continue
            }

            # Before the reboot: write the state and the resume entry first, then restart.
            Write-Step ('Riavvio per l''avvio a freddo {0} di {1}' -f ($done + 1), $Cycles)
            $s.inCorso = $true; $s.rebootRequestedUnix = (Get-UnixNow); Save-State $s
            Save-RunOnce
            Write-Info 'La VM si riavvia tra 10 secondi. Dopo il riavvio entra (senza password) e NON toccare nulla: lo script riprende da solo.'
            if ($NoReboot) { Write-Info 'REBOOT (test)'; exit 0 }
            & shutdown.exe /r /t 10 /c 'Kalei 07b: avvio a freddo'
            Start-Sleep -Seconds 60
            exit 0
        }
        default { Finish $false; exit 0 }
    }
}
