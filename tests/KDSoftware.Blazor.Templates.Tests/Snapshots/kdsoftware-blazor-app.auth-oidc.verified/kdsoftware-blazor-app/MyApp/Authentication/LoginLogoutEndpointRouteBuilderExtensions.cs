using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Authentication;

internal static class LoginLogoutEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapLoginAndLogout(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/authentication");

        group.MapGet("/login", static (string? returnUrl) => TypedResults.Challenge(GetAuthProperties(returnUrl)))
            .AllowAnonymous();

        // Logout is a POST so that it is covered by antiforgery validation and cannot be triggered by a link.
        group.MapPost("/logout", static ([FromForm] string? returnUrl) => TypedResults.SignOut(
            GetAuthProperties(returnUrl),
            [AuthenticationServiceCollectionExtensions.CookieScheme, AuthenticationServiceCollectionExtensions.OidcScheme]));

        return group;
    }

    private static AuthenticationProperties GetAuthProperties(string? returnUrl)
    {
        // Only local URLs are accepted to prevent open redirects.
        return new AuthenticationProperties
        {
            RedirectUri = !string.IsNullOrEmpty(returnUrl) && IsLocalUrl(returnUrl) ? returnUrl : "/",
        };
    }

    private static bool IsLocalUrl(string url)
    {
        return url[0] == '/'
            && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))
            && !url.Any(char.IsControl);
    }
}