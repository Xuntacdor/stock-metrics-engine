using System.Security.Claims;
using backend_api.Api.DTOs;
using backend_api.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ICommentRepository _comments;
    public CommentsController(ICommentRepository comments) => _comments = comments;

    /// <summary>GET /api/comments/{symbol}?limit=50</summary>
    [HttpGet("{symbol}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetComments(string symbol, [FromQuery] int limit = 50)
    {
        var list = await _comments.GetBySymbolAsync(symbol, limit);
        return Ok(list);
    }

    /// <summary>POST /api/comments/{symbol}</summary>
    [HttpPost("{symbol}")]
    [Authorize]
    public async Task<IActionResult> CreateComment(string symbol, [FromBody] CreateCommentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Content))
            return BadRequest("Nội dung bình luận không được để trống.");
        if (req.Content.Length > 2000)
            return BadRequest("Nội dung tối đa 2000 ký tự.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new UnauthorizedAccessException();

        var comment = await _comments.CreateAsync(symbol, userId, req.Content);
        return Created($"/api/comments/{symbol}/{comment.CommentId}", comment);
    }

    /// <summary>DELETE /api/comments/{id}</summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");
        var deleted = await _comments.DeleteAsync(id, userId, isAdmin);
        return deleted ? NoContent() : NotFound();
    }
}
