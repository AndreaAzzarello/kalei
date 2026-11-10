# Builds a folder that can be copied to the reference VM and run there without the .NET SDK.
# Usage (PowerShell, from the repository root):  .\spike\01-sandbox\build-package.ps1
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$pkg = Join-Path $root 'artifacts\package'
if (Test-Path $pkg) { Remove-Item $pkg -Recurse -Force }
dotnet publish (Join-Path $root 'Worker') -c Release -o (Join-Path $pkg 'worker') --nologo -v q
dotnet publish (Join-Path $root 'Launcher') -c Release -r win-x64 --self-contained -o $pkg --nologo -v q
@'
# Run on the target machine from this folder:  .\run-proofs.ps1
# Needs internet for the positive controls (TCP/UDP/DNS must work OUTSIDE the sandbox to prove anything inside it).
# Creates and removes a per-user AppContainer profile and the folder %LOCALAPPDATA%\KaleiSpike. No administrator rights.
.\SandboxSpike.Launcher.exe proofs --worker "$PSScriptRoot\worker" --out "$PSScriptRoot\report.json"
'@ | Set-Content -Encoding utf8 (Join-Path $pkg 'run-proofs.ps1')
Write-Host "Package ready: $pkg"
