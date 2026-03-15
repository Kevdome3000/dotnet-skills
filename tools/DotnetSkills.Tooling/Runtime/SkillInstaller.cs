namespace DotnetSkills.Tooling.Runtime;

internal sealed class SkillInstaller(SkillCatalogPackage catalog)
{
    public IReadOnlyList<SkillEntry> SelectSkills(IReadOnlyList<string> requestedSkills, bool installAll)
    {
        if (installAll || requestedSkills.Count == 0)
        {
            return catalog.Skills.OrderBy(skill => skill.Name, StringComparer.Ordinal).ToArray();
        }

        var available = catalog.Skills.ToDictionary(skill => skill.Name, StringComparer.OrdinalIgnoreCase);
        var selected = new List<SkillEntry>();

        foreach (var skillName in requestedSkills)
        {
            if (!TryResolveSkill(available, skillName, out var skill))
            {
                throw new InvalidOperationException($"Unknown skill: {skillName}");
            }

            selected.Add(skill);
        }

        return selected;
    }

    public SkillInstallSummary Install(IReadOnlyList<SkillEntry> skills, SkillInstallLayout layout, bool force)
    {
        layout.SkillRoot.Create();
        layout.AdapterRoot?.Create();

        var installedCount = 0;
        var generatedAdapters = 0;
        var skippedExisting = new List<string>();

        foreach (var skill in skills)
        {
            var sourceDirectory = catalog.ResolveSkillSource(skill.Name);
            var destinationDirectory = new DirectoryInfo(Path.Combine(layout.SkillRoot.FullName, skill.Name));

            if (destinationDirectory.Exists)
            {
                if (!force)
                {
                    skippedExisting.Add(skill.Name);
                    continue;
                }

                destinationDirectory.Delete(recursive: true);
            }

            CopyDirectory(sourceDirectory, destinationDirectory);

            if (layout.Agent == AgentPlatform.Claude && layout.AdapterRoot is not null)
            {
                WriteClaudeAdapter(layout.AdapterRoot, skill);
                generatedAdapters++;
            }

            installedCount++;
        }

        return new SkillInstallSummary(installedCount, generatedAdapters, skippedExisting);
    }

    public static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
    {
        destination.Create();

        foreach (var file in source.GetFiles())
        {
            var targetPath = Path.Combine(destination.FullName, file.Name);
            file.CopyTo(targetPath, overwrite: true);
        }

        foreach (var childDirectory in source.GetDirectories())
        {
            var childDestination = new DirectoryInfo(Path.Combine(destination.FullName, childDirectory.Name));
            CopyDirectory(childDirectory, childDestination);
        }
    }

    private static bool TryResolveSkill(
        IReadOnlyDictionary<string, SkillEntry> available,
        string requestedSkill,
        out SkillEntry skill)
    {
        if (available.TryGetValue(requestedSkill, out skill!))
        {
            return true;
        }

        if (!requestedSkill.StartsWith("dotnet-", StringComparison.OrdinalIgnoreCase))
        {
            return available.TryGetValue($"dotnet-{requestedSkill}", out skill!);
        }

        return false;
    }

    private static void WriteClaudeAdapter(DirectoryInfo adapterRoot, SkillEntry skill)
    {
        adapterRoot.Create();

        var adapterPath = Path.Combine(adapterRoot.FullName, $"{skill.Name}.md");
        var relativeSkillPath = Path.GetRelativePath(adapterRoot.FullName, Path.Combine(adapterRoot.Parent?.FullName ?? adapterRoot.FullName, "skills", skill.Name, "SKILL.md"))
            .Replace('\\', '/');

        var contents =
            $"""
            ---
            name: {skill.Name}
            description: "{EscapeYaml(skill.Description)}"
            ---

            You are the `{skill.Name}` Claude subagent for this repository.

            Before doing task-specific work, read `./{relativeSkillPath}` and follow it as the primary procedure.
            Use additional files from that skill directory only when needed.
            Keep responses concise and execution-focused.
            """;

        File.WriteAllText(adapterPath, contents);
    }

    private static string EscapeYaml(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

internal sealed record SkillInstallSummary(int InstalledCount, int GeneratedAdapters, IReadOnlyList<string> SkippedExisting);
