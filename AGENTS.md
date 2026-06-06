# ARNet Discovery Contribution Notes

ARNet Discovery is a Windows desktop application for substation LAN discovery and protocol evidence checks. Changes should keep the application practical, conservative on engineering networks, and easy for users to download and run.

## Product direction

- Keep the main device table as the primary work area.
- Use collapsible inspector and diagnostics panels to preserve space.
- Treat open ports and protocol labels as evidence, not final identity proof.
- Keep release packages clean and suitable for users who do not have Visual Studio.

## UI direction

- Prefer calm, readable, premium Windows desktop UI.
- Avoid crowded layouts, excessive glow, and oversized typography.
- Keep hover states readable and keyboard navigation predictable.

## Repository direction

- Keep documentation written for users of the application.
- Keep GitHub Pages deployed from GitHub Actions.
- Keep release assets focused on the portable Windows app, PDFs, license, notices, and checksums.
