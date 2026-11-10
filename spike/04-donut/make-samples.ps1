# Creates small sample files for the W10 drag tests in %TEMP%\KaleiSpike04Samples and prints the folder.
$dir = Join-Path $env:TEMP 'KaleiSpike04Samples'
New-Item -ItemType Directory -Force $dir | Out-Null

# 1x1 PNG
$png = [Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/q842iQAAAABJRU5ErkJggg==')
[IO.File]::WriteAllBytes((Join-Path $dir 'image.png'), $png)
# PNG bytes with a wrong extension (header must win)
[IO.File]::WriteAllBytes((Join-Path $dir 'wrong-extension.jpg'), $png)
[IO.File]::WriteAllText((Join-Path $dir 'table.csv'), "a,b`n1,2`n")
[IO.File]::WriteAllText((Join-Path $dir 'notes.txt'), "hello`n")
[IO.File]::WriteAllText((Join-Path $dir 'mystery.xyz'), "unknown format`n")
Get-ChildItem $dir | Select-Object Name, Length
Write-Host "Samples in $dir"
