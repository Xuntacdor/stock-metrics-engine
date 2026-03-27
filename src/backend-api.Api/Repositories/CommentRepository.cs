using backend_api.Api.Data;
using backend_api.Api.DTOs;
using backend_api.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly QuantIQContext _ctx;
    public CommentRepository(QuantIQContext ctx) => _ctx = ctx;

    public async Task<List<CommentDto>> GetBySymbolAsync(string symbol, int limit = 50)
    {
        return await _ctx.StockComments
            .Where(c => c.Symbol == symbol.ToUpper())
            .OrderByDescending(c => c.CreatedAt)
            .Take(Math.Min(limit, 200))
            .Select(c => new CommentDto(
                c.CommentId,
                c.Symbol,
                c.UserId,
                c.User.Username ?? c.UserId,
                c.Content,
                c.CreatedAt))
            .ToListAsync();
    }

    public async Task<CommentDto> CreateAsync(string symbol, string userId, string content)
    {
        var user = await _ctx.Users.FindAsync(userId);
        var comment = new StockComment
        {
            Symbol    = symbol.ToUpper(),
            UserId    = userId,
            Content   = content.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
        _ctx.StockComments.Add(comment);
        await _ctx.SaveChangesAsync();
        return new CommentDto(comment.CommentId, comment.Symbol, userId, user?.Username ?? userId, content, comment.CreatedAt);
    }

    public async Task<bool> DeleteAsync(int commentId, string requestingUserId, bool isAdmin)
    {
        var c = await _ctx.StockComments.FindAsync(commentId);
        if (c == null) return false;
        if (!isAdmin && c.UserId != requestingUserId) return false;
        _ctx.StockComments.Remove(c);
        await _ctx.SaveChangesAsync();
        return true;
    }
}
