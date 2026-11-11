# Helpers for avvia.ps1 and the auto-logon scripts. Windows PowerShell 5.1. ASCII only.
# Rules learnt from the kit of #47 (lessons in the vault):
#  - never ConvertTo-Json lines read with Get-Content (PS 5.1 adds PSPath/PSDrive and the expansion never ends): read with
#    [IO.File]::ReadAllLines and cast to [string];
#  - raw data is only ever appended; nothing here deletes it.

function Write-Step([string]$Message) { Write-Host ''; Write-Host ('=== ' + $Message) -ForegroundColor Cyan }
function Write-Info([string]$Message) { Write-Host ('  ' + $Message) }
function Write-Warn([string]$Message) { Write-Host ('  ATTENZIONE: ' + $Message) -ForegroundColor Yellow }
function Write-Bad([string]$Message) { Write-Host ('  ERRORE: ' + $Message) -ForegroundColor Red }
function Write-Good([string]$Message) { Write-Host ('  ' + $Message) -ForegroundColor Green }

function Test-Elevated {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    return ([Security.Principal.WindowsPrincipal]$id).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-Median([double[]]$Values) {
    $s = @($Values | Sort-Object)
    if ($s.Count -eq 0) { return $null }
    $m = [int][math]::Floor($s.Count / 2)
    if ($s.Count % 2 -eq 1) { return $s[$m] }
    return ($s[$m - 1] + $s[$m]) / 2
}

function Write-TextFile([string]$Path, [string]$Text) {
    [IO.File]::WriteAllText($Path, $Text, (New-Object Text.UTF8Encoding($false)))
}

function Write-JsonFile($Object, [string]$Path) {
    Write-TextFile $Path (ConvertTo-Json -InputObject $Object -Depth 8)
}

# Whole-file JSON read (one string, no Get-Content). Returns $null when the file is missing or empty.
function Read-JsonFile([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $text = [IO.File]::ReadAllText($Path)
    if ([string]::IsNullOrWhiteSpace($text)) { return $null }
    return ($text | ConvertFrom-Json)
}

# State file: written to a temp file and moved over the old one, so a crash never leaves a half-written state.
function Write-JsonAtomic($Object, [string]$Path) {
    $tmp = $Path + '.tmp'
    Write-TextFile $tmp (ConvertTo-Json -InputObject $Object -Depth 8)
    Move-Item -LiteralPath $tmp -Destination $Path -Force
}

# Append-only record file: one JSON object per line. Rows are only added, never rewritten or removed.
function Add-JsonlRow($Object, [string]$Path) {
    $line = (ConvertTo-Json -InputObject $Object -Depth 8 -Compress) + "`r`n"
    [IO.File]::AppendAllText($Path, $line, (New-Object Text.UTF8Encoding($false)))
}

# Reads every row of a .jsonl file. Lines are cast to [string] BEFORE parsing; a damaged line is skipped, not fatal.
function Read-JsonlRows([string]$Path) {
    $rows = New-Object System.Collections.ArrayList
    if (-not (Test-Path -LiteralPath $Path)) { return @() }
    foreach ($raw in [IO.File]::ReadAllLines($Path)) {
        $line = [string]$raw
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        try { [void]$rows.Add(($line | ConvertFrom-Json)) } catch { }
    }
    return @($rows)
}

# Removes personal data from any text that goes into a report: user paths, user name, computer name, kit folder.
function Protect-Text([string]$Text, [string]$Root) {
    if ([string]::IsNullOrEmpty($Text)) { return $Text }
    $t = [regex]::Replace($Text, '[A-Za-z]:\\{1,2}Users\\{1,2}[^\\"\s]+', '<PERCORSO-UTENTE>')
    if ($Root) {
        $t = [regex]::Replace($t, [regex]::Escape($Root.Replace('\', '\\')), '<CARTELLA-KIT>', 'IgnoreCase')
        $t = [regex]::Replace($t, [regex]::Escape($Root), '<CARTELLA-KIT>', 'IgnoreCase')
    }
    foreach ($pair in @(@($env:USERNAME, '<UTENTE>'), @($env:COMPUTERNAME, '<PC>'))) {
        $name = $pair[0]
        if ($name -and $name.Length -ge 3) {
            $t = [regex]::Replace($t, ('(?<![A-Za-z0-9])' + [regex]::Escape($name) + '(?![A-Za-z0-9])'), $pair[1], 'IgnoreCase')
        }
    }
    return $t
}

# True when no user path, user name or computer name is left in the text.
function Test-TextClean([string]$Text) {
    if ([regex]::IsMatch($Text, '[A-Za-z]:\\{1,2}Users\\{1,2}(?!<)')) { return $false }
    foreach ($name in @($env:USERNAME, $env:COMPUTERNAME)) {
        if ($name -and $name.Length -ge 3 -and [regex]::IsMatch($Text, ('(?<![A-Za-z0-9])' + [regex]::Escape($name) + '(?![A-Za-z0-9])'), 'IgnoreCase')) { return $false }
    }
    return $true
}
