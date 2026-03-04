using System.Net.Http.Headers;
using System.Text.Json;
using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using backend_api.Api.Data;

namespace backend_api.Api.Services;

public class KycService : IKycService
{
    private readonly IKycRepository _kycRepo;
    private readonly IUserRepository _userRepo;
    private readonly QuantIQContext _context;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<KycService> _logger;

    private const string FptAiUrl = "https://api.fpt.ai/vision/idr/vnm/";

    public KycService(
        IKycRepository kycRepo,
        IUserRepository userRepo,
        QuantIQContext context,
        IConfiguration config,
        IWebHostEnvironment env,
        IHttpClientFactory httpClientFactory,
        ILogger<KycService> logger)
    {
        _kycRepo = kycRepo;
        _userRepo = userRepo;
        _context = context;
        _config = config;
        _env = env;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }


    public async Task<KycUploadResponse> UploadAndOcrAsync(string userId, IFormFile image)
    {
        ValidateImageFile(image);

        var savedPath = await SaveImageAsync(image, userId);

        var ocrData = await CallFptAiOcrAsync(image);

        var doc = new KycDocument
        {
            UserId      = userId,
            CardNumber  = ocrData?.id,
            FullName    = ocrData?.name,
            DateOfBirth = ocrData?.dob,
            Sex         = ocrData?.sex,
            Nationality = ocrData?.nationality,
            HomeTown    = ocrData?.home,
            Address     = ocrData?.address,
            ExpiryDate  = ocrData?.doe,
            CardType    = ocrData?.type,
            ImagePath   = savedPath,
            Status      = "PENDING",
            SubmittedAt = DateTime.UtcNow
        };

        await _kycRepo.AddAsync(doc);
        await _kycRepo.SaveChangesAsync();

        _logger.LogInformation("KYC submitted by user {UserId}, KycId={KycId}", userId, doc.KycId);

        return MapToUploadResponse(doc, "Ảnh CCCD đã được nhận. Vui lòng chờ Admin xét duyệt.");
    }


    public async Task<IEnumerable<KycDetailResponse>> GetByUserIdAsync(string userId)
    {
        var docs = await _kycRepo.GetByUserIdAsync(userId);
        return docs.Select(MapToDetailResponse);
    }

    public async Task<UserProfileResponse> GetMyProfileAsync(string userId)
    {
        var user = await _context.Users
            .Include(u => u.KycDocuments)
            .FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        var latestKyc = user.KycDocuments
            .OrderByDescending(k => k.SubmittedAt)
            .FirstOrDefault();

        return new UserProfileResponse
        {
            UserId        = user.UserId,
            Username      = user.Username,
            Email         = user.Email,
            CreatedAt     = user.CreatedAt,
            AccountStatus = user.AccountStatus,
            KycStatus     = user.KycStatus,
            NextStep      = ResolveNextStep(user.AccountStatus, user.KycStatus, latestKyc?.Status),
            LatestKyc     = latestKyc == null ? null : new LatestKycInfo
            {
                KycId        = latestKyc.KycId,
                Status       = latestKyc.Status,
                RejectReason = latestKyc.RejectReason,
                SubmittedAt  = latestKyc.SubmittedAt,
                ReviewedAt   = latestKyc.ReviewedAt
            }
        };
    }

    public async Task<IEnumerable<KycDetailResponse>> GetPendingAsync()
    {
        var docs = await _kycRepo.GetPendingAsync();
        return docs.Select(MapToDetailResponse);
    }


