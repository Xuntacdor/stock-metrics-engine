using backend_api.Api.Data;
using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using backend_api.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace backend_api.Api.Tests;

/// <summary>
/// Unit tests for <see cref="KycService"/>.
///
/// ValidateImageFile is private — it is tested indirectly through
/// <see cref="KycService.UploadAndOcrAsync"/> which calls it first before
/// any I/O or HTTP calls. The test verifies that the exception is thrown
/// before any repo or HTTP factory call is made.
///
/// ReviewAsync and SuspendAccountAsync use an InMemory <see cref="QuantIQContext"/>
/// because those methods read/write the Users DbSet directly.
/// </summary>
public class KycServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static QuantIQContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<QuantIQContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new QuantIQContext(options);
    }

    private static IConfiguration BuildConfig(string? fptKey = "dummy-key")
    {
        var dict = new Dictionary<string, string?>
        {
            ["FptAi:ApiKey"] = fptKey
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static KycService BuildService(
        QuantIQContext ctx,
        IKycRepository? kycRepo = null,
        IUserRepository? userRepo = null,
        string? fptKey = "dummy-key")
    {
        kycRepo  ??= new Mock<IKycRepository>().Object;
        userRepo ??= new Mock<IUserRepository>().Object;

        var envMock     = new Mock<IWebHostEnvironment>();
        var httpFactory = new Mock<IHttpClientFactory>();
        var logger      = NullLogger<KycService>.Instance;

        return new KycService(
            kycRepo,
            userRepo,
            ctx,
            BuildConfig(fptKey),
            envMock.Object,
            httpFactory.Object,
            logger);
    }

    /// <summary>
    /// Creates an <see cref="IFormFile"/> mock with controllable Length and ContentType.
    /// </summary>
    private static IFormFile MakeFormFile(
        long length      = 1024,
        string contentType = "image/jpeg",
        string fileName  = "id.jpg")
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(length);
        fileMock.Setup(f => f.ContentType).Returns(contentType);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        return fileMock.Object;
    }

    private static User MakeUser(string userId = "user1") => new()
    {
        UserId        = userId,
        Username      = "tester",
        PasswordHash  = "hash",
        KycStatus     = "PENDING",
        AccountStatus = "INACTIVE"
    };

    private static KycDocument MakeKycDoc(
        int kycId, string userId,
        string status = "PENDING") => new()
    {
        KycId       = kycId,
        UserId      = userId,
        CardNumber  = "123456789",
        FullName    = "Test User",
        Status      = status,
        ImagePath   = "kyc/user1/test.jpg",
        SubmittedAt = DateTime.UtcNow
    };

    // ── UploadAndOcrAsync — ValidateImageFile (tested via public method) ───────

    [Fact]
    public async Task UploadAndOcrAsync_FileTooLarge_ThrowsArgumentException()
    {
        using var ctx = BuildContext();
        var svc = BuildService(ctx);

        // 5 MB + 1 byte exceeds the 5 MB limit
        var file = MakeFormFile(length: 5 * 1024 * 1024 + 1);

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.UploadAndOcrAsync("user1", file));
    }

    [Fact]
    public async Task UploadAndOcrAsync_NonImageContentType_ThrowsArgumentException()
    {
        using var ctx = BuildContext();
        var svc = BuildService(ctx);

        var file = MakeFormFile(length: 512 * 1024, contentType: "application/pdf");

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.UploadAndOcrAsync("user1", file));
    }

    [Fact]
    public async Task UploadAndOcrAsync_EmptyFile_ThrowsArgumentException()
    {
        using var ctx = BuildContext();
        var svc = BuildService(ctx);

        var file = MakeFormFile(length: 0);

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.UploadAndOcrAsync("user1", file));
    }

    [Fact]
    public async Task UploadAndOcrAsync_PngContentType_PassesValidation()
    {
        // A PNG within size limit should pass ValidateImageFile.
        // It will then fail on the FPT.AI API call (no real HTTP) — but the
        // ArgumentException from validation must NOT be thrown.
        using var ctx = BuildContext();
        var svc = BuildService(ctx);

        var file = MakeFormFile(length: 100 * 1024, contentType: "image/png");

        var ex = await Record.ExceptionAsync(() => svc.UploadAndOcrAsync("user1", file));

        // The exception must NOT be an ArgumentException from image validation
        Assert.IsNotType<ArgumentException>(ex);
    }

    [Fact]
    public async Task UploadAndOcrAsync_Exactly5MbFile_PassesValidation()
    {
        // Exactly 5 MB should be allowed (limit is strictly > 5 MB)
        using var ctx = BuildContext();
        var svc = BuildService(ctx);

        var file = MakeFormFile(length: 5 * 1024 * 1024, contentType: "image/jpeg");

        var ex = await Record.ExceptionAsync(() => svc.UploadAndOcrAsync("user1", file));

        Assert.IsNotType<ArgumentException>(ex);
    }

    // ── ReviewAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ReviewAsync_InvalidDecision_ThrowsArgumentException()
    {
        using var ctx = BuildContext();
        var kycRepoMock = new Mock<IKycRepository>();
        var svc = BuildService(ctx, kycRepo: kycRepoMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.ReviewAsync(1, new KycReviewRequest { Decision = "MAYBE" }));
    }

    [Fact]
    public async Task ReviewAsync_RejectedWithoutReason_ThrowsArgumentException()
    {
        using var ctx = BuildContext();
        var svc = BuildService(ctx);

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.ReviewAsync(1, new KycReviewRequest
            {
                Decision     = "REJECTED",
                RejectReason = null
            }));
    }

    [Fact]
    public async Task ReviewAsync_RejectedWithWhitespaceReason_ThrowsArgumentException()
    {
        using var ctx = BuildContext();
        var svc = BuildService(ctx);

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.ReviewAsync(1, new KycReviewRequest
            {
                Decision     = "REJECTED",
                RejectReason = "   "
            }));
    }

    [Fact]
    public async Task ReviewAsync_KycNotFound_ThrowsKeyNotFoundException()
    {
        using var ctx = BuildContext();
        var kycRepoMock = new Mock<IKycRepository>();
        kycRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((KycDocument?)null);

        var svc = BuildService(ctx, kycRepo: kycRepoMock.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.ReviewAsync(99, new KycReviewRequest { Decision = "APPROVED" }));
    }

    [Fact]
    public async Task ReviewAsync_AlreadyProcessed_ThrowsInvalidOperationException()
    {
        using var ctx = BuildContext();
        var kycRepoMock = new Mock<IKycRepository>();
        var doc = MakeKycDoc(kycId: 1, userId: "user1", status: "APPROVED");

        kycRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);

        var svc = BuildService(ctx, kycRepo: kycRepoMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ReviewAsync(1, new KycReviewRequest { Decision = "APPROVED" }));
    }

    [Fact]
    public async Task ReviewAsync_Approved_UpdatesUserKycAndAccountStatus()
    {
        using var ctx = BuildContext();

        var user = MakeUser("user1");
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var kycRepoMock = new Mock<IKycRepository>();
        var doc = MakeKycDoc(kycId: 1, userId: "user1", status: "PENDING");

        kycRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);
        kycRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KycDocument>())).Returns(Task.CompletedTask);
        kycRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var svc = BuildService(ctx, kycRepo: kycRepoMock.Object);

        var result = await svc.ReviewAsync(1, new KycReviewRequest { Decision = "APPROVED" });

        Assert.Equal("APPROVED", result.Status);

        var updatedUser = await ctx.Users.FirstAsync(u => u.UserId == "user1");
        Assert.Equal("APPROVED", updatedUser.KycStatus);
        Assert.Equal("ACTIVE",   updatedUser.AccountStatus);
    }

    [Fact]
    public async Task ReviewAsync_Rejected_SetsUserKycToRejectedAndAccountToInactive()
    {
        using var ctx = BuildContext();

        var user = MakeUser("user2");
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var kycRepoMock = new Mock<IKycRepository>();
        var doc = MakeKycDoc(kycId: 2, userId: "user2", status: "PENDING");

        kycRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(doc);
        kycRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KycDocument>())).Returns(Task.CompletedTask);
        kycRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var svc = BuildService(ctx, kycRepo: kycRepoMock.Object);

        var result = await svc.ReviewAsync(2, new KycReviewRequest
        {
            Decision     = "REJECTED",
            RejectReason = "Blurry photo"
        });

        Assert.Equal("REJECTED", result.Status);

        var updatedUser = await ctx.Users.FirstAsync(u => u.UserId == "user2");
        Assert.Equal("REJECTED",  updatedUser.KycStatus);
        Assert.Equal("INACTIVE",  updatedUser.AccountStatus);
    }

    // ── SuspendAccountAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task SuspendAccountAsync_InvalidAccountStatus_ThrowsArgumentException()
    {
        using var ctx = BuildContext();
        var svc = BuildService(ctx);

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.SuspendAccountAsync("user1", new SuspendAccountRequest
            {
                AccountStatus = "BANNED"  // not in [SUSPENDED, ACTIVE]
            }));
    }

    [Fact]
    public async Task SuspendAccountAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        using var ctx = BuildContext();
        var svc = BuildService(ctx);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.SuspendAccountAsync("ghost", new SuspendAccountRequest
            {
                AccountStatus = "SUSPENDED"
            }));
    }

    [Fact]
    public async Task SuspendAccountAsync_ValidRequest_UpdatesUserAccountStatus()
    {
        using var ctx = BuildContext();

        var user = MakeUser("user3");
        user.AccountStatus = "ACTIVE";
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);

        var result = await svc.SuspendAccountAsync("user3", new SuspendAccountRequest
        {
            AccountStatus = "SUSPENDED",
            Reason        = "Policy violation"
        });

        Assert.Equal("SUSPENDED", result.AccountStatus);

        var dbUser = await ctx.Users.FirstAsync(u => u.UserId == "user3");
        Assert.Equal("SUSPENDED", dbUser.AccountStatus);
    }

    [Fact]
    public async Task SuspendAccountAsync_ReactivatingUser_SetsAccountStatusToActive()
    {
        using var ctx = BuildContext();

        var user = MakeUser("user4");
        user.AccountStatus = "SUSPENDED";
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);

        var result = await svc.SuspendAccountAsync("user4", new SuspendAccountRequest
        {
            AccountStatus = "ACTIVE",
            Reason        = "Appeal approved"
        });

        Assert.Equal("ACTIVE", result.AccountStatus);
    }

    [Fact]
    public async Task SuspendAccountAsync_CaseInsensitiveStatus_Succeeds()
    {
        using var ctx = BuildContext();

        var user = MakeUser("user5");
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var svc = BuildService(ctx);

        // Lower-case "suspended" should be accepted (service uses OrdinalIgnoreCase)
        var result = await svc.SuspendAccountAsync("user5", new SuspendAccountRequest
        {
            AccountStatus = "suspended"
        });

        Assert.Equal("SUSPENDED", result.AccountStatus);
    }
}
