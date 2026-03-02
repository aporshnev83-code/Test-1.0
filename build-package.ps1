[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Platform = "Any CPU"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$solution = Join-Path $root "Transaq.NinjaTraderAdapter.sln"
$adapterProject = Join-Path $root "Transaq.NinjaTraderAdapter/Transaq.NinjaTraderAdapter.csproj"
$packageRoot = Join-Path $root "NinjaTraderPackage"
$customDir = Join-Path $packageRoot "bin/Custom"
$configDir = Join-Path $packageRoot "config"
$distDir = Join-Path $root "dist"

New-Item -ItemType Directory -Force -Path $customDir, $configDir, $distDir | Out-Null

Write-Host "==> Building ($Configuration|$Platform)"

$msbuild = Get-Command msbuild -ErrorAction SilentlyContinue
if ($msbuild) {
    & $msbuild.Path $solution "/t:Restore;Build" "/p:Configuration=$Configuration" "/p:Platform=$Platform"
}
else {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        throw "Neither msbuild nor dotnet was found in PATH. Install Visual Studio Build Tools or .NET SDK on Windows."
    }

    & dotnet restore $solution
    & dotnet build $solution -c $Configuration
}

$dllPath = Join-Path $root "Transaq.NinjaTraderAdapter/bin/$Configuration/net48/Transaq.NinjaTraderAdapter.dll"
if (-not (Test-Path $dllPath)) {
    throw "Built DLL not found: $dllPath"
}

Write-Host "==> Copying artifacts"
Copy-Item $dllPath (Join-Path $customDir "Transaq.NinjaTraderAdapter.dll") -Force

$configPath = Join-Path $root "Transaq.NinjaTraderAdapter/config.json"
if (Test-Path $configPath) {
    Copy-Item $configPath (Join-Path $configDir "config.json") -Force
}

$addonZip = Join-Path $distDir "Transaq.NinjaTraderAdapter.NT8.AddOn.zip"
$portableZip = Join-Path $distDir "Transaq.NinjaTraderAdapter.Release.zip"

if (Test-Path $addonZip) { Remove-Item $addonZip -Force }
if (Test-Path $portableZip) { Remove-Item $portableZip -Force }

Write-Host "==> Creating NT8 Add-On package"
Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $addonZip -CompressionLevel Optimal

Write-Host "==> Creating portable release zip"
$portableItems = @(
    (Join-Path $customDir "Transaq.NinjaTraderAdapter.dll"),
    (Join-Path $root "NinjaTraderPackage/readme.txt")
)
if (Test-Path (Join-Path $configDir "config.json")) {
    $portableItems += (Join-Path $configDir "config.json")
}
Compress-Archive -Path $portableItems -DestinationPath $portableZip -CompressionLevel Optimal

Write-Host "==> Done"
Write-Host "Add-On package: $addonZip"
Write-Host "Portable release: $portableZip"
