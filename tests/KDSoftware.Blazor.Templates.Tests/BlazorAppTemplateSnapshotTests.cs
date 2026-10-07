using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;

namespace KDSoftware.Blazor.Templates.Tests;

public sealed partial class BlazorAppTemplateSnapshotTests
{
    [Theory]
    [InlineData("oidc", true, "cookie")]
    [InlineData("oidc", true, "redis")]
    [InlineData("none", true, "cookie")]
    [InlineData("none", false, "cookie")]
    public async Task GeneratedContentMatchesSnapshot(string auth, bool centralPackageManagement, string sessionStore)
    {
        // The template is added to an existing solution, so instantiate it next to one; the snapshot then
        // also covers the projects being added to the solution.
        DirectoryInfo outputDirectory = Directory.CreateTempSubdirectory("kdsoftware-blazor-app-snapshot-");
        try
        {
            await File.WriteAllTextAsync(Path.Combine(outputDirectory.FullName, "MySolution.slnx"), "<Solution />");
            await VerifyAsync(auth, centralPackageManagement, sessionStore, outputDirectory.FullName);
        }
        finally
        {
            outputDirectory.Delete(recursive: true);
        }
    }

    private static async Task VerifyAsync(string auth, bool centralPackageManagement, string sessionStore, string outputDirectory)
    {
        string scenarioName = $"auth-{auth}";
        if (sessionStore != "cookie")
        {
            scenarioName += $"-{sessionStore}";
        }

        if (!centralPackageManagement)
        {
            scenarioName += "-nocpm";
        }

        TemplateVerifierOptions options = new TemplateVerifierOptions(TemplatePaths.BlazorAppShortName)
        {
            OutputDirectory = outputDirectory,
            EnsureEmptyOutputDirectory = false,
            TemplatePath = TemplatePaths.BlazorAppTemplate,
            TemplateSpecificArgs =
            [
                "--name", "MyApp",
                "--auth", auth,
                "--central-package-management", centralPackageManagement ? "true" : "false",
                "--session-store", sessionStore,
                "--http-port", "5000",
                "--https-port", "7000",
                "--no-restore",
            ],
            SnapshotsDirectory = "Snapshots",
            ScenarioName = scenarioName,
            DoNotAppendTemplateArgsToScenarioName = true,
            DoNotPrependCallerMethodNameToScenarioName = true,
            DisableDiffTool = true,
            // Binary assets copied verbatim from the template. Both separators are listed because
            // patterns are matched against OS-specific relative paths.
            VerificationExcludePatterns = ["**/*.png", @"**\*.png"],
        }
        .WithCustomScrubbers(ScrubbersDefinition.Empty.AddScrubber(ScrubUserSecretsId, "csproj"));

        VerificationEngine engine = new(NullLogger.Instance);
        await engine.Execute(options);
    }

    private static void ScrubUserSecretsId(StringBuilder content)
    {
        string scrubbed = UserSecretsIdRegex().Replace(content.ToString(), "<UserSecretsId>{UserSecretsId}</UserSecretsId>");
        content.Clear().Append(scrubbed);
    }

    [GeneratedRegex("<UserSecretsId>[^<]+</UserSecretsId>")]
    private static partial Regex UserSecretsIdRegex();
}