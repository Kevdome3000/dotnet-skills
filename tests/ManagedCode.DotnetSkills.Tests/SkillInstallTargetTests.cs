using ManagedCode.DotnetSkills.Runtime;

namespace ManagedCode.DotnetSkills.Tests;

public sealed class SkillInstallTargetTests
{
    [Fact]
    public void ResolveAutoProject_PrefersNativeCodexLayoutBeforeOtherPlatforms()
    {
        using var tempDirectory = new TemporaryDirectory();
        Directory.CreateDirectory(System.IO.Path.Combine(tempDirectory.Path, ".codex"));
        Directory.CreateDirectory(System.IO.Path.Combine(tempDirectory.Path, ".claude"));
        Directory.CreateDirectory(System.IO.Path.Combine(tempDirectory.Path, ".github"));

        var layout = SkillInstallTarget.Resolve(
            explicitTargetPath: null,
            agent: AgentPlatform.Auto,
            scope: InstallScope.Project,
            projectDirectory: tempDirectory.Path);

        Assert.Equal(AgentPlatform.Codex, layout.Agent);
        Assert.Equal(SkillInstallMode.RawSkillPayloads, layout.Mode);
        Assert.Equal(System.IO.Path.Combine(tempDirectory.Path, ".codex", "skills"), layout.PrimaryRoot.FullName);
    }

    [Fact]
    public void ResolveAllDetected_Project_ReturnsEveryExistingPlatformInConfiguredOrder()
    {
        using var tempDirectory = new TemporaryDirectory();
        Directory.CreateDirectory(System.IO.Path.Combine(tempDirectory.Path, ".codex"));
        Directory.CreateDirectory(System.IO.Path.Combine(tempDirectory.Path, ".claude"));
        Directory.CreateDirectory(System.IO.Path.Combine(tempDirectory.Path, ".github"));
        Directory.CreateDirectory(System.IO.Path.Combine(tempDirectory.Path, ".gemini"));
        Directory.CreateDirectory(System.IO.Path.Combine(tempDirectory.Path, ".agents"));

        var layouts = SkillInstallTarget.ResolveAllDetected(tempDirectory.Path, InstallScope.Project);

        Assert.Equal(
            [AgentPlatform.Codex, AgentPlatform.Claude, AgentPlatform.Copilot, AgentPlatform.Gemini, AgentPlatform.Auto],
            layouts.Select(layout => layout.Agent).ToArray());
        Assert.Equal(
            [
                System.IO.Path.Combine(tempDirectory.Path, ".codex", "skills"),
                System.IO.Path.Combine(tempDirectory.Path, ".claude", "agents"),
                System.IO.Path.Combine(tempDirectory.Path, ".github", "skills"),
                System.IO.Path.Combine(tempDirectory.Path, ".gemini", "skills"),
                System.IO.Path.Combine(tempDirectory.Path, ".agents", "skills"),
            ],
            layouts.Select(layout => layout.PrimaryRoot.FullName).ToArray());
    }

    [Fact]
    public void ResolveAutoProject_FallsBackToLegacyAgentsLayoutWhenNoPlatformFoldersExist()
    {
        using var tempDirectory = new TemporaryDirectory();

        var layout = SkillInstallTarget.Resolve(
            explicitTargetPath: null,
            agent: AgentPlatform.Auto,
            scope: InstallScope.Project,
            projectDirectory: tempDirectory.Path);

        Assert.Equal(AgentPlatform.Auto, layout.Agent);
        Assert.Equal(SkillInstallMode.RawSkillPayloads, layout.Mode);
        Assert.Equal(System.IO.Path.Combine(tempDirectory.Path, ".agents", "skills"), layout.PrimaryRoot.FullName);
    }
}
