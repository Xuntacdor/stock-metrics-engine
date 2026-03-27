using backend_api.Api.DTOs;

namespace backend_api.Api.Repositories;

public interface ILeaderboardRepository
{
    Task<IReadOnlyList<LeaderboardEntryDto>> GetTopTradersAsync(int limit);
}
