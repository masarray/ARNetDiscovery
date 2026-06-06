# Release Packaging

ARNet Discovery releases are built as portable Windows x64 packages for users who want to try the app without installing Visual Studio.

## Package contents

A release ZIP contains:

```text
ARNetDiscovery.exe
Quick Start.pdf
User Manual.pdf
README.txt
LICENSE.txt
NOTICE.txt
THIRD_PARTY_NOTICES.md
docs/QUICK_START.md
docs/USER_MANUAL.md
```

The release assets also include:

```text
SHA256SUMS.txt
```

## Create a package locally

From the repository root:

```powershell
PowerShell -ExecutionPolicy Bypass -File .\scripts\publish-windows-portable.ps1 -Version v2.16.0 -Clean
```

Verify it:

```powershell
PowerShell -ExecutionPolicy Bypass -File .\scripts\verify-release-package.ps1 `
  -ZipPath .\artifacts\release\ARNetDiscovery-v2.16.0-win-x64-portable.zip `
  -ChecksumPath .\artifacts\release\SHA256SUMS.txt
```

## Create a package with GitHub Actions

Open:

```text
Actions -> release-package -> Run workflow
```

Inputs:

| Input | Meaning |
|---|---|
| version | Release version such as `v2.16.0` or `v2.16.0-public-beta` |
| publish_release | When true, create a GitHub Release and upload the ZIP |
| prerelease | Mark the release as a prerelease |
| draft | Create the release as a draft |
| release_notes_file | Markdown file used as the GitHub Release body |

Tag push also works:

```powershell
git tag v2.16.0
git push origin v2.16.0
```

Tag releases publish automatically. Manual workflow runs can either publish a release or only create downloadable workflow artifacts.
