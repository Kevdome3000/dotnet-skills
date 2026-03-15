using DotnetSkills.Tooling.Runtime;

namespace DotnetSkills.Tooling;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            return await RunAsync(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            WriteUsage();
            return 1;
        }

        var command = args[0];
        return command switch
        {
            "list" => await RunListAsync(args[1..]),
            "install" => await RunInstallAsync(args[1..]),
            "sync" => await RunSyncAsync(args[1..]),
            "where" => RunWhere(args[1..]),
            _ => UnknownCommand(command),
        };
    }

    private static async Task<int> RunListAsync(string[] args)
    {
        string? targetPath = null;
        string? cachePath = null;
        string? catalogVersion = null;
        string? projectDirectory = null;
        var bundledOnly = false;
        var agent = AgentPlatform.Codex;
        var scope = InstallScope.Global;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--target":
                    targetPath = ReadValue(args, ++index, "--target");
                    break;
                case "--cache-dir":
                    cachePath = ReadValue(args, ++index, "--cache-dir");
                    break;
                case "--catalog-version":
                    catalogVersion = ReadValue(args, ++index, "--catalog-version");
                    break;
                case "--agent":
                    agent = SkillInstallTarget.ParseAgent(ReadValue(args, ++index, "--agent"));
                    break;
                case "--scope":
                    scope = SkillInstallTarget.ParseScope(ReadValue(args, ++index, "--scope"));
                    break;
                case "--project-dir":
                    projectDirectory = ReadValue(args, ++index, "--project-dir");
                    break;
                case "--bundled":
                    bundledOnly = true;
                    break;
                default:
                    return UnknownCommand($"list {string.Join(' ', args)}");
            }
        }

        var catalog = await ResolveCatalogForListAsync(bundledOnly, cachePath, catalogVersion);
        var installer = new SkillInstaller(catalog);
        var layout = SkillInstallTarget.Resolve(targetPath, agent, scope, projectDirectory);

        Console.Error.WriteLine($"Catalog source: {catalog.SourceLabel} ({catalog.CatalogVersion})");
        Console.Error.WriteLine($"Install target: {layout.SkillRoot.FullName}");
        if (layout.AdapterRoot is not null)
        {
            Console.Error.WriteLine($"Adapter target: {layout.AdapterRoot.FullName}");
        }

        foreach (var skill in catalog.Skills.OrderBy(skill => skill.Name, StringComparer.Ordinal))
        {
            var installedSuffix = Directory.Exists(Path.Combine(layout.SkillRoot.FullName, skill.Name)) ? " (installed)" : string.Empty;
            Console.WriteLine($"{skill.Name} {skill.Version}{installedSuffix} - {skill.Description}");
        }

        return 0;
    }

    private static async Task<int> RunInstallAsync(string[] args)
    {
        var requestedSkills = new List<string>();
        string? targetPath = null;
        string? cachePath = null;
        string? catalogVersion = null;
        string? projectDirectory = null;
        var installAll = false;
        var force = false;
        var bundledOnly = false;
        var refreshCatalog = false;
        var agent = AgentPlatform.Codex;
        var scope = InstallScope.Global;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--all":
                    installAll = true;
                    break;
                case "--force":
                    force = true;
                    break;
                case "--target":
                    targetPath = ReadValue(args, ++index, "--target");
                    break;
                case "--cache-dir":
                    cachePath = ReadValue(args, ++index, "--cache-dir");
                    break;
                case "--catalog-version":
                    catalogVersion = ReadValue(args, ++index, "--catalog-version");
                    break;
                case "--agent":
                    agent = SkillInstallTarget.ParseAgent(ReadValue(args, ++index, "--agent"));
                    break;
                case "--scope":
                    scope = SkillInstallTarget.ParseScope(ReadValue(args, ++index, "--scope"));
                    break;
                case "--project-dir":
                    projectDirectory = ReadValue(args, ++index, "--project-dir");
                    break;
                case "--bundled":
                    bundledOnly = true;
                    break;
                case "--refresh":
                    refreshCatalog = true;
                    break;
                default:
                    requestedSkills.Add(args[index]);
                    break;
            }
        }

        var catalog = await ResolveCatalogForInstallAsync(bundledOnly, cachePath, catalogVersion, refreshCatalog);
        var installer = new SkillInstaller(catalog);
        var layout = SkillInstallTarget.Resolve(targetPath, agent, scope, projectDirectory);
        var selectedSkills = installer.SelectSkills(requestedSkills, installAll);
        var summary = installer.Install(selectedSkills, layout, force);

        Console.Error.WriteLine($"Catalog source: {catalog.SourceLabel} ({catalog.CatalogVersion})");
        Console.Error.WriteLine($"Install target: {layout.SkillRoot.FullName}");
        if (layout.AdapterRoot is not null)
        {
            Console.Error.WriteLine($"Adapter target: {layout.AdapterRoot.FullName}");
        }

        Console.WriteLine($"Installed {summary.InstalledCount} skill(s) to {layout.SkillRoot.FullName}");

        if (summary.GeneratedAdapters > 0 && layout.AdapterRoot is not null)
        {
            Console.WriteLine($"Generated {summary.GeneratedAdapters} Claude subagent file(s) in {layout.AdapterRoot.FullName}");
        }

        if (summary.SkippedExisting.Count > 0)
        {
            Console.WriteLine($"Skipped existing: {string.Join(", ", summary.SkippedExisting)}");
        }

        Console.WriteLine(layout.ReloadHint);

        return 0;
    }

    private static int RunWhere(string[] args)
    {
        string? targetPath = null;
        string? projectDirectory = null;
        var agent = AgentPlatform.Codex;
        var scope = InstallScope.Global;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--target":
                    targetPath = ReadValue(args, ++index, "--target");
                    break;
                case "--agent":
                    agent = SkillInstallTarget.ParseAgent(ReadValue(args, ++index, "--agent"));
                    break;
                case "--scope":
                    scope = SkillInstallTarget.ParseScope(ReadValue(args, ++index, "--scope"));
                    break;
                case "--project-dir":
                    projectDirectory = ReadValue(args, ++index, "--project-dir");
                    break;
                default:
                    return UnknownCommand($"where {string.Join(' ', args)}");
            }
        }

        var layout = SkillInstallTarget.Resolve(targetPath, agent, scope, projectDirectory);
        Console.WriteLine(layout.PrimaryPath);
        return 0;
    }

    private static async Task<int> RunSyncAsync(string[] args)
    {
        string? cachePath = null;
        string? catalogVersion = null;
        var force = false;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--cache-dir":
                    cachePath = ReadValue(args, ++index, "--cache-dir");
                    break;
                case "--catalog-version":
                    catalogVersion = ReadValue(args, ++index, "--catalog-version");
                    break;
                case "--force":
                    force = true;
                    break;
                default:
                    return UnknownCommand($"sync {string.Join(' ', args)}");
            }
        }

        var client = CreateReleaseClient(cachePath);
        var catalog = await client.SyncAsync(catalogVersion, force, CancellationToken.None);

        Console.WriteLine($"Synced catalog {catalog.CatalogVersion} from {catalog.SourceLabel}");
        Console.WriteLine($"Cache: {catalog.CatalogRoot.FullName}");

        return 0;
    }

    private static async Task<SkillCatalogPackage> ResolveCatalogForListAsync(bool bundledOnly, string? cachePath, string? catalogVersion)
    {
        if (bundledOnly)
        {
            return SkillCatalogPackage.LoadBundled();
        }

        var client = CreateReleaseClient(cachePath);

        try
        {
            var manifest = await client.LoadManifestAsync(catalogVersion, CancellationToken.None);
            return SkillCatalogPackage.LoadFromDirectory(
                await MaterializeManifestOnlyCatalogAsync(client.ResolveCacheRoot(), manifest, catalogVersion),
                string.IsNullOrWhiteSpace(catalogVersion) ? "latest GitHub catalog manifest" : $"GitHub catalog manifest {catalogVersion}",
                string.IsNullOrWhiteSpace(catalogVersion) ? "latest" : catalogVersion);
        }
        catch (Exception exception) when (string.IsNullOrWhiteSpace(catalogVersion))
        {
            Console.Error.WriteLine($"Remote catalog unavailable: {exception.Message}");
            Console.Error.WriteLine("Falling back to bundled catalog.");
            return SkillCatalogPackage.LoadBundled();
        }
    }

    private static async Task<SkillCatalogPackage> ResolveCatalogForInstallAsync(bool bundledOnly, string? cachePath, string? catalogVersion, bool refreshCatalog)
    {
        if (bundledOnly)
        {
            return SkillCatalogPackage.LoadBundled();
        }

        var client = CreateReleaseClient(cachePath);

        try
        {
            return await client.SyncAsync(catalogVersion, refreshCatalog, CancellationToken.None);
        }
        catch (Exception exception) when (string.IsNullOrWhiteSpace(catalogVersion))
        {
            Console.Error.WriteLine($"Remote catalog unavailable: {exception.Message}");
            Console.Error.WriteLine("Falling back to bundled catalog.");
            return SkillCatalogPackage.LoadBundled();
        }
    }

    private static GitHubCatalogReleaseClient CreateReleaseClient(string? cachePath)
    {
        var cacheRoot = string.IsNullOrWhiteSpace(cachePath)
            ? GitHubCatalogReleaseClient.ResolveDefaultCacheDirectory()
            : new DirectoryInfo(Path.GetFullPath(cachePath));

        return new GitHubCatalogReleaseClient(cacheRoot);
    }

    private static async Task<DirectoryInfo> MaterializeManifestOnlyCatalogAsync(DirectoryInfo cacheRoot, SkillManifest manifest, string? catalogVersion)
    {
        var versionSuffix = string.IsNullOrWhiteSpace(catalogVersion) ? "latest" : catalogVersion;
        var directory = new DirectoryInfo(Path.Combine(cacheRoot.FullName, ".manifest", versionSuffix));
        directory.Create();

        var catalogDirectory = new DirectoryInfo(Path.Combine(directory.FullName, "catalog"));
        var skillsDirectory = new DirectoryInfo(Path.Combine(directory.FullName, "skills"));
        catalogDirectory.Create();
        skillsDirectory.Create();

        var manifestPath = Path.Combine(catalogDirectory.FullName, "skills.json");
        await File.WriteAllTextAsync(manifestPath, System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        return directory;
    }

    private static string ReadValue(string[] args, int index, string optionName)
    {
        if (index >= args.Length)
        {
            throw new InvalidOperationException($"{optionName} requires a value");
        }

        return args[index];
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        WriteUsage();
        return 1;
    }

    private static void WriteUsage()
    {
        Console.WriteLine(
            """
            dotnet-skills

            Usage:
              dotnet skills list [--bundled] [--catalog-version 1.2.3] [--target /path/to/skills]
              dotnet skills where [--agent copilot] [--scope project]
              dotnet skills sync [--catalog-version 1.2.3] [--cache-dir /path/to/cache] [--force]
              dotnet skills install
              dotnet skills install aspire orleans
              dotnet skills install aspire --agent anthropic --scope project
              dotnet skills install aspire --agent gemini --scope project
              dotnet skills install --all --target /path/to/skills --force

            Notes:
              - list and install use the latest catalog-v* GitHub release by default.
              - --bundled skips the network and uses the catalog packaged with the tool.
              - --catalog-version installs or lists a specific catalog-v<version> release.
              - --refresh forces install to redownload the selected remote catalog.
              - skill IDs stay namespaced as dotnet-*, but commands accept short aliases such as aspire -> dotnet-aspire.
              - --agent and --scope map the install path for Codex, Claude, Copilot, and Gemini layouts.
              - Claude installs skill payloads plus generated `.claude/agents` subagent adapters.
            """);
    }
}
