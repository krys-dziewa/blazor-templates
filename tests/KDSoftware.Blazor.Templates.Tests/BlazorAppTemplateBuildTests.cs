using System.Diagnostics;

namespace KDSoftware.Blazor.Templates.Tests;

/// <summary>
/// Adds the template to an existing solution and builds it. Requires network access to restore NuGet packages
/// and the client libraries restored by LibMan.
/// </summary>
[Trait("Category", "Integration")]
public sealed class BlazorAppTemplateBuildTests : IDisposable
{
    private const string DirectoryPackagesProps =
        """
        <Project>
          <PropertyGroup>
            <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
          </PropertyGroup>
          <ItemGroup>
            <PackageVersion Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="10.0.12" />
            <PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.12" />
            <PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly.Authentication" Version="10.0.12" />
            <PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="10.0.12" />
            <PackageVersion Include="Microsoft.AspNetCore.DataProtection.StackExchangeRedis" Version="10.0.12" />
            <PackageVersion Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.12" />
            <PackageVersion Include="Microsoft.Web.LibraryManager.Build" Version="3.0.114" />
          </ItemGroup>
        </Project>
        """;

    private readonly DirectoryInfo _solutionDirectory = Directory.CreateTempSubdirectory("kdsoftware-blazor-app-");

    [Theory]
    [InlineData("oidc", true, "cookie")]
    [InlineData("oidc", true, "redis")]
    [InlineData("none", true, "cookie")]
    [InlineData("none", false, "cookie")]
    public async Task ProjectsAreAddedToExistingSolutionAndBuildCleanly(string auth, bool centralPackageManagement, string sessionStore)
    {
        string root = _solutionDirectory.FullName;
        string hive = Path.Combine(root, ".hive");
        string projectsDirectory = Path.Combine(root, "src");
        Directory.CreateDirectory(projectsDirectory);

        // Pin the SDK used by this repository so that the generated projects are built the same way.
        File.Copy(Path.Combine(TemplatePaths.RepositoryRoot, "global.json"), Path.Combine(root, "global.json"));
        await File.WriteAllTextAsync(Path.Combine(root, "MySolution.slnx"), "<Solution />");
        if (centralPackageManagement)
        {
            await File.WriteAllTextAsync(Path.Combine(root, "Directory.Packages.props"), DirectoryPackagesProps);
        }

        await RunDotnetAsync(root, "new", "install", TemplatePaths.BlazorAppTemplate, "--debug:custom-hive", hive);
        await RunDotnetAsync(
            projectsDirectory,
            "new", TemplatePaths.BlazorAppShortName,
            "--name", "MyApp",
            "--auth", auth,
            "--central-package-management", centralPackageManagement ? "true" : "false",
            "--session-store", sessionStore,
            "--debug:custom-hive", hive);

        string solution = await File.ReadAllTextAsync(Path.Combine(root, "MySolution.slnx"));
        Assert.Contains("src/MyApp/MyApp.csproj", solution, StringComparison.Ordinal);
        Assert.Contains("src/MyApp.Client/MyApp.Client.csproj", solution, StringComparison.Ordinal);

        await RunDotnetAsync(root, "build", "--warnaserror");
        Assert.True(
            File.Exists(Path.Combine(projectsDirectory, "MyApp", "wwwroot", "lib", "bootstrap", "dist", "css", "bootstrap.min.css")),
            "LibMan did not restore Bootstrap during the build.");

        await RunDotnetAsync(root, "format", "--verify-no-changes", "--no-restore");
    }

    public void Dispose()
    {
        _solutionDirectory.Delete(recursive: true);
    }

    private static async Task RunDotnetAsync(string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo startInfo = new("dotnet", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        // Variables set by the test host's MSBuild would otherwise leak into the nested build.
        foreach (string key in startInfo.Environment.Keys.Where(static key => key.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            startInfo.Environment.Remove(key);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the dotnet process.");
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.True(
            process.ExitCode == 0,
            $"'dotnet {string.Join(' ', arguments)}' exited with code {process.ExitCode}.{Environment.NewLine}{await standardOutput}{Environment.NewLine}{await standardError}");
    }
}