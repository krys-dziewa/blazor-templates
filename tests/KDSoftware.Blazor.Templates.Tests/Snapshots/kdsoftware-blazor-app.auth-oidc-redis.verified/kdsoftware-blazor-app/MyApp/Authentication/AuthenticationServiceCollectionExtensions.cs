using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace MyApp.Authentication;

internal static class AuthenticationServiceCollectionExtensions
{
    public const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string OidcScheme = OpenIdConnectDefaults.AuthenticationScheme;

    /// <summary>
    /// Adds server-side OpenID Connect sign-in with a cookie session. Tokens are kept in the session store
    /// (see <see cref="SessionStoreServiceCollectionExtensions.AddDistributedSessionStore"/>) and are never
    /// exposed to the browser or to WebAssembly code.
    /// </summary>
    public static IServiceCollection AddOidcBffAuthentication(this IServiceCollection services)
    {
        services.AddOptions<OidcSettings>()
            .BindConfiguration(OidcSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieScheme;
                options.DefaultChallengeScheme = OidcScheme;
            })
            .AddCookie(CookieScheme, options =>
            {
                // The __Host- prefix makes the browser reject the cookie unless it is Secure, host-only and scoped to "/".
                options.Cookie.Name = "__Host-MyApp";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Events.OnRedirectToAccessDenied = static context =>
                {
                    if (context.Request.IsBffApiRequest())
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            })
            .AddOpenIdConnect(OidcScheme, options =>
            {
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.SaveTokens = true;
                options.MapInboundClaims = false;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Add(OpenIdConnectScope.OfflineAccess);
                options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                options.TokenValidationParameters.RoleClaimType = "role";
                options.Events.OnRedirectToIdentityProvider = static context =>
                {
                    // Browser fetch calls cannot follow a redirect to the identity provider; tell them to sign in instead.
                    if (context.Request.IsBffApiRequest())
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.HandleResponse();
                    }

                    return Task.CompletedTask;
                };
            });

        services.AddOptions<OpenIdConnectOptions>(OidcScheme)
            .Configure<IOptions<OidcSettings>>(static (options, settings) =>
            {
                OidcSettings oidc = settings.Value;
                options.Authority = oidc.Authority;
                options.ClientId = oidc.ClientId;
                options.ClientSecret = oidc.ClientSecret;
                foreach (string scope in oidc.Scopes)
                {
                    options.Scope.Add(scope);
                }
            });

        services.AddSingleton<CookieOidcRefresher>();
        services.AddOptions<CookieAuthenticationOptions>(CookieScheme)
            .Configure<CookieOidcRefresher>(static (options, refresher) =>
                options.Events.OnValidatePrincipal = context => refresher.ValidateOrRefreshCookieAsync(context, OidcScheme));

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        return services;
    }
}