using System.Text.Json;
using StackExchange.Redis;

namespace backend_api.Api.Services;

public class CacheService : ICacheService
{
    private readonly IDatabase _database;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CacheService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task SetTokenBlacklistAsync(string token, TimeSpan expiry)
    {
        await _database.StringSetAsync($"blacklisted:{token}", "revoked", expiry);
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token)
    {
        return await _database.KeyExistsAsync($"blacklisted:{token}");
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var value = await _database.StringGetAsync(key);
        if (value.IsNullOrEmpty) return null;

        try
        {
            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        if (expiry.HasValue)
            await _database.StringSetAsync(key, json, expiry.Value);
        else
            await _database.StringSetAsync(key, json);
    }

    public async Task RemoveAsync(string key)
    {
        await _database.KeyDeleteAsync(key);
    }

    public async Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan expiry)
    {
        return await _database.StringSetAsync(key, value, expiry, When.NotExists);
    }
}