    public async Task<KycDetailResponse> ReviewAsync(int kycId, KycReviewRequest request)
    {
        var valid = new[] { "APPROVED", "REJECTED" };
        if (!valid.Contains(request.Decision, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Decision phải là APPROVED hoặc REJECTED.");

        if (request.Decision.ToUpper() == "REJECTED" && string.IsNullOrWhiteSpace(request.RejectReason))
            throw new ArgumentException("Phải cung cấp lý do khi từ chối KYC.");

        var doc = await _kycRepo.GetByIdAsync(kycId)
            ?? throw new KeyNotFoundException($"Không tìm thấy KYC #{kycId}.");

        if (doc.Status != "PENDING")
            throw new InvalidOperationException($"KYC #{kycId} đã được xử lý (Status = {doc.Status}).");

        doc.Status       = request.Decision.ToUpper();
        doc.RejectReason = request.RejectReason;
        doc.ReviewedAt   = DateTime.UtcNow;

        await _kycRepo.UpdateAsync(doc);

        // ─── Đồng bộ trạng thái lên bảng Users ─────────────────────────────
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == doc.UserId);
        if (user != null)
        {
            user.KycStatus = doc.Status;

            // Khi APPROVED → Kích hoạt tài khoản để user được phép giao dịch
            // Khi REJECTED → Giữ INACTIVE, user phải nộp lại CCCD
            user.AccountStatus = doc.Status == "APPROVED" ? "ACTIVE" : "INACTIVE";

            _context.Users.Update(user);
        }

        await _kycRepo.SaveChangesAsync();

        _logger.LogInformation(
            "KYC #{KycId} reviewed: {Decision} → AccountStatus={AccountStatus}.",
            kycId, doc.Status, user?.AccountStatus);

        return MapToDetailResponse(doc);
    }


    private static void ValidateImageFile(IFormFile image)
    {
        if (image.Length == 0)
            throw new ArgumentException("File ảnh không được rỗng.");

        if (image.Length > 5 * 1024 * 1024)
            throw new ArgumentException("File ảnh không được vượt quá 5 MB.");

        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
        if (!allowedContentTypes.Contains(image.ContentType.ToLower()))
            throw new ArgumentException("Chỉ chấp nhận file ảnh định dạng JPEG hoặc PNG.");
    }

    private async Task<string> SaveImageAsync(IFormFile image, string userId)
    {
        var folder = Path.Combine(_env.WebRootPath ?? "wwwroot", "kyc", userId);
        Directory.CreateDirectory(folder);

        var extension = Path.GetExtension(image.FileName);
        var fileName  = $"{Guid.NewGuid()}{extension}";
        var filePath  = Path.Combine(folder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await image.CopyToAsync(stream);

        return Path.Combine("kyc", userId, fileName).Replace("\\", "/");
    }

    
    private async Task<FptAiOcrData?> CallFptAiOcrAsync(IFormFile image)
    {
        var apiKey = _config["FptAi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("FPT.AI API Key chưa được cấu hình (FptAi:ApiKey).");

        var client = _httpClientFactory.CreateClient("FptAi");
        client.DefaultRequestHeaders.Add("api-key", apiKey);

        image.OpenReadStream().Seek(0, SeekOrigin.Begin);

        using var content = new MultipartFormDataContent();
        var imageContent = new StreamContent(image.OpenReadStream());
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
        content.Add(imageContent, "image", image.FileName);

        var response = await client.PostAsync(FptAiUrl, content);
        var body     = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("FPT.AI OCR failed: {Status} - {Body}", response.StatusCode, body);
            throw new HttpRequestException($"FPT.AI trả về lỗi: {response.StatusCode}");
        }

        var ocrResult = JsonSerializer.Deserialize<FptAiOcrResponse>(body);

        if (ocrResult?.errorCode != 0 || ocrResult.data == null || ocrResult.data.Count == 0)
        {
            _logger.LogWarning("FPT.AI OCR errorCode={Code} msg={Msg}", ocrResult?.errorCode, ocrResult?.errorMessage);
            throw new InvalidOperationException(
                $"Không thể đọc thông tin CCCD. {TranslateFptError(ocrResult?.errorCode)}");
        }

        return ocrResult.data[0];
    }

    private static string TranslateFptError(int? code) => code switch
    {
        1  => "Tham số không hợp lệ.",
        2  => "Không thể cắt ảnh – CCCD bị thiếu góc.",
        3  => "Không tìm thấy CCCD trong ảnh hoặc ảnh quá mờ/tối.",
        7  => "File không phải là ảnh hợp lệ.",
        8  => "File ảnh bị lỗi hoặc định dạng không được hỗ trợ.",
        _  => "Vui lòng kiểm tra lại ảnh CCCD."
    };


    private static KycUploadResponse MapToUploadResponse(KycDocument doc, string message) => new()
    {
        KycId       = doc.KycId,
        Status      = doc.Status,
        CardNumber  = doc.CardNumber,
        FullName    = doc.FullName,
        DateOfBirth = doc.DateOfBirth,
        Sex         = doc.Sex,
        Nationality = doc.Nationality,
        HomeTown    = doc.HomeTown,
        Address     = doc.Address,
        ExpiryDate  = doc.ExpiryDate,
        CardType    = doc.CardType,
        SubmittedAt = doc.SubmittedAt,
        Message     = message
    };

    private static KycDetailResponse MapToDetailResponse(KycDocument doc) => new()
    {
        KycId        = doc.KycId,
        UserId       = doc.UserId,
        CardNumber   = doc.CardNumber,
        FullName     = doc.FullName,
        DateOfBirth  = doc.DateOfBirth,
        Sex          = doc.Sex,
        Nationality  = doc.Nationality,
        HomeTown     = doc.HomeTown,
        Address      = doc.Address,
        ExpiryDate   = doc.ExpiryDate,
        CardType     = doc.CardType,
        ImagePath    = doc.ImagePath,
        Status       = doc.Status,
        RejectReason = doc.RejectReason,
        SubmittedAt  = doc.SubmittedAt,
        ReviewedAt   = doc.ReviewedAt
    };

    // ─── Admin suspend / unsuspend ────────────────────────────────────────────

    public async Task<UserProfileResponse> SuspendAccountAsync(string targetUserId, SuspendAccountRequest request)
    {
        var allowed = new[] { "SUSPENDED", "ACTIVE" };
        if (!allowed.Contains(request.AccountStatus, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("AccountStatus phải là SUSPENDED hoặc ACTIVE.");

        var user = await _context.Users
            .Include(u => u.KycDocuments)
            .FirstOrDefaultAsync(u => u.UserId == targetUserId)
            ?? throw new KeyNotFoundException($"Không tìm thấy user #{targetUserId}.");

        var prev = user.AccountStatus;
        user.AccountStatus = request.AccountStatus.ToUpper();
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Admin changed AccountStatus of user {UserId}: {Prev} → {New}. Reason: {Reason}",
            targetUserId, prev, user.AccountStatus, request.Reason);

        var latestKyc = user.KycDocuments.OrderByDescending(k => k.SubmittedAt).FirstOrDefault();

        return new UserProfileResponse
        {
            UserId        = user.UserId,
            Username      = user.Username,
            Email         = user.Email,
            CreatedAt     = user.CreatedAt,
            AccountStatus = user.AccountStatus,
            KycStatus     = user.KycStatus,
            NextStep      = ResolveNextStep(user.AccountStatus, user.KycStatus, latestKyc?.Status),
            LatestKyc     = latestKyc == null ? null : new LatestKycInfo
            {
                KycId        = latestKyc.KycId,
                Status       = latestKyc.Status,
                RejectReason = latestKyc.RejectReason,
                SubmittedAt  = latestKyc.SubmittedAt,
                ReviewedAt   = latestKyc.ReviewedAt
            }
        };
    }

    // ─── Helper: hướng dẫn bước tiếp theo cho user ───────────────────────────

    private static string ResolveNextStep(string accountStatus, string kycStatus, string? latestKycStatus)
        => (accountStatus, kycStatus, latestKycStatus) switch
        {
            // Tài khoản đang bị khóa
            ("SUSPENDED", _, _)         => "Tài khoản của bạn đang bị tạm khóa. Vui lòng liên hệ bộ phận hỗ trợ.",

            // Tài khoản đã ACTIVE → hoàn tất
            ("ACTIVE", _, _)            => "Tài khoản đã được xác minh. Bạn có thể bắt đầu giao dịch.",

            // Chưa nộp KYC lần nào
            (_, "PENDING", null)        => "Vui lòng upload ảnh CCCD để xác minh danh tính.",

            // Đã nộp, đang chờ Admin duyệt
            (_, "PENDING", "PENDING")   => "Hồ sơ CCCD đang được xem xét. Vui lòng chờ 1–2 ngày làm việc.",

            // Bị từ chối → nộp lại
            (_, "REJECTED", _)          => "Hồ sơ CCCD bị từ chối. Vui lòng upload lại ảnh CCCD rõ nét hơn.",

            // Các trường hợp khác
            _                           => "Vui lòng liên hệ bộ phận hỗ trợ để được trợ giúp."
        };
}
