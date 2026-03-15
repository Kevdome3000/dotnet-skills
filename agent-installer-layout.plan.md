## Scope

- Update the `dotnet-skills` tool so `--agent` understands Codex, Claude, Copilot, and Gemini install layouts.
- Keep the tool focused on skill distribution, not general agent management.
- Update public docs and CI smoke coverage for the new install model.

## Out of Scope

- Reworking the skill catalog format itself.
- Replacing Python-based GitHub automation with `.NET`.
- Adding new skills or changing skill content unrelated to installer compatibility.

## Current State

- The installer only models `codex`, `claude`, and `copilot`.
- Claude is treated like a plain `skills` directory, which does not match Anthropic's official subagent model.
- Gemini is unsupported.
- CLI messaging and docs still assume Codex-first behavior in a few places.

## Plan

1. Update target-resolution logic to model agent-specific install layouts and add Gemini support.
2. Update installer logic so Claude installs both skill payloads and Claude subagent adapters.
3. Update CLI help, output, README, CONTRIBUTING, and AGENTS to describe the new behavior.
4. Extend smoke coverage and run build plus tool checks.

## Verification

- `dotnet build tools/DotnetSkills.Tooling/DotnetSkills.Tooling.csproj -c Release`
- `dotnet pack tools/DotnetSkills.Tooling/DotnetSkills.Tooling.csproj -c Release`
- `dotnet run --project tools/DotnetSkills.Tooling/DotnetSkills.Tooling.csproj -- where --agent claude --scope project`
- `dotnet run --project tools/DotnetSkills.Tooling/DotnetSkills.Tooling.csproj -- where --agent gemini --scope project`
- `dotnet run --project tools/DotnetSkills.Tooling/DotnetSkills.Tooling.csproj -- install aspire --bundled --agent claude --target <tmp>`
- `bash scripts/smoke_test_tool.sh`

## Status

- Completed: agent-specific layout resolution now covers Codex, Claude, Copilot, and Gemini.
- Completed: Claude installs reusable skill payloads plus generated `.claude/agents` adapters.
- Completed: README, CONTRIBUTING, AGENTS, and CI smoke coverage were updated for the new install model.
- Verified locally without a contributor-local `dotnet tool install --add-source ...` loop:
  - `dotnet build tools/DotnetSkills.Tooling/DotnetSkills.Tooling.csproj -c Release`
  - `dotnet pack tools/DotnetSkills.Tooling/DotnetSkills.Tooling.csproj -c Release`
  - `dotnet run --project tools/DotnetSkills.Tooling/DotnetSkills.Tooling.csproj -- where --agent anthropic --scope project`
  - `dotnet run --project tools/DotnetSkills.Tooling/DotnetSkills.Tooling.csproj -- where --agent gemini --scope project`
  - `dotnet run --project tools/DotnetSkills.Tooling/DotnetSkills.Tooling.csproj -- install aspire --bundled --agent anthropic --scope project --project-dir <tmp>`
  - `bash -n scripts/smoke_test_tool.sh`
