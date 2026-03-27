using backend_api.Api.DTOs;

namespace backend_api.Api.Repositories;

public interface ICommentRepository
{
    Task<List<CommentDto>> GetBySymbolAsync(string symbol, int limit = 50);
    Task<CommentDto> CreateAsync(string symbol, string userId, string content);
    Task<bool> DeleteAsync(int commentId, string requestingUserId, bool isAdmin);
}
