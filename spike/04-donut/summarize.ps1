# Summarises a latency log and its .drops.csv for one tag (median, max, share of runs at or under 100 ms).
# Statistics use only the first drag-enter of each process (the cold one). Re-entries in the same process are listed apart.
# Old logs (latency.csv, no 'enter' column): the first row of each run id counts as the cold one.
param(
    [Parameter(Mandatory)][string]$Tag,
    [string]$Log = (Join-Path $env:LOCALAPPDATA 'KaleiSpike04\latency-v2.csv'),
    [double]$LimitMs = 100
)
function Median($v) { $s = @($v | Sort-Object); if ($s.Count -eq 0) { return [double]::NaN }; $m = [int][math]::Floor($s.Count / 2); if ($s.Count % 2) { $s[$m] } else { ($s[$m - 1] + $s[$m]) / 2 } }
$all = @(Import-Csv $Log | Where-Object { $_.tag -eq $Tag })
if ($all.Count -eq 0) { Write-Host "No rows for tag '$Tag' in $Log"; return }
if ($all[0].PSObject.Properties.Name -contains 'enter') {
    $rows = @($all | Where-Object { $_.enter -eq '1' })
} else {
    $seen = @{}
    $rows = @($all | Where-Object { if ($seen.ContainsKey($_.run)) { $false } else { $seen[$_.run] = $true; $true } })
}
$reentries = $all.Count - $rows.Count
$ok = @($rows | Where-Object { $_.flag -eq 'ok' })
$noFrame = $rows.Count - $ok.Count
$inv = [Globalization.CultureInfo]::InvariantCulture
$f2 = @($ok | ForEach-Object { [double]::Parse($_.frame2Ms, $inv) })
$f1 = @($ok | ForEach-Object { [double]::Parse($_.frame1Ms, $inv) })
$within = @($f2 | Where-Object { $_ -le $LimitMs }).Count
$dropLog = [IO.Path]::ChangeExtension($Log, '.drops.csv')
$d = @()
if (Test-Path $dropLog) {
    $d = @(Get-Content $dropLog | ForEach-Object { $c = $_ -split ','; [pscustomobject]@{ run = $c[0]; tag = $c[2]; effect = $c[3]; unchanged = $c[4] } } | Where-Object { $_.tag -eq $Tag })
}
$runsSeen = @($d | ForEach-Object { $_.run } | Sort-Object -Unique).Count
"Tag: $Tag  processes with a drop: $runsSeen  cold rows: $($rows.Count) (valid $($ok.Count), no-frame $noFrame)  re-entries excluded: $reentries"
"Overlay visible in layout (frame1): median {0:N1} ms  max {1:N1} ms" -f (Median $f1), ($f1 | Measure-Object -Maximum).Maximum
"First frame presented  (frame2): median {0:N1} ms  max {1:N1} ms  within {2} ms: {3}/{4}" -f (Median $f2), ($f2 | Measure-Object -Maximum).Maximum, $LimitMs, $within, $f2.Count
"Scaling values seen: " + (($rows | ForEach-Object { $_.renderScaling } | Sort-Object -Unique) -join ', ')
"Screens seen (idx:scaling): " + (($rows | ForEach-Object { "$($_.screenIdx):$($_.screenScaling)" } | Sort-Object -Unique) -join ', ')
"Window straddling a screen edge in " + @($rows | Where-Object { $_.windowStraddles -eq 'True' }).Count + " rows; ring inside window in " + @($rows | Where-Object { $_.ringInsideWindow -eq 'True' }).Count + " rows"
"Formats known at drag-enter: " + (($rows | ForEach-Object { $_.formats } | Sort-Object -Unique) -join ' | ') + "   header sniff: " + (($rows | ForEach-Object { $_.headerFormat } | Sort-Object -Unique) -join ' | ')
if ($d.Count -gt 0) {
    "Drops: $($d.Count)  effect Copy: $(@($d | Where-Object { $_.effect -eq 'Copy' }).Count)  source unchanged: $(@($d | Where-Object { $_.unchanged -eq 'True' }).Count)"
}
