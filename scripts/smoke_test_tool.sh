#!/usr/bin/env bash
set -euo pipefail

package_source="${1:-artifacts/nuget}"
tool_path="${RUNNER_TEMP:-/tmp}/dotnet-skills-tool"
skills_path="${RUNNER_TEMP:-/tmp}/dotnet-skills-installed-skills"
workspace_path="${RUNNER_TEMP:-/tmp}/dotnet-skills-workspace"
codex_workspace="$workspace_path/codex"
claude_workspace="$workspace_path/claude"
plain_workspace="$workspace_path/plain"
gemini_workspace="$workspace_path/gemini"

rm -rf "$tool_path" "$skills_path" "$workspace_path"
mkdir -p "$tool_path" "$skills_path" "$codex_workspace/.codex" "$claude_workspace/.claude" "$plain_workspace" "$gemini_workspace/.gemini"

dotnet tool install \
  --tool-path "$tool_path" \
  dotnet-skills \
  --add-source "$package_source"

export PATH="$tool_path:$PATH"

dotnet skills list --target "$skills_path" > "$skills_path/list.txt"
grep -q "dotnet-aspire" "$skills_path/list.txt"

dotnet skills install aspire --target "$skills_path"
test -f "$skills_path/dotnet-aspire/SKILL.md"

dotnet skills install aspire --agent anthropic --scope project --project-dir "$workspace_path"
test -f "$workspace_path/.claude/agents/dotnet-aspire.md"

auto_codex_target="$(dotnet skills where --project-dir "$codex_workspace")"
case "$auto_codex_target" in
  */.codex/skills) ;;
  *)
    echo "Unexpected auto Codex target: $auto_codex_target" >&2
    exit 1
    ;;
esac

dotnet skills install aspire --bundled --project-dir "$codex_workspace"
test -f "$codex_workspace/.codex/skills/dotnet-aspire/SKILL.md"

auto_claude_target="$(dotnet skills where --project-dir "$claude_workspace")"
case "$auto_claude_target" in
  */.claude/agents) ;;
  *)
    echo "Unexpected auto Claude target: $auto_claude_target" >&2
    exit 1
    ;;
esac

dotnet skills install aspire --bundled --project-dir "$claude_workspace"
test -f "$claude_workspace/.claude/agents/dotnet-aspire.md"

auto_plain_target="$(dotnet skills where --project-dir "$plain_workspace")"
case "$auto_plain_target" in
  */plain/skills) ;;
  *)
    echo "Unexpected plain fallback target: $auto_plain_target" >&2
    exit 1
    ;;
esac

dotnet skills install aspire --bundled --project-dir "$plain_workspace"
test -f "$plain_workspace/skills/dotnet-aspire/SKILL.md"

copilot_project_target="$(dotnet skills where --agent copilot --scope project)"
case "$copilot_project_target" in
  */.github/skills) ;;
  *)
    echo "Unexpected Copilot project target: $copilot_project_target" >&2
    exit 1
    ;;
esac

gemini_project_target="$(dotnet skills where --agent gemini --scope project)"
case "$gemini_project_target" in
  */.gemini/skills) ;;
  *)
    echo "Unexpected Gemini project target: $gemini_project_target" >&2
    exit 1
  ;;
esac

auto_gemini_target="$(dotnet skills where --project-dir "$gemini_workspace")"
case "$auto_gemini_target" in
  */.gemini/skills) ;;
  *)
    echo "Unexpected auto Gemini target: $auto_gemini_target" >&2
    exit 1
  ;;
esac
