# ManagedCode Watch Alignment Plan

## Scope

- Remove generic `dotnet` or `dotnet-architecture` watch mappings from project-specific ManagedCode release watches.
- Create dedicated skills for the ManagedCode libraries already tracked in the watch configuration.
- Point each ManagedCode watch entry only at the dedicated skill that matches the watched project.
- Update policy and contributor docs so future project-specific watches follow the same rule.

## Out Of Scope

- Reworking the watcher runtime.
- Adding more vendors to the watch list.
- Publishing a release.

## Status

- In progress: document the rule that project-specific release watches must not point to generic umbrella skills.
- Completed: added dedicated ManagedCode skills for Storage, Communication, MarkItDown, Orleans.SignalR, MimeTypes, and Orleans.Graph.
- In progress: update ManagedCode watch mappings and contributor-facing rules so each watch points only to its dedicated skill.
