# Contributing to ARNet Discovery

Thanks for considering a contribution. ARNet Discovery is intended to be a lightweight, practical engineering tool for LAN discovery, target-list verification, and protocol evidence collection.

## Contribution areas

Good contributions include:

- bug fixes;
- safer scan behavior;
- better device/protocol classification;
- target-list import improvements;
- UI clarity and accessibility improvements;
- documentation and field examples;
- deterministic tests for core logic;
- export/report improvements.

## Development principles

Please keep the tool:

- safe for engineering LANs;
- responsive during scans;
- clear about what is evidence and what is confirmed;
- portable on Windows;
- free from unnecessary heavy dependencies;
- compatible with the Apache-2.0 project direction.

## Before submitting changes

Recommended local checks:

```bat
dotnet restore ARNetDiscovery.sln
dotnet build ARNetDiscovery.sln -c Release
```

If tests are added:

```bat
dotnet test ARNetDiscovery.sln -c Release
```

## Pull request guidance

A good pull request should explain:

- what problem it solves;
- how the change behaves in the app;
- how it was tested;
- whether scan behavior, protocol probing, or exported evidence changed.

For scanner changes, include safe timeout/cancellation behavior and route exceptions to diagnostics.

## Documentation changes

When changing user-visible behavior, update the relevant documentation:

- `README.md`
- `docs/USER_MANUAL.md`
- `docs/ARCHITECTURE.md`
- `docs/ROADMAP.md`

