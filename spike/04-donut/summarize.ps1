# Summarises latency.csv and latency.drops.csv for one tag (median, max, share of runs at or under 100 ms).
param(
    [Parameter(Mandatory)][string]$Tag,
    [string]$Log = (Join-Path $env:LOCALAPPDATA 'KaleiSpike04\latency.csv'),
    [double]$LimitMs = 100
)
function Median($v) { $s = @($v | Sort-Object); if ($s.Count -eq 0) { return [double]::NaN }; $m = [int][math]::Floor($s.Count / 2); if ($s.Count % 2) { $s[$m] } else { ($s[$m - 1] + $s[$m]) / 2 } }
$rows = @(Import-Csv $Log | Where-Object { $_.tag -eq $Tag })
if ($rows.Count -eq 0) { Write-Host "No rows for tag '$Tag' in $Log"; return }
$ok = @($rows | Where-Object { $_.flag -eq 'ok' })
$noFrame = @($rows | Where-Object { $_.flag -ne 'ok' }).Count
$f2 = @($ok | ForEach-Object { [double]::Parse($_.frame2Ms, [Globalization.CultureInfo]::InvariantCulture) })
$f1 = @($ok | ForEach-Object { [double]::Parse($_.frame1Ms, [Globalization.CultureInfo]::InvariantCulture) })
$within = @($f2 | Where-Object { $_ -le $LimitMs }).Count
"Tag: $Tag  rows: $($rows.Count)  no-frame: $noFrame"
"Overlay visible in layout (frame1): median {0:N1} ms  max {1:N1} ms" -f (Median $f1), ($f1 | Measure-Object -Maximum).Maximum
"First frame presented  (frame2): median {0:N1} ms  max {1:N1} ms  within {2} ms: {3}/{4}" -f (Median $f2), ($f2 | Measure-Object -Maximum).Maximum, $LimitMs, $within, $f2.Count
"Scaling values seen: " + (($rows | ForEach-Object { $_.renderScaling } | Sort-Object -Unique) -join ', ')
"Screens seen (idx:scaling): " + (($rows | ForEach-Object { "$($_.screenIdx):$($_.screenScaling)" } | Sort-Object -Unique) -join ', ')
"Window straddling a screen edge in " + @($rows | Where-Object { $_.windowStraddles -eq 'True' }).Count + " rows; ring inside window in " + @($rows | Where-Object { $_.ringInsideWindow -eq 'True' }).Count + " rows"
"Formats known at drag-enter: " + (($rows | ForEach-Object { $_.formats } | Sort-Object -Unique) -join ' | ') + "   header sniff: " + (($rows | ForEach-Object { $_.headerFormat } | Sort-Object -Unique) -join ' | ')
$dropLog = [IO.Path]::ChangeExtension($Log, '.drops.csv')
if (Test-Path $dropLog) {
    $d = @(Get-Content $dropLog | ForEach-Object { $c = $_ -split ','; [pscustomobject]@{ tag = $c[2]; effect = $c[3]; unchanged = $c[4] } } | Where-Object { $_.tag -eq $Tag })
    "Drops: $($d.Count)  effect Copy: $(@($d | Where-Object { $_.effect -eq 'Copy' }).Count)  source unchanged: $(@($d | Where-Object { $_.unchanged -eq 'True' }).Count)"
}
