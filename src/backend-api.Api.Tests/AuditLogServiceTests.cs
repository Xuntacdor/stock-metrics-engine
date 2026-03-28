using backend_api.Api.Models;
using backend_api.Api.Repositories;
using backend_api.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace backend_api.Api.Tests;

/// <summary>
/// Unit tests for <see cref="AuditLogService"/>.
/// All repository calls are mocked. The service must never rethrow exceptions
/// from the audit path — business flow should not be interrupted.
/// </summary>
public class AuditLogServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (AuditLogService svc, Mock<IAuditLogRepository> repoMock) Build()
    {
        var repoMock = new Mock<IAuditLogRepository>();
        var logger   = NullLogger<AuditLogService>.Instance;
        var svc      = new AuditLogService(repoMock.Object, logger);
        return (svc, repoMock);
    }

    // ── LogAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LogAsync_HappyPath_AddsLogAndSaves()
    {
        var (svc, repo) = Build();

        repo.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .Returns(Task.CompletedTask)
            .Verifiable();
        repo.Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act — should not throw
        await svc.LogAsync("user1", "PlaceOrder", new { OrderId = 99 }, "127.0.0.1");

        repo.Verify(r => r.AddAsync(It.Is<AuditLog>(
            l => l.UserId == "user1"
              && l.Action == "PlaceOrder"
              && l.IpAddress == "127.0.0.1"
              && l.Detail != null)), Times.Once);

        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task LogAsync_WithNullDetail_SerializesNullCorrectly()
    {
        var (svc, repo) = Build();

        AuditLog? captured = null;

        repo.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(l => captured = l)
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await svc.LogAsync("user2", "Logout");

        Assert.NotNull(captured);
        Assert.Null(captured!.Detail);
    }

    [Fact]
    public async Task LogAsync_RepoThrows_DoesNotRethrow()
    {
        // The service must swallow exceptions so the main business flow is not broken.
        var (svc, repo) = Build();

        repo.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
            .ThrowsAsync(new InvalidOperationException("DB unavailable"));

        // Act — must complete without throwing
        var exception = await Record.ExceptionAsync(() => svc.LogAsync("user1", "Action"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task LogAsync_SaveChangesThrows_DoesNotRethrow()
    {
        var (svc, repo) = Build();

        repo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new Exception("Save failed"));

        var exception = await Record.ExceptionAsync(() => svc.LogAsync("user1", "Action"));

        Assert.Null(exception);
    }

    // ── GetLogsByUserAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetLogsByUserAsync_DelegatesToRepo_ReturnsCorrectLogs()
    {
        var (svc, repo) = Build();
        var expected = new List<AuditLog>
        {
            new() { AuditLogId = 1, UserId = "user1", Action = "Login" },
            new() { AuditLogId = 2, UserId = "user1", Action = "PlaceOrder" }
        };

        repo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(expected);

        var result = (await svc.GetLogsByUserAsync("user1")).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, l => Assert.Equal("user1", l.UserId));
        repo.Verify(r => r.GetByUserIdAsync("user1"), Times.Once);
    }

    [Fact]
    public async Task GetLogsByUserAsync_NoLogs_ReturnsEmptyEnumerable()
    {
        var (svc, repo) = Build();

        repo.Setup(r => r.GetByUserIdAsync(It.IsAny<string>()))
            .ReturnsAsync(Enumerable.Empty<AuditLog>());

        var result = await svc.GetLogsByUserAsync("unknownUser");

        Assert.Empty(result);
    }

    // ── GetAllLogsPagedAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAllLogsPagedAsync_DelegatesToRepo_WithCorrectPageParams()
    {
        var (svc, repo) = Build();
        var expected = new List<AuditLog>
        {
            new() { AuditLogId = 10, UserId = "u1", Action = "X" }
        };

        repo.Setup(r => r.GetAllPagedAsync(2, 25))
            .ReturnsAsync(expected);

        var result = (await svc.GetAllLogsPagedAsync(2, 25)).ToList();

        Assert.Single(result);
        Assert.Equal(10, result[0].AuditLogId);
        repo.Verify(r => r.GetAllPagedAsync(2, 25), Times.Once);
    }

    [Fact]
    public async Task GetAllLogsPagedAsync_Page1Size10_PassesParamsThrough()
    {
        var (svc, repo) = Build();

        repo.Setup(r => r.GetAllPagedAsync(1, 10))
            .ReturnsAsync(new List<AuditLog>())
            .Verifiable();

        await svc.GetAllLogsPagedAsync(1, 10);

        repo.Verify(r => r.GetAllPagedAsync(1, 10), Times.Once);
    }
}
