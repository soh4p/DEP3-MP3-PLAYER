$ErrorActionPreference = "SilentlyContinue"

$installDir = $PSScriptRoot

Add-Type -AssemblyName System.Windows.Forms
$answer = [System.Windows.Forms.MessageBox]::Show(
    "Remove DEP3 from this computer?",
    "Uninstall DEP3",
    [System.Windows.Forms.MessageBoxButtons]::YesNo,
    [System.Windows.Forms.MessageBoxIcon]::Question,
    [System.Windows.Forms.MessageBoxDefaultButton]::Button2)

if ($answer -ne [System.Windows.Forms.DialogResult]::Yes) {
    exit 0
}

Get-Process DEP3 -ErrorAction SilentlyContinue | Stop-Process -Force

$desktop = [Environment]::GetFolderPath("Desktop")
$programs = [Environment]::GetFolderPath("Programs")
Remove-Item (Join-Path $desktop "DEP3.lnk") -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $programs "DEP3.lnk") -Force -ErrorAction SilentlyContinue
Remove-Item "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\DEP3" -Recurse -Force -ErrorAction SilentlyContinue

$cleanup = Join-Path $env:TEMP ("dep3-uninstall-" + [guid]::NewGuid().ToString("N") + ".cmd")
@"
@echo off
timeout /t 2 /nobreak >nul
rd /s /q "$installDir"
del "%~f0"
"@ | Set-Content -Path $cleanup -Encoding ASCII

Start-Process "cmd.exe" -ArgumentList "/c `"$cleanup`"" -WindowStyle Hidden

[System.Windows.Forms.MessageBox]::Show(
    "DEP3 was removed.",
    "Uninstall DEP3",
    [System.Windows.Forms.MessageBoxButtons]::OK,
    [System.Windows.Forms.MessageBoxIcon]::Information)
