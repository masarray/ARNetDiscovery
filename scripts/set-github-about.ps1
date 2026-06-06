[CmdletBinding(SupportsShouldProcess = $true)]
param(
  [string]$Owner = 'masarray',
  [string]$Repo = 'ARNetDiscovery',
  [string]$HomepageUrl = 'https://masarray.github.io/ARNetDiscovery/',
  [string]$Description = 'Portable Windows LAN discovery and relay IP scanner for substation automation, FAT, commissioning, and protocol evidence checks.',
  [string[]]$Topics = @(
    'substation-automation',
    'network-discovery',
    'lan-scanner',
    'relay-testing',
    'iec61850',
    'iec60870-5-104',
    'modbus-tcp',
    'dnp3',
    'opcua',
    'scada',
    'industrial-automation',
    'wpf',
    'dotnet',
    'windows',
    'fat-testing',
    'commissioning',
    'protection-relay',
    'engineering-tools'
  ),
  [bool]$EnableIssues = $true,
  [bool]$DisableWiki = $true,
  [bool]$DisableProjects = $true,
  [bool]$DisableDiscussions = $true
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
  throw 'GitHub CLI was not found. Install GitHub CLI first: https://cli.github.com/'
}

& gh auth status --hostname github.com *> $null
if ($LASTEXITCODE -ne 0) {
  throw 'GitHub CLI is not authenticated. Run: gh auth login'
}

$fullName = "$Owner/$Repo"
$topicsCsv = ($Topics | ForEach-Object { $_.Trim().ToLowerInvariant() } | Where-Object { $_ } | Sort-Object -Unique) -join ','

Write-Host ''
Write-Host 'GitHub About panel preview' -ForegroundColor Cyan
Write-Host "Repository : $fullName"
Write-Host "Description: $Description"
Write-Host "Homepage   : $HomepageUrl"
Write-Host "Topics     : $topicsCsv"
Write-Host "Issues     : $EnableIssues"
Write-Host "Wiki       : $(-not $DisableWiki)"
Write-Host "Projects   : $(-not $DisableProjects)"
Write-Host "Discussions: $(-not $DisableDiscussions)"
Write-Host ''

$help = (& gh repo edit --help) -join "`n"
$args = @('repo', 'edit', $fullName, '--description', $Description, '--homepage', $HomepageUrl)

foreach ($topic in ($topicsCsv -split ',')) {
  if ($topic) {
    $args += '--add-topic'
    $args += $topic
  }
}

if ($help -match '--enable-issues') { $args += "--enable-issues=$($EnableIssues.ToString().ToLowerInvariant())" }
if ($help -match '--enable-wiki') { $args += "--enable-wiki=$(((-not $DisableWiki).ToString()).ToLowerInvariant())" }
if ($help -match '--enable-projects') { $args += "--enable-projects=$(((-not $DisableProjects).ToString()).ToLowerInvariant())" }
if ($help -match '--enable-discussions') { $args += "--enable-discussions=$(((-not $DisableDiscussions).ToString()).ToLowerInvariant())" }

Write-Host 'Command preview:' -ForegroundColor Cyan
Write-Host ('gh ' + ($args | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }) -join ' ')
Write-Host ''

if ($PSCmdlet.ShouldProcess($fullName, 'Update GitHub repository description, homepage, topics, and selected features')) {
  & gh @args
  if ($LASTEXITCODE -ne 0) { throw "gh repo edit failed with exit code $LASTEXITCODE" }
  Write-Host "GitHub metadata updated for $fullName" -ForegroundColor Green
}
