using StackExchange.Redis;

namespace backend_api.Api.Services;

public class CacheService : ICacheService
{
    private readonly IDatabase _database;

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
}
