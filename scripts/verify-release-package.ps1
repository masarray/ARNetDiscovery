param(
  [Parameter(Mandatory = $true)]
  [string]$ZipPath,
  [string]$ChecksumPath = 'artifacts/release/SHA256SUMS.txt',
  [string]$ExpectedExe = 'ARNetDiscovery.exe'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $ZipPath)) { throw "ZIP package not found: $ZipPath" }
if (-not (Test-Path $ChecksumPath)) { throw "Checksum file not found: $ChecksumPath" }

$zipItem = Get-Item $ZipPath
if ($zipItem.Length -lt 1024) { throw "ZIP package is unexpectedly small: $($zipItem.Length) bytes" }

$hash = Get-FileHash -Algorithm SHA256 $ZipPath
$checksumText = Get-Content $ChecksumPath -Raw
if ($checksumText -notmatch [regex]::Escape($hash.Hash)) {
  throw "SHA256SUMS.txt does not contain the calculated hash for $ZipPath"
}

$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("arnet-release-check-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $temp | Out-Null

try {
  Expand-Archive -Path $ZipPath -DestinationPath $temp -Force

  $required = @(
    $ExpectedExe,
    'README.txt',
    'LICENSE.txt',
    'Quick Start.pdf',
    'User Manual.pdf',
    'docs/QUICK_START.md',
    'docs/USER_MANUAL.md'
  )

  foreach ($file in $required) {
    $path = Join-Path $temp $file
    if (-not (Test-Path $path)) { throw "Required package file missing: $file" }
  }

  $batFiles = Get-ChildItem -Path $temp -Recurse -File -Filter '*.bat'
  if ($batFiles.Count -gt 0) { throw "Release package must not include .bat launcher files." }

  $exe = Get-ChildItem -Path $temp -Recurse -File -Filter $ExpectedExe | Select-Object -First 1
  if ($exe.Length -lt 1024) { throw "$ExpectedExe is unexpectedly small." }

  Write-Host "Release package verified successfully." -ForegroundColor Green
  Write-Host "ZIP: $ZipPath"
  Write-Host "SHA256: $($hash.Hash)"
}
finally {
  if (Test-Path $temp) { Remove-Item -Recurse -Force $temp }
}
