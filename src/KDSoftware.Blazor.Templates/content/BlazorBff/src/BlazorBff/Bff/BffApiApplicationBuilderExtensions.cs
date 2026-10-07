using Microsoft.AspNetCore.Diagnostics;

namespace BlazorBff.Bff;

internal static class BffApiApplicationBuilderExtensions
{
    public const string ApiPathPrefix = "/api";

    public static bool IsBffApiRequest(this HttpRequest request)
    {
        return request.Path.StartsWithSegments(ApiPathPrefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Protects the cookie-authenticated BFF API surface (local endpoints and proxied routes under <c>/api</c>).
    /// </summary>
    /// <remarks>
    /// Requiring a custom header forces browsers to send a CORS preflight for cross-origin requests,
    /// which this application never approves, so other sites cannot call the API with the user's cookie.
    /// </remarks>
    public static IApplicationBuilder UseBffApiProtection(this IApplicationBuilder app)
    {
        return app.Use(static (context, next) =>
        {
            if (!context.Request.IsBffApiRequest())
            {
                return next(context);
            }

            // API callers expect plain status codes rather than re-executed HTML error pages.
            if (context.Features.Get<IStatusCodePagesFeature>() is { } statusCodePages)
            {
                statusCodePages.Enabled = false;
            }

            if (!context.Request.Headers.ContainsKey(CsrfHeaderHandler.HeaderName))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Task.CompletedTask;
            }

            return next(context);
        });
    }
}