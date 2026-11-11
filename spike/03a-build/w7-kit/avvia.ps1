# Kalei - W7 cold-start breakdown on the reference VM (ticket 03c, issue #56). Windows PowerShell 5.1, NOT elevated. ASCII only.
# Usage (normal PowerShell, from the kit folder copied to C:\KaleiW7):
#     Set-ExecutionPolicy -Scope Process Bypass
#     .\avvia.ps1
# It resumes by itself after every reboot. Other modes:  .\avvia.ps1 -Report   |   .\avvia.ps1 -Annulla
#
# Design rules (lessons of the kit of #47):
#  - the state is written BEFORE every step that can fail or reboot the machine;
#  - measurements are only appended to stato\misure.jsonl (one JSON object per line); progress is recomputed by counting rows;
#  - nothing deletes stato\ or risultati\, at the entry of a phase or anywhere else;
#  - lines are read with [IO.File]::ReadAllLines and cast to [string] (never Get-Content into ConvertTo-Json).
param(
    [switch]$Resume,        # used by the automatic resume (RunOnce) after each reboot
    [switch]$Annulla,       # stop the measures and have the temporary auto-logon removed
    [switch]$Report,        # only write the report from the data collected so far
    [switch]$Ridotto,       # only for the very first start: rounds 6-10 without variant c (45 starts instead of 50); kept in the state
    [switch]$DryRun,        # host test only: no auto-logon, no waiting, no reboot; the measures are warm and NOT valid
    [int]$CrashAt = 0,      # test only (with -DryRun): throws after the state of measure number N was written
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
$Marker = Join-Path $State 'fine.txt'
$RunOnceKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\RunOnce'
$AllVariants = @('a', 'b', 'c', 'd', 'e', 'f')
$CoreVariants = @('a', 'b', 'c', 'd')
$ThresholdMs = 1000
$SettleSeconds = 150
$MaxExtraAttempts = 8

$VariantText = @{
    a = 'baseline di #47 (ReadyToRun + trimming) con i marcatori'
    b = 'finestra subito con contenuto minimo; vista completa dopo il secondo frame'
    c = 'avvio snello: senza Avalonia.Desktop, font predefinito esplicito, ComboBox riempita dopo il secondo frame'
    d = 'opzioni del runtime: ReadyToRun composite, TieredPGO off, GC non concorrente, chiavi di risorsa di sistema'
    e = 'rendering software (senza ANGLE/EGL): diagnostica del costo della GPU sulla VM'
    f = 'combinazione di b + c + d'
}
$MarkOrder = @('runtime', 'main', 'builderReady', 'platformServices', 'appInitStart', 'appInitEnd', 'afterSetup', 'frameworkReady', 'windowCtorEnd', 'firstLayout', 'opened', 'frame1', 'frame2', 'fullContent')
$MarkText = @{
    runtime = 'processo creato -> primo codice gestito (host + avvio CLR)'
    main = 'ingresso in Main'
    builderReady = 'AppBuilder configurato'
    platformServices = 'servizi di piattaforma Avalonia/Skia pronti'
    appInitStart = 'inizio App.Initialize'
    appInitEnd = 'tema Fluent caricato (fine App.Initialize)'
    afterSetup = 'Avalonia inizializzata (fine Setup)'
    frameworkReady = 'OnFrameworkInitializationCompleted'
    windowCtorEnd = 'finestra creata'
    firstLayout = 'primo layout'
    opened = 'finestra aperta (Opened)'
    frame1 = 'primo frame (inizio)'
    frame2 = 'secondo frame = metrica W7'
    fullContent = 'contenuto completo mostrato (solo b, c, f)'
}

# ---------------------------------------------------------------------------------------------------- schedule
# 50 cold starts: rounds 1-5 all six variants, rounds 6-10 variants a-d. The order rotates every round, so no variant
# always runs first or last (drift of the VM, caches, Defender).
function New-Schedule([bool]$Reduced) {
    $list = New-Object System.Collections.ArrayList
    $n = 0
    for ($r = 1; $r -le 10; $r++) {
        $set = $(if ($r -le 5) { $AllVariants } elseif ($Reduced) { @("a", "b", "d") } else { $CoreVariants })
        $k = ($r - 1) % $set.Count
        for ($j = 0; $j -lt $set.Count; $j++) {
            $n++
            [void]$list.Add([pscustomobject]@{ n = $n; round = $r; variant = $set[($j + $k) % $set.Count] })
        }
    }
    return @($list)
}

# ---------------------------------------------------------------------------------------------------- state
function Get-State {
    $s = Read-JsonFile $StatoFile
    if ($s) { return $s }
    return [pscustomobject]@{ schema = 1; fase = 'ambiente'; autologon = $false; attempts = 0; inCorso = $null; rebootRequestedUnix = 0; ridotto = [bool]$Ridotto; startedUtc = (Get-Date).ToUniversalTime().ToString('o') }
}
function Save-State($s) { Write-JsonAtomic $s $StatoFile }
function Get-UnixNow { return [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() }
function Get-Rows { return @(Read-JsonlRows $Misure) }

function Save-RunOnce {
    $cmd = 'powershell.exe -NoProfile -ExecutionPolicy Bypass -File "{0}" -Resume' -f (Join-Path $Root 'avvia.ps1')
    if ($DataDir -ne $Root) { $cmd += (' -DataDir "{0}"' -f $DataDir) }
    if (-not (Test-Path $RunOnceKey)) { New-Item -Path $RunOnceKey -Force | Out-Null }
    Set-ItemProperty -Path $RunOnceKey -Name 'KaleiW7' -Value $cmd
}
function Remove-RunOnce { Remove-ItemProperty -Path $RunOnceKey -Name 'KaleiW7' -ErrorAction SilentlyContinue }

# ---------------------------------------------------------------------------------------------------- one start
# Starts a variant with --out, waits for it, returns the parsed JSON line of the app (marks, assemblies, CPU...) or the failure.
function Invoke-Start([string]$Variant) {
    $exe = Join-Path $Root ('varianti\' + $Variant + '\AotWindow.exe')
    if (-not (Test-Path -LiteralPath $exe)) { return [pscustomobject]@{ ok = $false; reason = 'eseguibile mancante' } }
    $f = Join-Path $env:TEMP ('kalei-w7-' + [guid]::NewGuid().ToString('N') + '.json')
    $p = Start-Process -FilePath $exe -ArgumentList '--out', ('"' + $f + '"') -PassThru
    if (-not $p.WaitForExit(30000)) { try { $p.Kill() } catch { }; return [pscustomobject]@{ ok = $false; reason = 'timeout 30 s' } }
    if (-not (Test-Path -LiteralPath $f)) { return [pscustomobject]@{ ok = $false; reason = ('nessun output, codice ' + $p.ExitCode) } }
    $text = [IO.File]::ReadAllText($f)
    Remove-Item -LiteralPath $f -Force -ErrorAction SilentlyContinue
    try { $j = $text | ConvertFrom-Json } catch { return [pscustomobject]@{ ok = $false; reason = 'JSON non valido' } }
    if (-not $j.marks -or $null -eq $j.marks.frame2) { return [pscustomobject]@{ ok = $false; reason = 'manca frame2' } }
    return [pscustomobject]@{ ok = $true; json = $j }
}

function New-Row([string]$Kind, $Item, [int]$Attempt, [double]$UptimeS, $Json, [string]$Reason) {
    $row = [ordered]@{ kind = $Kind; whenUtc = (Get-Date).ToUniversalTime().ToString('o'); attempt = $Attempt; uptimeS = [math]::Round($UptimeS) }
    if ($Item) { $row.n = $Item.n; $row.round = $Item.round; $row.variant = $Item.variant }
    if ($Json) {
        $row.frame2Ms = [double]$Json.marks.frame2
        $row.app = $Json
        if ($Json.variant) { $row.variant = [string]$Json.variant }
    }
    if ($Reason) { $row.reason = $Reason }
    return [pscustomobject]$row
}

# ---------------------------------------------------------------------------------------------------- phases
function Get-EnvInfo {
    $cv = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion'
    $cs = Get-CimInstance Win32_ComputerSystem
    $av = @()
    try { $av = @(Get-CimInstance -Namespace 'root/SecurityCenter2' -ClassName AntiVirusProduct -ErrorAction Stop | ForEach-Object { [string]$_.displayName }) } catch { }
    $def = 'non letto'
    try { $m = Get-MpComputerStatus -ErrorAction Stop; $def = ('protezione in tempo reale {0}, servizio {1}' -f $m.RealTimeProtectionEnabled, $m.AMServiceEnabled) } catch { }
    $displayVersion = ''
    if ($cv.PSObject.Properties['DisplayVersion']) { $displayVersion = [string]$cv.DisplayVersion }
    return [pscustomobject]@{
        windows = [pscustomobject]@{ product = [string]$cv.ProductName; displayVersion = $displayVersion; build = [string]$cv.CurrentBuild; ubr = [string]$cv.UBR }
        hardware = [pscustomobject]@{ logicalCpus = $cs.NumberOfLogicalProcessors; ramGB = [math]::Round($cs.TotalPhysicalMemory / 1GB, 1); manufacturer = [string]$cs.Manufacturer; model = [string]$cs.Model }
        elevated = (Test-Elevated)
        powershell = $PSVersionTable.PSVersion.ToString()
        antivirus = $av
        defender = $def
        dryRun = [bool]$DryRun
    }
}

function Invoke-EnvPhase($st) {
    if (Test-Path -LiteralPath $EnvFile) { return }
    Write-Step 'Ambiente'
    $info = Get-EnvInfo
    Write-Info ('Windows: {0} build {1}.{2} ({3})' -f $info.windows.product, $info.windows.build, $info.windows.ubr, $info.windows.displayVersion)
    Write-Info ('CPU logiche: {0}   RAM: {1} GB   {2} / {3}' -f $info.hardware.logicalCpus, $info.hardware.ramGB, $info.hardware.manufacturer, $info.hardware.model)
    Write-Info ('PowerShell elevata: {0}   Antivirus: {1}   Defender: {2}' -f $info.elevated, $(if ($info.antivirus.Count -eq 0) { 'nessuno registrato' } else { $info.antivirus -join ', ' }), $info.defender)
    if ($DryRun) { Write-Warn 'DRY RUN: niente accesso automatico, niente attese, niente riavvii; le misure sono a caldo e NON valide per W7.'; Write-JsonFile $info $EnvFile; return }
    if ($info.windows.build -ne '19045') { Write-Warn ('build {0}: la VM di riferimento dovrebbe essere Windows 10 22H2 (build 19045).' -f $info.windows.build) }

    Write-Step 'Accesso automatico temporaneo'
    Write-Info 'Le misure fanno fino a circa 60 riavvii. Dopo ogni riavvio Windows si ferma alla schermata di accesso.'
    Write-Info 'Se attivo l''accesso automatico ti chiedo la password UNA volta (finestra UAC), la verifico e la salvo solo come segreto LSA cifrato'
    Write-Info '(mai in chiaro nel registro). Un task di sistema lo rimuove da solo a fine misure e comunque entro 6 ore.'
    $a = Read-Host 'Attivo l''accesso automatico? (S = si; N = no, inserirai tu la password a ogni riavvio)'
    if ($a -match '^[sS]') {
        # State first: if the elevated script dies halfway, the tasks it registered clean themselves up at the deadline.
        $st.autologon = $false
        Save-State $st
        $p = Start-Process -FilePath 'powershell.exe' -Verb RunAs -Wait -PassThru -ArgumentList @(
            '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + (Join-Path $Root 'lib\autologon-setup.ps1') + '"'),
            '-Root', ('"' + $DataDir + '"'), '-User', ('"' + $env:USERNAME + '"'))
        if ($p.ExitCode -eq 0 -and (Test-Path (Join-Path $State 'autologon-setup.json'))) {
            $st.autologon = $true
            Write-Good 'Accesso automatico attivo.'
        } else {
            Write-Warn 'Accesso automatico NON attivato: dopo ogni riavvio dovrai inserire tu la password.'
        }
        Save-State $st
    } else {
        Write-Info 'Va bene: accesso automatico non attivato, ti serviranno molti inserimenti della password.'
    }
    $info | Add-Member -NotePropertyName autologon -NotePropertyValue ([bool]$st.autologon) -Force
    Write-JsonFile $info $EnvFile
}

# One unmeasured start of every variant, before the first reboot: the first start of a new file triggers the antivirus scan,
# which would inflate the first counted run of each variant. Rows are kept (kind "warmup") but never counted.
function Invoke-WarmupPhase($st) {
    $done = @{}
    foreach ($r in (Get-Rows)) { if ($r.kind -eq 'warmup' -and $r.variant) { $done[[string]$r.variant] = $true } }
    $todo = @($AllVariants | Where-Object { -not $done.ContainsKey($_) })
    if ($todo.Count -eq 0) { return }
    Write-Step ('Riscaldamento: un avvio non contato per ogni variante ({0} da fare)' -f $todo.Count)
    $st.fase = 'warmup'
    Save-State $st
    foreach ($v in $todo) {
        $res = Invoke-Start $v
        if ($res.ok) {
            Add-JsonlRow (New-Row 'warmup' $null 0 0 $res.json '') $Misure
            Write-Info ('variante {0}: ok ({1:N0} ms, a caldo)' -f $v, $res.json.marks.frame2)
        } else {
            Add-JsonlRow ([pscustomobject]@{ kind = 'warmup-fail'; variant = $v; reason = $res.reason; whenUtc = (Get-Date).ToUniversalTime().ToString('o') }) $Misure
            throw ('La variante {0} non parte ({1}). Controlla la copia del kit (cartella varianti\{0}).' -f $v, $res.reason)
        }
    }
}

function Complete-Run($st, [string]$Status) {
    $st.fase = $Status
    if ($st.inCorso) { Add-JsonlRow ([pscustomobject]@{ kind = "interrotta"; n = $st.inCorso.n; variant = $st.inCorso.variant; attempt = $st.inCorso.attempt; whenUtc = (Get-Date).ToUniversalTime().ToString("o") }) $Misure }
    $st.inCorso = $null
    Save-State $st
    Remove-RunOnce
    Write-TextFile $Marker $Status      # lets the SYSTEM task remove the temporary auto-logon
    if ($st.autologon -and -not $DryRun) {
        Write-Info 'Attendo la rimozione dell''accesso automatico (task di sistema, fino a 3 minuti)...'
        $f = Join-Path $State 'autologon-cleanup.json'
        for ($i = 0; $i -lt 36; $i++) { if (Test-Path -LiteralPath $f) { break }; Start-Sleep -Seconds 5 }
        $c = Read-JsonFile $f
        if ($c -and $c.ok) { Write-Good 'Accesso automatico rimosso e verificato.' }
        else { Write-Warn 'Rimozione dell''accesso automatico NON confermata: dopo aver salvato il report torna al checkpoint "aggiornato".' }
    }
    Write-Report
}

function Invoke-Measures($st) {
    $plan = New-Schedule ([bool]$st.ridotto)
    $limit = $plan.Count + $MaxExtraAttempts
    $st.fase = 'misure'
    Save-State $st
    # A measure that was started but has no result (crash, power loss, reboot in the middle) is recorded, not lost silently.
    if ($st.inCorso) {
        Add-JsonlRow ([pscustomobject]@{ kind = 'interrotta'; n = $st.inCorso.n; variant = $st.inCorso.variant; attempt = $st.inCorso.attempt; whenUtc = (Get-Date).ToUniversalTime().ToString('o') }) $Misure
        $st.inCorso = $null
        Save-State $st
    }
    while ($true) {
        $rows = Get-Rows
        $done = @{}
        foreach ($r in $rows) { if ($r.kind -eq 'cold') { $done[[int]$r.n] = $true } }
        $next = $plan | Where-Object { -not $done.ContainsKey([int]$_.n) } | Select-Object -First 1
        if (-not $next) { break }

        $trail = 0
        foreach ($r in ($rows | Where-Object { $_.kind -in 'cold', 'fail', 'interrotta' })) { if ($r.kind -eq 'cold') { $trail = 0 } else { $trail++ } }
        if ($trail -ge 3) { Write-Bad 'Tre tentativi di fila senza una misura valida: mi fermo.'; Complete-Run $st 'interrotto'; return }
        if ([int]$st.attempts -ge $limit) { Write-Bad 'Troppi tentativi: mi fermo con le misure raccolte.'; Complete-Run $st 'interrotto'; return }

        $boot = (Get-CimInstance Win32_OperatingSystem).LastBootUpTime
        $bootUnix = (New-Object DateTimeOffset($boot)).ToUnixTimeSeconds()
        $upS = ((Get-Date) - $boot).TotalSeconds
        $rebooted = ([long]$st.rebootRequestedUnix -gt 0) -and ($bootUnix -gt [long]$st.rebootRequestedUnix) -and ($upS -lt 1800)
        if ($DryRun -or $rebooted) {
            if (-not $DryRun -and $upS -lt $SettleSeconds) {
                $wait = [int][math]::Ceiling($SettleSeconds - $upS)
                Write-Info ('Attendo {0} s dall''avvio perche'' il desktop si assesti (uptime {1:N0} s)...' -f $wait, $upS)
                Start-Sleep -Seconds $wait
                $upS = ((Get-Date) - $boot).TotalSeconds
            }
            $st.attempts = [int]$st.attempts + 1
            $st.inCorso = [pscustomobject]@{ n = $next.n; variant = $next.variant; attempt = $st.attempts; startUnix = (Get-UnixNow) }
            Save-State $st                                   # BEFORE the step that can fail
            if ($CrashAt -gt 0 -and $next.n -eq $CrashAt -and $DryRun) { throw ('CrashAt: crash simulato alla misura {0}' -f $CrashAt) }
            $res = Invoke-Start $next.variant
            if ($res.ok) {
                Add-JsonlRow (New-Row 'cold' $next $st.attempts $upS $res.json '') $Misure
                Write-Good ('Avvio {0} di {1} (round {2}, variante {3}): {4:N0} ms' -f $next.n, $plan.Count, $next.round, $next.variant, $res.json.marks.frame2)
            } else {
                Add-JsonlRow (New-Row 'fail' $next $st.attempts $upS $null $res.reason) $Misure
                Write-Warn ('Misura {0} (variante {1}) fallita: {2}. La ripeto dopo un nuovo riavvio.' -f $next.n, $next.variant, $res.reason)
            }
            $st.inCorso = $null
            Save-State $st
            # all measures there? then no further reboot is needed
            if (@(Get-Rows | Where-Object { $_.kind -eq 'cold' }).Count -ge $plan.Count) { break }
        }
        if ($DryRun) { continue }
        # reboot for the next cold start
        $st.rebootRequestedUnix = (Get-UnixNow)
        Save-State $st                                       # BEFORE the reboot
        Save-RunOnce
        Write-Info ('Riavvio tra 10 secondi. Dopo il riavvio lo script riparte da solo{0}.' -f $(if ($st.autologon) { '' } else { ' appena inserisci la password' }))
        Start-Sleep -Seconds 10
        Restart-Computer -Force
        Start-Sleep -Seconds 120
        throw 'Il riavvio non e'' partito.'
    }
    Complete-Run $st 'completo'
}

# ---------------------------------------------------------------------------------------------------- report
function F0($x) { if ($null -eq $x) { return '-' }; return ([math]::Round([double]$x)).ToString('F0', $inv) }
function F1($x) { if ($null -eq $x) { return '-' }; return ([math]::Round([double]$x, 1)).ToString('F1', $inv) }
function MaxOf([double[]]$v) { if ($v.Count -eq 0) { return $null }; return ($v | Measure-Object -Maximum).Maximum }
function MinOf([double[]]$v) { if ($v.Count -eq 0) { return $null }; return ($v | Measure-Object -Minimum).Minimum }

function Get-Mark($row, [string]$Name) {
    $p = $row.app.marks.PSObject.Properties[$Name]
    if ($p) { return [double]$p.Value }
    return $null
}

function Get-VariantSummary([string]$V, $cold) {
    $rows = @($cold | Where-Object { $_.variant -eq $V } | Sort-Object { [int]$_.n })
    $s = [ordered]@{ variant = $V; description = $VariantText[$V]; runs = $rows.Count }
    if ($rows.Count -eq 0) { return [pscustomobject]$s }
    $f2 = @($rows | ForEach-Object { [double]$_.frame2Ms })
    $ready = @($rows | ForEach-Object { $fc = Get-Mark $_ 'fullContent'; if ($null -ne $fc) { $fc } else { [double]$_.frame2Ms } })
    $s.frame2 = [pscustomobject]@{ medianMs = (Get-Median $f2); minMs = (MinOf $f2); maxMs = (MaxOf $f2); underThreshold = @($f2 | Where-Object { $_ -le $ThresholdMs }).Count; valuesInOrder = @($f2 | ForEach-Object { [math]::Round($_, 1) }) }
    $s.readyMedianMs = (Get-Median $ready)
    $s.readyMaxMs = (MaxOf $ready)
    $early = @($rows | Where-Object { [int]$_.round -le 5 } | ForEach-Object { [double]$_.frame2Ms })
    $s.frame2MedianRounds1to5 = $(if ($early.Count -gt 0) { Get-Median $early } else { $null })
    $s.firstCountedRunMs = $f2[0]
    $s.slowestRunIsFirst = ($f2[0] -eq (MaxOf $f2))
    $s.lateLogon = @($rows | Where-Object { [double]$_.uptimeS -gt 240 }).Count

    $phases = @()
    $prev = $null
    foreach ($m in $MarkOrder) {
        $cum = @(); $delta = @()
        foreach ($r in $rows) {
            $t = Get-Mark $r $m
            if ($null -eq $t) { continue }
            $cum += $t
            $pt = $(if ($prev) { Get-Mark $r $prev } else { 0.0 })
            if ($null -ne $pt) { $delta += ($t - $pt) }
        }
        if ($cum.Count -gt 0) {
            $phases += [pscustomobject]@{ mark = $m; text = $MarkText[$m]; runs = $cum.Count; cumMedianMs = (Get-Median $cum); cumMaxMs = (MaxOf $cum); stepMedianMs = (Get-Median $delta); stepMaxMs = (MaxOf $delta) }
            $prev = $m
        }
    }
    $s.phases = $phases

    function MedOf($rows, [scriptblock]$Pick) { $v = @($rows | ForEach-Object { & $Pick $_ } | Where-Object { $null -ne $_ } | ForEach-Object { [double]$_ }); if ($v.Count -eq 0) { return $null }; return (Get-Median $v) }
    $s.cpuUserMedianMs = MedOf $rows { param($r) $r.app.cpuUserMs }
    $s.cpuKernelMedianMs = MedOf $rows { param($r) $r.app.cpuKernelMs }
    $s.readMBMedian = MedOf $rows { param($r) if ($null -ne $r.app.readBytes) { [double]$r.app.readBytes / 1048576.0 } }
    $s.peakWorkingSetMBMedian = MedOf $rows { param($r) $r.app.peakWorkingSetMB }
    $s.assemblyCount = [int]$rows[0].app.assemblyCount
    $s.assemblies = @($rows[0].app.assemblies | ForEach-Object { [string]$_ })
    return [pscustomobject]$s
}

function Write-Report {
    $rows = Get-Rows
    $cold = @($rows | Where-Object { $_.kind -eq 'cold' })
    $envI = Read-JsonFile $EnvFile
    $st = Read-JsonFile $StatoFile
    $cleanup = Read-JsonFile (Join-Path $State 'autologon-cleanup.json')
    $plan = New-Schedule ([bool]$st.ridotto)
    $summ = @(); foreach ($v in $AllVariants) { $summ += (Get-VariantSummary $v $cold) }
    $base = $summ | Where-Object { $_.variant -eq 'a' }

    $L = New-Object System.Collections.ArrayList
    function Add-L([string]$t) { [void]$L.Add($t) }
    Add-L 'KALEI - W7 AVVIO A FREDDO, DOVE VA IL TEMPO (ticket 03c, issue #56)'
    Add-L ('Stato: {0}. Avvii a freddo validi: {1} di {2}. Tentativi: {3}. Avvii falliti: {4}; interrotti: {5}.' -f $(if ($st) { $st.fase } else { '?' }), $cold.Count, $plan.Count, $(if ($st) { $st.attempts } else { '?' }), @($rows | Where-Object { $_.kind -eq 'fail' }).Count, @($rows | Where-Object { $_.kind -eq 'interrotta' }).Count)
    if ($DryRun -or ($envI -and $envI.dryRun)) { Add-L 'ATTENZIONE: DRY RUN sul PC host, misure a caldo, NON valide per W7.' }
    if ($envI) {
        Add-L ('Ambiente: {0} build {1}.{2} ({3}); CPU logiche {4}, RAM {5} GB; {6} / {7}; antivirus: {8}; Defender: {9}; PowerShell elevata: {10}.' -f $envI.windows.product, $envI.windows.build, $envI.windows.ubr, $envI.windows.displayVersion, $envI.hardware.logicalCpus, $envI.hardware.ramGB, $envI.hardware.manufacturer, $envI.hardware.model, $(if (@($envI.antivirus).Count -eq 0) { 'nessuno registrato' } else { @($envI.antivirus) -join ', ' }), $envI.defender, $envI.elevated)
    }
    Add-L ('Metodo: riavvio prima di ogni avvio, {0} s di assestamento dall''avvio, una misura, riavvio; un avvio di riscaldamento non contato per variante; ordine ruotato a ogni round. Soglia W7: {1} ms (secondo frame).' -f $SettleSeconds, $ThresholdMs)
    Add-L 'Tempi in ms dalla creazione del processo (kernel). "runtime" = primo codice gestito: comprende apphost, hostfxr, hostpolicy e avvio del CLR.'
    Add-L ''
    Add-L 'SINTESI (mediana del secondo frame, soglia 1000 ms)'
    Add-L ('{0,-3}{1,6}{2,10}{3,8}{4,8}{5,10}{6,12}{7,12}' -f 'var', 'avvii', 'mediana', 'min', 'max', '<=1000', 'vs a (ms)', 'pronta*')
    foreach ($x in $summ) {
        if ($x.runs -eq 0) { Add-L ('{0,-3}{1,6}   nessun avvio' -f $x.variant, 0); continue }
        $ref = $(if ($x.runs -lt 10 -and $base -and $null -ne $base.frame2MedianRounds1to5) { $base.frame2MedianRounds1to5 } elseif ($base -and $base.frame2) { $base.frame2.medianMs } else { $null })
        $d = $(if ($null -ne $ref) { F0 ($x.frame2.medianMs - $ref) } else { '-' })
        $r15 = $(if ($x.runs -lt 10) { ' (vs a round 1-5)' } else { '' })
        Add-L ('{0,-3}{1,6}{2,10}{3,8}{4,8}{5,10}{6,12}{7,12}{8}' -f $x.variant, $x.runs, (F0 $x.frame2.medianMs), (F0 $x.frame2.minMs), (F0 $x.frame2.maxMs), ('{0}/{1}' -f $x.frame2.underThreshold, $x.runs), $d, (F0 $x.readyMedianMs), $r15)
    }
    Add-L '* pronta = mediana del frame col contenuto completo (b, c, f: "fullContent"; le altre: uguale al secondo frame).'
    Add-L ''
    foreach ($x in $summ) {
        Add-L ('VARIANTE {0}: {1}' -f $x.variant.ToUpper(), $x.description)
        if ($x.runs -eq 0) { Add-L '  nessun avvio a freddo registrato'; Add-L ''; continue }
        Add-L ('  secondo frame: mediana {0}, min {1}, max {2}; sotto soglia {3} di {4}; valori in ordine di esecuzione: {5}' -f (F0 $x.frame2.medianMs), (F0 $x.frame2.minMs), (F0 $x.frame2.maxMs), $x.frame2.underThreshold, $x.runs, ((@($x.frame2.valuesInOrder) | ForEach-Object { F0 $_ }) -join ' '))
        Add-L ('  primo avvio contato: {0} ms; e'' il piu'' lento: {1}; avvii con accesso tardivo (uptime > 240 s): {2}' -f (F0 $x.firstCountedRunMs), $(if ($x.slowestRunIsFirst) { 'si' } else { 'no' }), $x.lateLogon)
        Add-L ('  {0,-18}{1,11}{2,11}{3,11}{4,11}  {5}' -f 'fase', 'da t0 med', 'da t0 max', 'passo med', 'passo max', 'cosa')
        foreach ($p in $x.phases) {
            Add-L ('  {0,-18}{1,11}{2,11}{3,11}{4,11}  {5}' -f $p.mark, (F0 $p.cumMedianMs), (F0 $p.cumMaxMs), (F0 $p.stepMedianMs), (F0 $p.stepMaxMs), $p.text)
        }
        Add-L ('  CPU utente/kernel (mediana): {0}/{1} ms; ReadFile (senza i file mappati in memoria): {2} MB; working set di picco: {3} MB; assembly caricati al secondo frame: {4}' -f (F0 $x.cpuUserMedianMs), (F0 $x.cpuKernelMedianMs), (F1 $x.readMBMedian), (F1 $x.peakWorkingSetMBMedian), $x.assemblyCount)
        if ($base -and $base.runs -gt 0 -and $x.variant -ne 'a') {
            $plus = @($x.assemblies | Where-Object { $base.assemblies -notcontains $_ })
            $minus = @($base.assemblies | Where-Object { $x.assemblies -notcontains $_ })
            Add-L ('  assembly rispetto ad a: in piu'' [{0}]; in meno [{1}]' -f ($plus -join ', '), ($minus -join ', '))
        }
        Add-L ''
    }
    Add-L 'ACCESSO AUTOMATICO'
    if ($cleanup) { Add-L ('  pulizia: {0} ({1})' -f $(if ($cleanup.ok) { 'ok' } else { 'NON confermata' }), (@($cleanup.notes) -join '; ')) }
    elseif ($st -and $st.autologon) { Add-L '  attivato; la pulizia non e'' ancora confermata (si completa a fine misure).' }
    else { Add-L '  non attivato.' }
    Add-L ''
    Add-L 'Il report non decide: la scelta fra AOT (ticket 38), soglia rivista e ottimizzazioni resta al titolare.'

    $txt = Protect-Text (($L -join "`r`n") + "`r`n") $DataDir

    $runsOut = @()
    foreach ($r in ($cold | Sort-Object { [int]$_.n })) {
        $marks = [ordered]@{}
        foreach ($p in $r.app.marks.PSObject.Properties) { $marks[$p.Name] = [double]$p.Value }
        $runsOut += [pscustomobject][ordered]@{ n = [int]$r.n; round = [int]$r.round; variant = [string]$r.variant; uptimeS = $r.uptimeS; marks = [pscustomobject]$marks; cpuUserMs = $r.app.cpuUserMs; cpuKernelMs = $r.app.cpuKernelMs; readBytes = $r.app.readBytes; peakWorkingSetMB = $r.app.peakWorkingSetMB; assemblyCount = $r.app.assemblyCount }
    }
    $obj = [pscustomobject]@{
        ticket = '03c (issue 56)'; thresholdMs = $ThresholdMs; settleSeconds = $SettleSeconds; environment = $envI
        state = $(if ($st) { [pscustomobject]@{ fase = $st.fase; attempts = $st.attempts; autologon = $st.autologon } } else { $null })
        autologonCleanup = $cleanup; dryRun = [bool]$DryRun
        variants = $summ; runs = $runsOut
        rowsFailed = @($rows | Where-Object { $_.kind -eq 'fail' } | ForEach-Object { [pscustomobject]@{ n = $_.n; variant = $_.variant; reason = $_.reason } })
        rowsInterrupted = @($rows | Where-Object { $_.kind -eq 'interrotta' }).Count
    }
    $json = Protect-Text (ConvertTo-Json -InputObject $obj -Depth 8) $DataDir
    $rawText = $(if (Test-Path -LiteralPath $Misure) { [IO.File]::ReadAllText($Misure) } else { '' })

    if (-not (Test-TextClean $txt) -or -not (Test-TextClean $json)) {
        Write-Bad 'La sanificazione non e'' riuscita (nel report c''e'' ancora un nome utente, di PC o un percorso personale): NON scrivo il report.'
        return
    }
    $txtFile = Join-Path $Out 'report-w7.txt'
    $jsonFile = Join-Path $Out 'report-w7.json'
    Write-TextFile $txtFile $txt
    Write-TextFile $jsonFile $json
    Write-Good ('Report scritto: {0} e report-w7.json' -f $txtFile)

    # copy outside the VM (the checkpoint restore wipes everything inside); the raw rows hold only numbers, variant names and assembly names
    $dest = '\\tsclient\E\KaleiVM-W7\risultati'
    try {
        if (Test-Path -LiteralPath $dest) {
            Copy-Item -LiteralPath $txtFile, $jsonFile -Destination $dest -Force
            if ((Test-TextClean $rawText) -and $rawText) { Copy-Item -LiteralPath $Misure -Destination (Join-Path $dest 'misure-w7.jsonl') -Force }
            Write-Good 'Copiati sul disco E: del PC (E:\KaleiVM-W7\risultati).'
        } else { Write-Warn 'Disco E: del PC non raggiungibile: copia a mano risultati\report-w7.* e stato\misure.jsonl in E:\KaleiVM-W7\risultati\ PRIMA di tornare al checkpoint.' }
    } catch { Write-Warn ('Copia sul PC non riuscita: ' + $_.Exception.Message + '. Copia a mano risultati\report-w7.* e stato\misure.jsonl.') }
}

# ---------------------------------------------------------------------------------------------------- main
if ($Report) { Write-Report; return }
if ($Annulla) {
    $st = Get-State
    Complete-Run $st 'interrotto'
    Write-Good 'Misure fermate. Il task di sistema rimuove l''accesso automatico entro 1-2 minuti. I dati raccolti sono in stato\misure.jsonl e nel report.'
    return
}
if (-not $DryRun -and (Test-Elevated)) { throw 'Questa PowerShell e'' ELEVATA: chiudila e apri una PowerShell normale.' }
$st = Get-State
if ($st.fase -in 'completo', 'interrotto') { Write-Info ('Le misure sono gia'' in stato "{0}". Uso -Report per rigenerare il report.' -f $st.fase); Write-Report; return }
if ($Resume) { Write-Info 'Ripresa dopo il riavvio.' }
Invoke-EnvPhase $st
Invoke-WarmupPhase $st
Invoke-Measures $st
