# ADR 0001: Native CLI install targets with default skills fallback

## Status

Accepted

## Context

`dotnet-skills` installs two different payload types:

- reusable skill directories from `skills/*`
- orchestration agent definitions from `agents/*`

The first implementation mixed three concerns in a small number of large classes:

- path detection for Codex, Claude, Copilot, and Gemini
- fallback behavior for the default `.agents/skills` location
- platform-specific output formats such as Markdown, `.agent.md`, and Codex TOML agent roles

That made the resolver logic hard to change safely and caused repeated drift between vendor-specific rules. It also duplicated shared environment and home-directory resolution logic.

The current repository policy is:

- use native per-CLI targets when a concrete CLI root already exists
- do not fan out into shared `.agents/skills` when one or more native roots already exist
- use `.agents/skills` only as a fallback when no native CLI root exists
- keep agent installation native-only with no shared `.agents` fallback
- let explicit user choices (`--agent`, `--target`) override auto-detect

## Decision

### 1. Use per-platform strategy classes

Path resolution and native layout decisions are implemented through one strategy per CLI:

- Codex
- Claude
- Copilot
- Gemini

A shared path-context object resolves:

- project root
- user home
- `CODEX_HOME`

Thin resolver entry points (`SkillInstallTarget` and `AgentInstallTarget`) delegate to the strategy registry instead of embedding platform-specific switches.

### 2. Separate native targets from the default fallback

For skill installation:

- detect native CLI roots in this order: `.codex`, `.claude`, `.github`, `.gemini`
- install into every detected native skill target
- if no native root exists, fall back to `.agents/skills`

For agent installation:

- detect native CLI roots in this order: `.codex`, `.claude`, `.github`, `.gemini`
- install only into native agent targets
- if no native root exists, auto mode fails and asks for `--agent` or `--target`

Shared `.agents` is never added alongside native targets during auto-detect.

### 3. Keep platform-native output formats

Skill payloads are copied as skill directories for every platform.

Agent payloads remain platform-native:

- Codex: TOML role files
- Claude: Markdown agent files
- Copilot: `.agent.md`
- Gemini: Markdown agent files

## Target matrix

### Skills

| Platform | Project target | Global target |
| --- | --- | --- |
| Codex | `.codex/skills` | `$CODEX_HOME/skills` or `~/.codex/skills` |
| Claude | `.claude/skills` | `~/.claude/skills` |
| Copilot | `.github/skills` | `~/.copilot/skills` |
| Gemini | `.gemini/skills` | `~/.gemini/skills` |
| Default fallback | `.agents/skills` | `~/.agents/skills` |

### Agents

| Platform | Project target | Global target |
| --- | --- | --- |
| Codex | `.codex/agents` | `$CODEX_HOME/agents` or `~/.codex/agents` |
| Claude | `.claude/agents` | `~/.claude/agents` |
| Copilot | `.github/agents` | `~/.copilot/agents` |
| Gemini | `.gemini/agents` | `~/.gemini/agents` |

## Consequences

### Positive

- native CLI layouts are explicit and predictable
- fallback behavior is simple: `.agents/skills` only when no native roots exist
- platform changes are isolated to one strategy class
- shared environment resolution is centralized

### Negative

- the codebase gains more small files and indirection
- tests must cover both strategy behavior and top-level resolver behavior

## Implementation notes

- `SKILL.md` remains the canonical skill payload
- `AGENT.md` remains the canonical repository source for orchestration agents
- vendor-native output artifacts are generated only at install time
