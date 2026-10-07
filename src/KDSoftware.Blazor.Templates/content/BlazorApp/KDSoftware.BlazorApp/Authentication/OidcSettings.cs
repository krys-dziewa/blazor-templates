using System.ComponentModel.DataAnnotations;

namespace KDSoftware.BlazorApp.Authentication;

internal sealed class OidcSettings
{
    public const string SectionName = "Authentication:Oidc";

    [Required]
    [Url]
    public string Authority { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Store the client secret in user secrets, environment variables or a secret store, never in appsettings.json.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Scopes requested in addition to <c>openid</c>, <c>profile</c> and <c>offline_access</c>.
    /// </summary>
    public IList<string> Scopes { get; } = [];
}