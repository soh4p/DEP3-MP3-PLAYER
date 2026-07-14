$ErrorActionPreference = "Stop"

function Test-DotNetDesktop8 {
    try {
        $runtimes = & dotnet --list-runtimes 2>$null
        return $runtimes -match "Microsoft\.WindowsDesktop\.App 8\."
    }
    catch {
        return $false
    }
}

function New-Shortcut($path, $target) {
    $dir = Split-Path $path -Parent
    if ($dir -and -not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    $shell = New-Object -ComObject WScript.Shell
    $link = $shell.CreateShortcut($path)
    $link.TargetPath = $target
    $link.WorkingDirectory = Split-Path $target -Parent
    $link.IconLocation = "$target,0"
    $link.Description = "DEP3 Music Player"
    $link.Save()
}

$sourceDir = $PSScriptRoot
$installDir = Join-Path $env:LOCALAPPDATA "Programs\DEP3"
$appPath = Join-Path $installDir "DEP3.exe"
$uninstallPath = Join-Path $installDir "uninstall.cmd"

if (-not (Test-DotNetDesktop8)) {
    Add-Type -AssemblyName System.Windows.Forms
    $answer = [System.Windows.Forms.MessageBox]::Show(
        ".NET 8 Desktop Runtime is required.`n`nOpen the download page now?",
        "DEP3 Setup",
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Information)

    if ($answer -eq [System.Windows.Forms.DialogResult]::Yes) {
        Start-Process "https://dotnet.microsoft.com/en-us/download/dotnet/8.0"
    }
    exit 1
}

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item (Join-Path $sourceDir "DEP3.exe") $appPath -Force
Copy-Item (Join-Path $sourceDir "uninstall.ps1") (Join-Path $installDir "uninstall.ps1") -Force
Copy-Item (Join-Path $sourceDir "uninstall.cmd") $uninstallPath -Force

$programs = [Environment]::GetFolderPath("Programs")
New-Shortcut (Join-Path $programs "DEP3.lnk") $appPath
New-Shortcut (Join-Path ([Environment]::GetFolderPath("Desktop")) "DEP3.lnk") $appPath

$uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\DEP3"
New-Item -Path $uninstallKey -Force | Out-Null
Set-ItemProperty -Path $uninstallKey -Name DisplayName -Value "DEP3"
Set-ItemProperty -Path $uninstallKey -Name Publisher -Value "Gim4"
Set-ItemProperty -Path $uninstallKey -Name DisplayIcon -Value $appPath
Set-ItemProperty -Path $uninstallKey -Name DisplayVersion -Value "1.0.0"
Set-ItemProperty -Path $uninstallKey -Name InstallLocation -Value $installDir
Set-ItemProperty -Path $uninstallKey -Name UninstallString -Value "`"$uninstallPath`""
Set-ItemProperty -Path $uninstallKey -Name NoModify -Value 1 -Type DWord
Set-ItemProperty -Path $uninstallKey -Name NoRepair -Value 1 -Type DWord

Add-Type -AssemblyName System.Windows.Forms
$launch = [System.Windows.Forms.MessageBox]::Show(
    "DEP3 was installed.`n`n$installDir`n`nLaunch DEP3 now?",
    "DEP3 Setup",
    [System.Windows.Forms.MessageBoxButtons]::YesNo,
    [System.Windows.Forms.MessageBoxIcon]::Information)

if ($launch -eq [System.Windows.Forms.DialogResult]::Yes) {
    Start-Process $appPath
}
