using System.Diagnostics;

namespace KDSoftware.Blazor.Templates.Tests;

/// <summary>
/// Instantiates the template and builds the result. Requires network access to restore NuGet packages.
/// </summary>
[Trait("Category", "Integration")]
public sealed class BlazorBffTemplateBuildTests : IDisposable
{
    private readonly DirectoryInfo _workDirectory = Directory.CreateTempSubdirectory("kd-blazor-bff-");

    [Theory]
    [InlineData("oidc")]
    [InlineData("none")]
    public async Task GeneratedSolutionBuildsWithoutWarningsAndIsFormatted(string auth)
    {
        string hive = Path.Combine(_workDirectory.FullName, "hive");
        string output = Path.Combine(_workDirectory.FullName, "MyApp");

        await RunDotnetAsync(_workDirectory.FullName, "new", "install", TemplatePaths.BlazorBffTemplate, "--debug:custom-hive", hive);
        await RunDotnetAsync(_workDirectory.FullName, "new", TemplatePaths.BlazorBffShortName, "--name", "MyApp", "--output", output, "--auth", auth, "--no-restore", "--debug:custom-hive", hive);
        await RunDotnetAsync(output, "build", "--warnaserror");
        await RunDotnetAsync(output, "format", "--verify-no-changes", "--no-restore");
    }

    public void Dispose()
    {
        _workDirectory.Delete(recursive: true);
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