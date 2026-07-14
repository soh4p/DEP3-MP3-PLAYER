$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$release = Join-Path $root "release"
$staging = Join-Path $release "staging"
$payload = Join-Path $release "payload"
$sfxDir = Join-Path $root "installer\sfx"
$sevenZip = "C:\Program Files\7-Zip\7z.exe"
$sfxModule = "C:\Program Files\7-Zip\7z.sfx"

if (-not (Test-Path $sevenZip)) {
    throw "7-Zip is required. Install from https://www.7-zip.org/"
}

Write-Host "Publishing DEP3 (framework-dependent)..."
dotnet publish (Join-Path $root "DEP3.csproj") -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=false `
    -o $staging

Write-Host "Building installer payload..."
if (Test-Path $payload) { Remove-Item $payload -Recurse -Force }
New-Item -ItemType Directory -Force -Path $payload | Out-Null
Copy-Item (Join-Path $staging "DEP3.exe") $payload -Force
Copy-Item (Join-Path $sfxDir "install.cmd") $payload -Force
Copy-Item (Join-Path $sfxDir "install.ps1") $payload -Force
Copy-Item (Join-Path $sfxDir "uninstall.cmd") $payload -Force
Copy-Item (Join-Path $sfxDir "uninstall.ps1") $payload -Force

$archive = Join-Path $release "app.7z"
if (Test-Path $archive) { Remove-Item $archive -Force }
& $sevenZip a -t7z -mx=9 -mfb=273 -ms=on $archive (Join-Path $payload "*") | Out-Null

$installer = Join-Path $release "installer.exe"
$config = Join-Path $sfxDir "config.txt"
$bytes = [IO.File]::ReadAllBytes($sfxModule) + [IO.File]::ReadAllBytes($config) + [IO.File]::ReadAllBytes($archive)
[IO.File]::WriteAllBytes($installer, $bytes)

Copy-Item (Join-Path $staging "DEP3.exe") (Join-Path $release "DEP3.exe") -Force

Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $payload -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $archive -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $release "DEP3-Setup.exe") -Force -ErrorAction SilentlyContinue

$installerMb = [math]::Round((Get-Item $installer).Length / 1MB, 2)
$portableMb = [math]::Round((Get-Item (Join-Path $release "DEP3.exe")).Length / 1MB, 2)

Write-Host ""
Write-Host "Done."
Write-Host "  Installer: $installer ($installerMb MB)"
Write-Host "  Portable:  $(Join-Path $release 'DEP3.exe') ($portableMb MB)"
Write-Host ""
Write-Host "Note: requires .NET 8 Desktop Runtime on the target PC."

if ($installerMb -gt 25) {
    Write-Warning "Installer is larger than the 25 MB GitHub target."
}
