namespace backend_api.Api.Services;

public interface ICacheService
{
    Task SetTokenBlacklistAsync(string token, TimeSpan expiry);
    Task<bool> IsTokenBlacklistedAsync(string token);

    /// <summary>Get a cached value by key. Returns null if not found or expired.</summary>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>Store a value in cache with optional TTL.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

    /// <summary>Remove a key from cache.</summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Set a key only if it does not already exist (NX semantics).
    /// Returns true if the key was set, false if it already existed.
    /// Used for distributed locking / idempotency guards.
    /// </summary>
    Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan expiry);
}