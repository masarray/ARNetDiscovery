param(
  [string]$AppName = 'ARNetDiscovery',
  [string]$ProjectPath = 'src/ARNetDiscovery.Wpf/ARNetDiscovery.Wpf.csproj',
  [string]$Version = 'v0.0.0-local',
  [string]$Configuration = 'Release',
  [string]$Runtime = 'win-x64',
  [string]$OutputRoot = 'artifacts',
  [bool]$SelfContained = $true,
  [switch]$Clean
)

$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
  $current = (Get-Location).Path
  while ($current) {
    if (Test-Path (Join-Path $current 'ARNetDiscovery.sln')) { return $current }
    $parent = Split-Path -Parent $current
    if ($parent -eq $current) { break }
    $current = $parent
  }
  throw 'Repository root with ARNetDiscovery.sln was not found. Run this script from inside the repository.'
}

function Get-AssemblyVersion([string]$ReleaseVersion) {
  $clean = $ReleaseVersion.TrimStart('v')
  $match = [regex]::Match($clean, '^(\d+)\.(\d+)\.(\d+)')
  if (-not $match.Success) { throw "Version must start with vX.Y.Z or X.Y.Z. Actual: $ReleaseVersion" }
  return $match.Value
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
  throw 'dotnet SDK was not found. Install .NET 8 SDK before publishing.'
}

if (-not (Test-Path $ProjectPath)) {
  throw "Project file not found: $ProjectPath"
}

if ($Version -notmatch '^v?\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$') {
  throw "Version must look like v2.16.0 or v2.16.0-public-beta. Actual: $Version"
}

if (-not $Version.StartsWith('v')) { $Version = "v$Version" }
$assemblyVersion = Get-AssemblyVersion $Version

$publishDir = Join-Path $OutputRoot "publish\$Runtime\app"
$packageRoot = Join-Path $OutputRoot "package\$AppName-$Version-$Runtime-portable"
$releaseDir = Join-Path $OutputRoot 'release'
$zipName = "$AppName-$Version-$Runtime-portable.zip"
$zipPath = Join-Path $releaseDir $zipName
$checksumPath = Join-Path $releaseDir 'SHA256SUMS.txt'

if ($Clean) {
  foreach ($path in @($publishDir, $packageRoot, $releaseDir)) {
    if (Test-Path $path) { Remove-Item -Recurse -Force $path }
  }
}

New-Item -ItemType Directory -Force -Path $publishDir, $packageRoot, $releaseDir | Out-Null

Write-Host "Publishing $AppName $Version for $Runtime..." -ForegroundColor Cyan
$selfContainedValue = if ($SelfContained) { 'true' } else { 'false' }

dotnet publish $ProjectPath `
  --configuration $Configuration `
  --runtime $Runtime `
  --self-contained $selfContainedValue `
  --output $publishDir `
  /p:Version=$($Version.TrimStart('v')) `
  /p:AssemblyVersion=$assemblyVersion `
  /p:FileVersion=$assemblyVersion `
  /p:InformationalVersion=$($Version.TrimStart('v')) `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:IncludeAllContentForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true `
  /p:DebugType=None `
  /p:DebugSymbols=false

Copy-Item -Path (Join-Path $publishDir '*') -Destination $packageRoot -Recurse -Force

$readmeText = @"
ARNet Discovery $Version - Windows portable package

Run
1. Extract this ZIP to a local folder.
2. Run ARNetDiscovery.exe.
3. Select the Ethernet adapter connected to the engineering LAN.
4. Use Scan Local, Probe, or Import Excel + Scan List.
5. Export CSV when you need evidence for FAT, SAT, commissioning, or troubleshooting records.

Included documentation
- Quick Start.pdf
- User Manual.pdf
- docs/QUICK_START.md
- docs/USER_MANUAL.md

Notes
- No installer is required.
- No subscription or license key is required.
- Use the tool only on networks where you are authorized to test.
- Treat open ports and protocol labels as engineering evidence, not final device identity proof.

Project
https://github.com/masarray/ARNetDiscovery
"@
$readmeText | Set-Content -Path (Join-Path $packageRoot 'README.txt') -Encoding UTF8

foreach ($pair in @(
  @{ Source='LICENSE'; Destination='LICENSE.txt' },
  @{ Source='NOTICE'; Destination='NOTICE.txt' },
  @{ Source='THIRD_PARTY_NOTICES.md'; Destination='THIRD_PARTY_NOTICES.md' }
)) {
  if (Test-Path $pair.Source) { Copy-Item $pair.Source -Destination (Join-Path $packageRoot $pair.Destination) -Force }
}

New-Item -ItemType Directory -Force -Path (Join-Path $packageRoot 'docs') | Out-Null
foreach ($file in @('docs/QUICK_START.md','docs/USER_MANUAL.md','docs/QUICK_START.pdf','docs/USER_MANUAL.pdf')) {
  if (Test-Path $file) {
    Copy-Item $file -Destination (Join-Path $packageRoot $file) -Force
  }
}
if (Test-Path 'docs/QUICK_START.pdf') { Copy-Item 'docs/QUICK_START.pdf' -Destination (Join-Path $packageRoot 'Quick Start.pdf') -Force }
if (Test-Path 'docs/USER_MANUAL.pdf') { Copy-Item 'docs/USER_MANUAL.pdf' -Destination (Join-Path $packageRoot 'User Manual.pdf') -Force }

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $zipPath -Force

$hash = Get-FileHash -Algorithm SHA256 $zipPath
"$($hash.Hash)  $zipName" | Set-Content -Path $checksumPath -Encoding ASCII

Write-Host "Portable package created:" -ForegroundColor Green
Write-Host "  $zipPath"
Write-Host "  $checksumPath"
