using Fallout.Common;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.IO;
using Fallout.Common.Tools.DotNet;
using static Fallout.Common.EnvironmentInfo;

[GitHubActions(
    "main",
    GitHubActionsImage.WindowsLatest,
    On = [GitHubActionsTrigger.WorkflowDispatch],
    InvokedTargets = [nameof(PushToGithub)],
    EnableGitHubToken = true)]
[GitHubActions(
    "nuget.org",
    GitHubActionsImage.WindowsLatest,
    On = [GitHubActionsTrigger.WorkflowDispatch],
    InvokedTargets = [nameof(PushToNugetOrg)],
    ImportSecrets = [nameof(VvvvOrgNugetKey)])]
class Build : FalloutBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter]
    [Secret]
    readonly string VvvvOrgNugetKey;

    AbsolutePath Project => RootDirectory / "src" / "VL.Audio.VST.csproj";
    AbsolutePath TestProject => RootDirectory / "tests" / "VL.Audio.VST.Tests" / "VL.Audio.VST.Tests.csproj";
    AbsolutePath PackageOutputDirectory => RootDirectory / "lib";

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

    Target PushToGithub => _ => _
        .DependsOn(Pack)
        .Executes(() =>
        {
            foreach (var file in PackageOutputDirectory.GetFiles("*.nupkg"))
            {
                DotNetTasks.DotNetNuGetPush(_ => _
                    .SetTargetPath(file)
                    .SetApiKey(GitHubActions.Instance.Token)
                    .SetSource("https://nuget.pkg.github.com/vvvv/index.json"));
            }
        });

    Target PushToNugetOrg => _ => _
        .DependsOn(Pack)
        .Executes(() =>
        {
            foreach (var file in PackageOutputDirectory.GetFiles("*.nupkg"))
            {
                DotNetTasks.DotNetNuGetPush(_ => _
                    .SetTargetPath(file)
                    .SetApiKey(VvvvOrgNugetKey)
                    .SetSource("nuget.org"));
            }
        });
}
