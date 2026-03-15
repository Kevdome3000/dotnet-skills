namespace DotnetSkills.Tooling.Runtime;

internal enum AgentPlatform
{
    Codex,
    Claude,
    Copilot,
    Gemini,
}

internal enum InstallScope
{
    Global,
    Project,
}

internal sealed record SkillInstallLayout(
    AgentPlatform Agent,
    InstallScope Scope,
    DirectoryInfo SkillRoot,
    DirectoryInfo? AdapterRoot,
    bool IsExplicitTarget)
{
    public string PrimaryPath => SkillRoot.FullName;

    public string ReloadHint => Agent switch
    {
        AgentPlatform.Codex => "Restart Codex to pick up new skills.",
        AgentPlatform.Claude => "Restart Claude Code or start a new session to pick up the generated subagents.",
        AgentPlatform.Copilot => "Restart Copilot CLI or your IDE agent session to pick up new skills.",
        AgentPlatform.Gemini => "Run /skills reload or restart Gemini CLI to pick up new skills.",
        _ => "Restart your agent session to pick up new skills.",
    };
}

internal static class SkillInstallTarget
{
    public static SkillInstallLayout Resolve(
        string? explicitTargetPath,
        AgentPlatform agent,
        InstallScope scope,
        string? projectDirectory)
    {
        if (!string.IsNullOrWhiteSpace(explicitTargetPath))
        {
            return ResolveExplicit(agent, scope, explicitTargetPath);
        }

        return scope switch
        {
            InstallScope.Global => ResolveGlobal(agent),
            InstallScope.Project => ResolveProject(agent, projectDirectory),
            _ => throw new InvalidOperationException($"Unsupported install scope: {scope}"),
        };
    }

    public static AgentPlatform ParseAgent(string value) => value.ToLowerInvariant() switch
    {
        "codex" => AgentPlatform.Codex,
        "claude" => AgentPlatform.Claude,
        "anthropic" => AgentPlatform.Claude,
        "copilot" => AgentPlatform.Copilot,
        "github" => AgentPlatform.Copilot,
        "github-copilot" => AgentPlatform.Copilot,
        "gemini" => AgentPlatform.Gemini,
        _ => throw new InvalidOperationException("Unsupported agent: " + value + ". Expected codex, claude, anthropic, copilot, github-copilot, or gemini."),
    };

    public static InstallScope ParseScope(string value) => value.ToLowerInvariant() switch
    {
        "global" => InstallScope.Global,
        "project" => InstallScope.Project,
        _ => throw new InvalidOperationException($"Unsupported scope: {value}. Expected global or project."),
    };

    private static SkillInstallLayout ResolveExplicit(AgentPlatform agent, InstallScope scope, string explicitTargetPath)
    {
        var skillRoot = new DirectoryInfo(Path.GetFullPath(explicitTargetPath));
        DirectoryInfo? adapterRoot = null;

        if (agent == AgentPlatform.Claude &&
            string.Equals(skillRoot.Name, "skills", StringComparison.OrdinalIgnoreCase))
        {
            var parentPath = skillRoot.Parent?.FullName;
            if (!string.IsNullOrWhiteSpace(parentPath))
            {
                adapterRoot = new DirectoryInfo(Path.Combine(parentPath, "agents"));
            }
        }

        return new SkillInstallLayout(agent, scope, skillRoot, adapterRoot, IsExplicitTarget: true);
    }

    private static SkillInstallLayout ResolveGlobal(AgentPlatform agent)
    {
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return agent switch
        {
            AgentPlatform.Codex => new SkillInstallLayout(agent, InstallScope.Global, ResolveCodexGlobal(userHome), null, IsExplicitTarget: false),
            AgentPlatform.Claude => new SkillInstallLayout(
                agent,
                InstallScope.Global,
                new DirectoryInfo(Path.Combine(userHome, ".claude", "skills")),
                new DirectoryInfo(Path.Combine(userHome, ".claude", "agents")),
                IsExplicitTarget: false),
            AgentPlatform.Copilot => new SkillInstallLayout(agent, InstallScope.Global, new DirectoryInfo(Path.Combine(userHome, ".copilot", "skills")), null, IsExplicitTarget: false),
            AgentPlatform.Gemini => new SkillInstallLayout(agent, InstallScope.Global, new DirectoryInfo(Path.Combine(userHome, ".gemini", "skills")), null, IsExplicitTarget: false),
            _ => throw new InvalidOperationException($"Unsupported agent: {agent}"),
        };
    }

    private static SkillInstallLayout ResolveProject(AgentPlatform agent, string? projectDirectory)
    {
        var rootDirectory = string.IsNullOrWhiteSpace(projectDirectory)
            ? Path.GetFullPath(Directory.GetCurrentDirectory())
            : Path.GetFullPath(projectDirectory);

        return agent switch
        {
            AgentPlatform.Codex => new SkillInstallLayout(agent, InstallScope.Project, new DirectoryInfo(Path.Combine(rootDirectory, ".codex", "skills")), null, IsExplicitTarget: false),
            AgentPlatform.Claude => new SkillInstallLayout(
                agent,
                InstallScope.Project,
                new DirectoryInfo(Path.Combine(rootDirectory, ".claude", "skills")),
                new DirectoryInfo(Path.Combine(rootDirectory, ".claude", "agents")),
                IsExplicitTarget: false),
            AgentPlatform.Copilot => new SkillInstallLayout(agent, InstallScope.Project, new DirectoryInfo(Path.Combine(rootDirectory, ".github", "skills")), null, IsExplicitTarget: false),
            AgentPlatform.Gemini => new SkillInstallLayout(agent, InstallScope.Project, new DirectoryInfo(Path.Combine(rootDirectory, ".gemini", "skills")), null, IsExplicitTarget: false),
            _ => throw new InvalidOperationException($"Unsupported agent: {agent}"),
        };
    }

    private static DirectoryInfo ResolveCodexGlobal(string userHome)
    {
        var codexHome = Environment.GetEnvironmentVariable("CODEX_HOME");
        if (!string.IsNullOrWhiteSpace(codexHome))
        {
            return new DirectoryInfo(Path.Combine(codexHome, "skills"));
        }

        return new DirectoryInfo(Path.Combine(userHome, ".codex", "skills"));
    }
}
