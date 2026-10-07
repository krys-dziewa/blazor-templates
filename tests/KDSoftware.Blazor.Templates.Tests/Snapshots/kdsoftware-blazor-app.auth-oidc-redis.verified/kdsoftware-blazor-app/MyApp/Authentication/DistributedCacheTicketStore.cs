using System.Buffers.Text;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace MyApp.Authentication;

/// <summary>
/// Keeps the authentication ticket (claims and OpenID Connect tokens) in a distributed cache, so the session
/// cookie only carries an opaque key. Tickets are encrypted with Data Protection before they leave the process.
/// </summary>
internal sealed class DistributedCacheTicketStore : ITicketStore
{
    private const string KeyPrefix = "session:";

    // Used only when the cookie handler did not set an expiration; matches the default cookie lifetime.
    private static readonly TimeSpan FallbackLifetime = TimeSpan.FromDays(14);

    private readonly IDistributedCache _cache;
    private readonly IDataProtector _protector;
    private readonly ILogger<DistributedCacheTicketStore> _logger;

    public DistributedCacheTicketStore(
        IDistributedCache cache,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<DistributedCacheTicketStore> logger)
    {
        _cache = cache;
        _protector = dataProtectionProvider.CreateProtector("MyApp.SessionTicket");
        _logger = logger;
    }

    public Task<string> StoreAsync(AuthenticationTicket ticket) => StoreAsync(ticket, CancellationToken.None);

    public async Task<string> StoreAsync(AuthenticationTicket ticket, CancellationToken cancellationToken)
    {
        string key = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        await RenewAsync(key, ticket, cancellationToken);
        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket) => RenewAsync(key, ticket, CancellationToken.None);

    public Task RenewAsync(string key, AuthenticationTicket ticket, CancellationToken cancellationToken)
    {
        byte[] protectedTicket = _protector.Protect(TicketSerializer.Default.Serialize(ticket));
        DistributedCacheEntryOptions entryOptions = ticket.Properties.ExpiresUtc is { } expiresUtc
            ? new() { AbsoluteExpiration = expiresUtc }
            : new() { AbsoluteExpirationRelativeToNow = FallbackLifetime };

        return _cache.SetAsync(KeyPrefix + key, protectedTicket, entryOptions, cancellationToken);
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key) => RetrieveAsync(key, CancellationToken.None);

    public async Task<AuthenticationTicket?> RetrieveAsync(string key, CancellationToken cancellationToken)
    {
        byte[]? protectedTicket = await _cache.GetAsync(KeyPrefix + key, cancellationToken);
        if (protectedTicket is null)
        {
            // Expired, signed out or evicted: the cookie handler treats the request as anonymous.
            return null;
        }

        try
        {
            return TicketSerializer.Default.Deserialize(_protector.Unprotect(protectedTicket));
        }
        catch (CryptographicException exception)
        {
            // The key that protected the ticket is no longer available; the user has to sign in again.
            _logger.LogWarning(exception, "Discarding a session ticket that could not be decrypted.");
            return null;
        }
    }

    public Task RemoveAsync(string key) => RemoveAsync(key, CancellationToken.None);

    public Task RemoveAsync(string key, CancellationToken cancellationToken) =>
        _cache.RemoveAsync(KeyPrefix + key, cancellationToken);
}