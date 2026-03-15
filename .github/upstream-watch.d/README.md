# upstream-watch fragments

This directory is the human-maintained source of truth for upstream watch configuration.

Rules:

- keep fragments small and grouped by vendor, framework family, or domain
- add new custom libraries to a dedicated fragment instead of growing one giant file
- do not hand-edit `../upstream-watch.json`
- regenerate the root file with `python3 scripts/generate_upstream_watch.py`

Suggested naming:

- `00-metadata.json`
- `10-microsoft-releases.json`
- `20-managedcode-releases.json`
- `30-docs.json`
- `40-<vendor>.json`
