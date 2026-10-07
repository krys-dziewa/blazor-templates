#if (UseOidc)
using Microsoft.AspNetCore.Authentication;

#endif
using Yarp.ReverseProxy.Transforms;

namespace BlazorBff.Bff;

internal static class ReverseProxyServiceCollectionExtensions
{
    public const string ConfigurationSectionName = "ReverseProxy";

    /// <summary>
    /// Adds YARP so that the BFF can forward <c>/api/...</c> calls from the browser to downstream services.
    /// Routes and clusters are configured in the <c>ReverseProxy</c> configuration section.
    /// </summary>
    public static IServiceCollection AddBffReverseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection(ConfigurationSectionName))
            .AddTransforms(static context =>
            {
                // The session cookie and the CSRF header are only meaningful to the BFF itself.
                context.AddRequestHeaderRemove("Cookie");
                context.AddRequestHeaderRemove(CsrfHeaderHandler.HeaderName);
#if (UseOidc)

                // Exchange the session cookie for the user's access token, which never leaves the server.
                context.AddRequestTransform(static async transformContext =>
                {
                    string? accessToken = await transformContext.HttpContext.GetTokenAsync("access_token");
                    if (accessToken is not null)
                    {
                        transformContext.ProxyRequest.Headers.Authorization = new("Bearer", accessToken);
                    }
                });
#endif
            });

        return services;
    }
}