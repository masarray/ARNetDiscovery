# ARNet Discovery Development Rules

## Core rules

- Keep scan logic in `ARNetDiscovery.Core`, never in WPF code-behind.
- WPF is only presentation and orchestration.
- All probe exceptions must be routed to diagnostics.
- Do not let a single bad IP, blocked port, DNS issue, ARP issue, or socket exception crash the app.
- Prefer immutable scan result records for snapshots.
- Do not introduce heavy UI libraries unless there is a strong reason.
- Do not embed third-party OUI databases unless license/redistribution is verified.

## UI rules

- No default-looking DataGrid-first UX.
- Center experience is a visual discovery canvas with compact premium device cards.
- Right panel is the source of truth for selected device properties.
- Keep typography calm: no oversized hero text.
- Use restrained accent colors and clear protocol chips.
- Preserve the light professional engineering look.

## Performance rules

- Avoid serial IP scanning.
- Use guarded async concurrency.
- Default settings must be safe for industrial LANs.
- Large subnet scan must be capped unless user explicitly expands it.

## Repository hygiene

- Keep `bin`, `obj`, `artifacts`, `dist`, and `node_modules` out of Git.
- Project license is Apache-2.0.
