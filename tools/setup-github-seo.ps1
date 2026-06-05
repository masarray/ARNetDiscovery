# Configure GitHub repository metadata for ARNet Discovery.
# Requires GitHub CLI: https://cli.github.com/
# Run from any PowerShell terminal after `gh auth login`.

$Repo = "masarray/ARNetDiscovery"
$Description = "Portable Windows LAN discovery and relay IP scanner for substation automation, FAT, commissioning, and protocol evidence checks."
$Homepage = "https://masarray.github.io/ARNetDiscovery/"

$Topics = @(
  "substation-automation",
  "network-discovery",
  "lan-scanner",
  "relay-testing",
  "iec61850",
  "iec60870-5-104",
  "modbus-tcp",
  "dnp3",
  "opcua",
  "scada",
  "industrial-automation",
  "wpf",
  "dotnet",
  "windows",
  "fat-testing",
  "commissioning",
  "protection-relay",
  "engineering-tools"
)

$Arguments = @("repo", "edit", $Repo, "--description", $Description, "--homepage", $Homepage)
foreach ($Topic in $Topics) {
  $Arguments += "--add-topic"
  $Arguments += $Topic
}

gh @Arguments

Write-Host "GitHub metadata updated for $Repo" -ForegroundColor Green
