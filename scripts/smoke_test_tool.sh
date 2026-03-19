#!/usr/bin/env bash
set -euo pipefail

package_source="${1:-artifacts/nuget}"
tool_path="${RUNNER_TEMP:-/tmp}/dotnet-skills-tool"
skills_path="${RUNNER_TEMP:-/tmp}/dotnet-skills-installed-skills"
workspace_path="${RUNNER_TEMP:-/tmp}/dotnet-skills-workspace"
codex_workspace="$workspace_path/codex"
claude_workspace="$workspace_path/claude"
hybrid_workspace="$workspace_path/hybrid"
shared_workspace="$workspace_path/shared"
plain_workspace="$workspace_path/plain"
gemini_workspace="$workspace_path/gemini"

rm -rf "$tool_path" "$skills_path" "$workspace_path"
mkdir -p "$tool_path" "$skills_path" "$codex_workspace/.codex" "$claude_workspace/.claude" "$hybrid_workspace/.codex" "$hybrid_workspace/.claude" "$shared_workspace/.codex" "$shared_workspace/.claude" "$shared_workspace/.agents" "$plain_workspace" "$gemini_workspace/.gemini"

shopt -s nullglob
if [[ -f "$package_source" ]]; then
  latest_package="$package_source"
  package_feed="$(dirname "$latest_package")"
elif [[ -d "$package_source" ]]; then
  packages=( "$package_source"/dotnet-skills.*.nupkg )
  if [[ ${#packages[@]} -eq 0 ]]; then
    echo "No dotnet-skills package found in $package_source" >&2
    exit 1
  fi

  latest_package="$(ls -t "${packages[@]}" | head -n 1)"
  package_feed="$package_source"
else
  echo "Package source must be a .nupkg file or a directory: $package_source" >&2
  exit 1
fi

package_version="$(basename "$latest_package")"
package_version="${package_version#dotnet-skills.}"
package_version="${package_version%.nupkg}"

dotnet tool install \
  --tool-path "$tool_path" \
  --version "$package_version" \
  dotnet-skills \
  --add-source "$package_feed"

export PATH="$tool_path:$PATH"
export DOTNET_SKILLS_SKIP_UPDATE_CHECK=1

dotnet skills version --no-check > "$skills_path/version.txt"
grep -q "dotnet-skills" "$skills_path/version.txt"

dotnet skills list --available-only --target "$skills_path" > "$skills_path/available-list.txt"
grep -q "dotnet-aspire" "$skills_path/available-list.txt"

dotnet skills install aspire --target "$skills_path"
test -f "$skills_path/dotnet-aspire/SKILL.md"
dotnet skills list --local --target "$skills_path" > "$skills_path/local-list.txt"
grep -q "aspire" "$skills_path/local-list.txt"
dotnet skills remove --all --target "$skills_path"
test ! -e "$skills_path/dotnet-aspire"

dotnet skills install aspire --agent anthropic --scope project --project-dir "$workspace_path"
test -f "$workspace_path/.claude/skills/dotnet-aspire/SKILL.md"

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
test ! -e "$codex_workspace/.agents"

auto_claude_target="$(dotnet skills where --project-dir "$claude_workspace")"
case "$auto_claude_target" in
  */.claude/skills) ;;
  *)
    echo "Unexpected auto Claude target: $auto_claude_target" >&2
    exit 1
    ;;
esac

dotnet skills install aspire --bundled --project-dir "$claude_workspace"
test -f "$claude_workspace/.claude/skills/dotnet-aspire/SKILL.md"

dotnet skills install aspire --bundled --project-dir "$hybrid_workspace"
test -f "$hybrid_workspace/.codex/skills/dotnet-aspire/SKILL.md"
test -f "$hybrid_workspace/.claude/skills/dotnet-aspire/SKILL.md"
test ! -e "$hybrid_workspace/.agents"

dotnet skills install aspire --bundled --project-dir "$shared_workspace"
test -f "$shared_workspace/.codex/skills/dotnet-aspire/SKILL.md"
test -f "$shared_workspace/.claude/skills/dotnet-aspire/SKILL.md"
test ! -e "$shared_workspace/.agents/skills/dotnet-aspire"

auto_plain_target="$(dotnet skills where --project-dir "$plain_workspace")"
case "$auto_plain_target" in
  */.agents/skills) ;;
  *)
    echo "Unexpected plain fallback target: $auto_plain_target" >&2
    exit 1
    ;;
esac

dotnet skills install aspire --bundled --project-dir "$plain_workspace"
test -f "$plain_workspace/.agents/skills/dotnet-aspire/SKILL.md"

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
