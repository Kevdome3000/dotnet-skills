#!/usr/bin/env bash
set -euo pipefail

package_source="${1:-artifacts/nuget}"
tool_path="${RUNNER_TEMP:-/tmp}/dotnet-skills-tool"
skills_path="${RUNNER_TEMP:-/tmp}/dotnet-skills-installed-skills"
workspace_path="${RUNNER_TEMP:-/tmp}/dotnet-skills-workspace"

rm -rf "$tool_path" "$skills_path" "$workspace_path"
mkdir -p "$tool_path" "$skills_path" "$workspace_path"

dotnet tool install \
  --tool-path "$tool_path" \
  ManagedCode.DotnetSkills.Tool \
  --add-source "$package_source"

export PATH="$tool_path:$PATH"

dotnet skills list --target "$skills_path" > "$skills_path/list.txt"
grep -q "dotnet-aspire" "$skills_path/list.txt"

dotnet skills install aspire --target "$skills_path"
test -f "$skills_path/dotnet-aspire/SKILL.md"

dotnet skills install aspire --agent anthropic --scope project --project-dir "$workspace_path"
test -f "$workspace_path/.claude/skills/dotnet-aspire/SKILL.md"
test -f "$workspace_path/.claude/agents/dotnet-aspire.md"

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
