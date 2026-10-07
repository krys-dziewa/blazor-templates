using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace MyApp.Authentication;

/// <summary>
/// Refreshes the access token stored in the session cookie shortly before it expires, so that server-side
/// calls to downstream APIs always carry a valid token. Based on the ASP.NET Core BlazorWebAppOidcBff sample.
/// </summary>
internal sealed class CookieOidcRefresher
{
    private static readonly TimeSpan RefreshThreshold = TimeSpan.FromMinutes(5);

    private readonly IOptionsMonitor<OpenIdConnectOptions> _oidcOptionsMonitor;

    private readonly OpenIdConnectProtocolValidator _oidcTokenValidator = new()
    {
        // The nonce cookie was deleted at the end of the authorization code flow and nonces are not
        // intended for refresh requests, so oidcOptions.ProtocolValidator cannot be used here.
        RequireNonce = false,
    };

    public CookieOidcRefresher(IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor)
    {
        _oidcOptionsMonitor = oidcOptionsMonitor;
    }

    public async Task ValidateOrRefreshCookieAsync(CookieValidatePrincipalContext validateContext, string oidcScheme)
    {
        string? accessTokenExpirationText = validateContext.Properties.GetTokenValue("expires_at");
        if (!DateTimeOffset.TryParse(accessTokenExpirationText, CultureInfo.InvariantCulture, out DateTimeOffset accessTokenExpiration))
        {
            return;
        }

        OpenIdConnectOptions oidcOptions = _oidcOptionsMonitor.Get(oidcScheme);
        DateTimeOffset now = (oidcOptions.TimeProvider ?? TimeProvider.System).GetUtcNow();
        if (now + RefreshThreshold < accessTokenExpiration)
        {
            return;
        }

        CancellationToken cancellationToken = validateContext.HttpContext.RequestAborted;
        IConfigurationManager<OpenIdConnectConfiguration> configurationManager = oidcOptions.ConfigurationManager
            ?? throw new InvalidOperationException("Cannot refresh cookie. The OpenID Connect configuration manager is not initialized.");
        OpenIdConnectConfiguration oidcConfiguration = await configurationManager.GetConfigurationAsync(cancellationToken);
        string tokenEndpoint = oidcConfiguration.TokenEndpoint
            ?? throw new InvalidOperationException("Cannot refresh cookie. The identity provider does not expose a token endpoint.");

        using FormUrlEncodedContent refreshRequest = new(new Dictionary<string, string?>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = oidcOptions.ClientId,
            ["client_secret"] = oidcOptions.ClientSecret,
            ["scope"] = string.Join(' ', oidcOptions.Scope),
            ["refresh_token"] = validateContext.Properties.GetTokenValue("refresh_token"),
        });
        using HttpResponseMessage refreshResponse = await oidcOptions.Backchannel.PostAsync(tokenEndpoint, refreshRequest, cancellationToken);

        if (!refreshResponse.IsSuccessStatusCode)
        {
            validateContext.RejectPrincipal();
            return;
        }

        string refreshJson = await refreshResponse.Content.ReadAsStringAsync(cancellationToken);
        OpenIdConnectMessage message = new(refreshJson);

        TokenValidationParameters validationParameters = oidcOptions.TokenValidationParameters.Clone();
        if (configurationManager is BaseConfigurationManager baseConfigurationManager)
        {
            validationParameters.ConfigurationManager = baseConfigurationManager;
        }
        else
        {
            validationParameters.ValidIssuer = oidcConfiguration.Issuer;
            validationParameters.IssuerSigningKeys = oidcConfiguration.SigningKeys;
        }

        TokenValidationResult validationResult = await oidcOptions.TokenHandler.ValidateTokenAsync(message.IdToken, validationParameters);
        if (!validationResult.IsValid || validationResult.SecurityToken is not JsonWebToken idToken)
        {
            validateContext.RejectPrincipal();
            return;
        }

        JwtSecurityToken validatedIdToken = JwtSecurityTokenConverter.Convert(idToken);
        validatedIdToken.Payload["nonce"] = null;
        _oidcTokenValidator.ValidateTokenResponse(new()
        {
            ProtocolMessage = message,
            ClientId = oidcOptions.ClientId,
            ValidatedIdToken = validatedIdToken,
        });

        validateContext.ShouldRenew = true;
        validateContext.ReplacePrincipal(new ClaimsPrincipal(validationResult.ClaimsIdentity));

        int expiresIn = int.Parse(message.ExpiresIn, NumberStyles.Integer, CultureInfo.InvariantCulture);
        DateTimeOffset expiresAt = now + TimeSpan.FromSeconds(expiresIn);
        validateContext.Properties.StoreTokens(
        [
            new() { Name = "access_token", Value = message.AccessToken },
            new() { Name = "id_token", Value = message.IdToken },
            new() { Name = "refresh_token", Value = message.RefreshToken },
            new() { Name = "token_type", Value = message.TokenType },
            new() { Name = "expires_at", Value = expiresAt.ToString("o", CultureInfo.InvariantCulture) },
        ]);
    }
}