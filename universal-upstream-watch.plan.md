# Universal Upstream Watch Plan

## Scope

- simplify human-authored upstream watch fragments to one preferred field: `source`
- keep generated `.github/upstream-watch.json` fully normalized for runtime automation
- update contributor and repository docs so configuration is easy to understand
- preserve stable watch ids where existing state depends on them

## Out Of Scope

- changing the runtime watcher behavior in `scripts/upstream_watch.py`
- changing issue formats or scheduling policy
- changing unrelated release or catalog automation

## Steps

1. Update `scripts/generate_upstream_watch.py` to support `source` as the preferred universal fragment field.
2. Migrate watch fragments to `source`.
3. Rewrite README and contributor guidance around the universal format.
4. Regenerate watch config and run verification commands.
5. Commit and push only the relevant changes.
