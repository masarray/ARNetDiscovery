# ARNet Discovery maintainer guidelines

These guidelines help keep ARNet Discovery reliable, lightweight, and useful for field engineering work.

## Product principles

- Keep the main workflow table-first and evidence-based.
- Keep the app responsive during scans.
- Show partial results early; enrich results in the background.
- Be clear about evidence versus confirmed device identity.
- Keep the inspector and diagnostics panels secondary and collapsible.
- Avoid visual noise and oversized dashboard elements.

## Scanner principles

- Keep scan logic inside `ARNetDiscovery.Core`.
- Route expected network failures to diagnostics.
- Treat ping timeout as evidence, not as a crash condition.
- Use guarded async concurrency and cancellation.
- Keep default scan behavior conservative for industrial LANs.
- Cap broad subnet scans unless the user explicitly chooses a bounded target list/range.

## UI principles

- WPF is the primary desktop product line.
- Use premium, clean, lightweight controls.
- Avoid default-looking Windows controls when they affect product quality.
- Keep the central device table as the main work area.
- Keep icons embedded/vector-based so the project does not depend on bundled font files.

## Repository hygiene

- Keep `bin`, `obj`, `artifacts`, `dist`, and `node_modules` out of Git.
- Keep documentation user-facing and product-oriented.
- Keep the project Apache-2.0 friendly.
- Add tests before making major scanner behavior changes.

