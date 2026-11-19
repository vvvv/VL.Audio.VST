using Fallout.Common;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using static Fallout.Common.EnvironmentInfo;

// Packages are only published to nuget.org (the GitHub package feed only holds the first, old versions).
// Publishing also creates the GitHub release of the version, so it needs to write to the repository.
[GitHubActions(
    "nuget.org",
    GitHubActionsImage.WindowsLatest,
    On = [GitHubActionsTrigger.WorkflowDispatch],
    InvokedTargets = [nameof(PushToNugetOrg)],
    ImportSecrets = [nameof(VvvvOrgNugetKey)],
    EnableGitHubToken = true,
    WritePermissions = [GitHubActionsPermissions.Contents])]
class Build : FalloutBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter]
    [Secret]
    readonly string VvvvOrgNugetKey;

    const string Repository = "vvvv/VL.Audio.VST";

    AbsolutePath Project => RootDirectory / "src" / "VL.Audio.VST.csproj";
    AbsolutePath TestProject => RootDirectory / "tests" / "VL.Audio.VST.Tests" / "VL.Audio.VST.Tests.csproj";
    AbsolutePath PackageOutputDirectory => RootDirectory / "lib";

    string Version => XDocument.Load(Project).Descendants("Version").Single().Value;

    Target Compile => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(Project)
                .SetConfiguration(Configuration));
        });

    // Headless tests of the pin logic and of the host against a fake plugin: no vvvv, no installed plugins
    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(TestProject)
                .SetConfiguration(Configuration));
        });

    // Depends on Test, so a failing test stops the build before anything is pushed
    Target Pack => _ => _
        .DependsOn(Compile, Test)
        .Executes(() =>
        {
            PackageOutputDirectory.GlobFiles("*.nupkg").DeleteFiles();
            DotNetTasks.DotNetPack(s => s
                .SetProject(Project)
                .SetConfiguration(Configuration));
        });

    Target PushToNugetOrg => _ => _
        .DependsOn(Pack)
        .Triggers(CreateGitHubRelease)
        .Executes(() =>
        {
            foreach (var file in PackageOutputDirectory.GetFiles("*.nupkg"))
            {
                // Skipping a version that is already there makes it safe to run the workflow again,
                // e.g. when creating the release failed after the push
                DotNetTasks.DotNetNuGetPush(_ => _
                    .SetTargetPath(file)
                    .SetApiKey(VvvvOrgNugetKey)
                    .SetSource("nuget.org")
                    .EnableSkipDuplicate());
            }
        });

    // Tags the commit with v<Version> and creates the GitHub release with the package attached and the commits
    // since the previous release as notes. Skipped when the release already exists.
    Target CreateGitHubRelease => _ => _
        .DependsOn(Pack)
        .After(PushToNugetOrg)
        .Executes(() =>
        {
            var version = Version;
            var tag = $"v{version}";
            if (TryGh(["release", "view", tag, "--repo", Repository], out var existing))
            {
                Serilog.Log.Information("Release {Tag} exists already", tag);
                return;
            }

            var commit = GitHubActions.Instance?.Sha ?? Git("rev-parse", "HEAD");
            var previousTag = Gh("release", "list", "--repo", Repository, "--exclude-drafts", "--limit", "20", "--json", "tagName", "--jq", ".[].tagName")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(t => t != tag);

            var notes = new List<string>
            {
                $"Published to [nuget.org](https://www.nuget.org/packages/VL.Audio.VST/{version}).",
                "",
            };
            if (previousTag != null)
            {
                // The GitHub API knows the history, the checkout of the workflow only has the last commit
                var subjects = Gh("api", $"repos/{Repository}/compare/{previousTag}...{commit}", "--paginate", "--jq", ".commits[].commit.message | split(\"\\n\")[0]");
                notes.Add("## Changes");
                notes.AddRange(subjects.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(s => $"- {s}"));
                notes.Add("");
                notes.Add($"**Full changelog**: https://github.com/{Repository}/compare/{previousTag}...{tag}");
            }

            var notesFile = TemporaryDirectory / $"release-notes-{version}.md";
            File.WriteAllLines(notesFile, notes);

            var package = PackageOutputDirectory / $"VL.Audio.VST.{version}.nupkg";
            var arguments = new List<string> { "release", "create", tag, package, "--repo", Repository, "--target", commit, "--title", version, "--notes-file", notesFile };
            if (version.Contains('-'))
                arguments.Add("--prerelease");
            Gh([.. arguments]);
        });

    // gh is installed on the GitHub runners; there it uses the workflow's token, locally your own login
    static string Gh(params string[] arguments)
    {
        if (!TryGh(arguments, out var output))
            throw new Exception($"gh {string.Join(' ', arguments)} failed:\n{output}");
        return output;
    }

    static bool TryGh(string[] arguments, out string output)
    {
        var environment = new Dictionary<string, string>();
        if (GitHubActions.Instance?.Token is { Length: > 0 } token)
            environment["GH_TOKEN"] = token;
        return Run("gh", arguments, environment, out output);
    }

    static string Git(params string[] arguments) =>
        Run("git", arguments, new(), out var output) ? output.Trim() : throw new Exception($"git failed:\n{output}");

    static bool Run(string fileName, string[] arguments, Dictionary<string, string> environment, out string output)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = RootDirectory,
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        foreach (var (key, value) in environment)
            startInfo.Environment[key] = value;

        using var process = Process.Start(startInfo)!;
        var standardError = process.StandardError.ReadToEndAsync();
        var standardOutput = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        output = process.ExitCode == 0 ? standardOutput : standardOutput + standardError.Result;
        return process.ExitCode == 0;
    }
}
