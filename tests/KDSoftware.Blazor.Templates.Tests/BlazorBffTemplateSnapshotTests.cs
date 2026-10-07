using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;

namespace KDSoftware.Blazor.Templates.Tests;

public sealed partial class BlazorBffTemplateSnapshotTests
{
    [Theory]
    [InlineData("oidc")]
    [InlineData("none")]
    public async Task GeneratedContentMatchesSnapshot(string auth)
    {
        TemplateVerifierOptions options = new TemplateVerifierOptions(TemplatePaths.BlazorBffShortName)
        {
            TemplatePath = TemplatePaths.BlazorBffTemplate,
            TemplateSpecificArgs = ["--name", "MyApp", "--auth", auth, "--http-port", "5000", "--https-port", "7000", "--no-restore"],
            SnapshotsDirectory = "Snapshots",
            ScenarioName = $"auth-{auth}",
            DoNotAppendTemplateArgsToScenarioName = true,
            DisableDiffTool = true,
            // Third-party and binary assets copied verbatim from the template. Both separators are listed because
            // patterns are matched against OS-specific relative paths.
            VerificationExcludePatterns = ["**/wwwroot/lib/*", @"**\wwwroot\lib\*", "**/*.png", @"**\*.png"],
        }
        .WithCustomScrubbers(ScrubbersDefinition.Empty.AddScrubber(ScrubUserSecretsId, "csproj"));

        VerificationEngine engine = new(NullLogger.Instance);
        await engine.Execute(options);
    }

    private static void ScrubUserSecretsId(System.Text.StringBuilder content)
    {
        string scrubbed = UserSecretsIdRegex().Replace(content.ToString(), "<UserSecretsId>{UserSecretsId}</UserSecretsId>");
        content.Clear().Append(scrubbed);
    }

    [GeneratedRegex("<UserSecretsId>[^<]+</UserSecretsId>")]
    private static partial Regex UserSecretsIdRegex();
}