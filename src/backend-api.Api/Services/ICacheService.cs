namespace backend_api.Api.Services;

public interface ICacheService
{
    Task SetTokenBlacklistAsync(string token, TimeSpan expiry);
    Task<bool> IsTokenBlacklistedAsync(string token);
}