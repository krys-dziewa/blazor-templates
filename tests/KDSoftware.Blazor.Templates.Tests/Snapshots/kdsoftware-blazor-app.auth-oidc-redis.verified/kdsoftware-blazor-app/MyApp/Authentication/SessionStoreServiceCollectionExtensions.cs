using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.Extensions.Caching.StackExchangeRedis;

using StackExchange.Redis;

namespace MyApp.Authentication;

internal static class SessionStoreServiceCollectionExtensions
{
    public const string ConnectionStringName = "SessionStore";

    /// <summary>
    /// Stores cookie sessions in Valkey or Redis and shares the Data Protection keys through the same server,
    /// so that every instance can read the session cookie and the stored tickets survive restarts.
    /// </summary>
    public static IServiceCollection AddDistributedSessionStore(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"The connection string '{ConnectionStringName}' is required to store sessions in Valkey or Redis. Set 'ConnectionStrings:{ConnectionStringName}'.");
        }

        // One multiplexer is shared by the cache and the Data Protection key repository.
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));

        services.AddStackExchangeRedisCache(static _ => { });
        services.AddOptions<RedisCacheOptions>()
            .Configure<IConnectionMultiplexer>(static (options, connection) =>
            {
                options.InstanceName = "MyApp:";
                options.ConnectionMultiplexerFactory = () => Task.FromResult(connection);
            });

        services.AddDataProtection().SetApplicationName("MyApp");
        services.AddOptions<KeyManagementOptions>()
            .Configure<IConnectionMultiplexer>(static (options, connection) =>
                options.XmlRepository = new RedisXmlRepository(() => connection.GetDatabase(), "MyApp:DataProtection-Keys"));

        services.AddSingleton<DistributedCacheTicketStore>();
        services.AddOptions<CookieAuthenticationOptions>(AuthenticationServiceCollectionExtensions.CookieScheme)
            .Configure<DistributedCacheTicketStore>(static (options, store) => options.SessionStore = store);

        return services;
    }
}