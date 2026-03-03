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

    // ─── Upload & OCR ────────────────────────────────────────────────────────

    public async Task<KycUploadResponse> UploadAndOcrAsync(string userId, IFormFile image)
    {
        // 1. Validate file
        ValidateImageFile(image);

        // 2. Lưu file vào server
        var savedPath = await SaveImageAsync(image, userId);

        // 3. Gọi FPT.AI
        var ocrData = await CallFptAiOcrAsync(image);

        // 4. Map vào model và lưu DB
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

    // ─── Queries ─────────────────────────────────────────────────────────────

    public async Task<IEnumerable<KycDetailResponse>> GetByUserIdAsync(string userId)
    {
        var docs = await _kycRepo.GetByUserIdAsync(userId);
        return docs.Select(MapToDetailResponse);
    }

    public async Task<IEnumerable<KycDetailResponse>> GetPendingAsync()
    {
        var docs = await _kycRepo.GetPendingAsync();
        return docs.Select(MapToDetailResponse);
    }

    // ─── Admin Review ─────────────────────────────────────────────────────────

    public async Task<KycDetailResponse> ReviewAsync(int kycId, KycReviewRequest request)
    {
        var valid = new[] { "APPROVED", "REJECTED" };
        if (!valid.Contains(request.Decision, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Decision phải là APPROVED hoặc REJECTED.");

        if (request.Decision == "REJECTED" && string.IsNullOrWhiteSpace(request.RejectReason))
            throw new ArgumentException("Phải cung cấp lý do khi từ chối KYC.");

        var doc = await _kycRepo.GetByIdAsync(kycId)
            ?? throw new KeyNotFoundException($"Không tìm thấy KYC #{kycId}.");

        if (doc.Status != "PENDING")
            throw new InvalidOperationException($"KYC #{kycId} đã được xử lý (Status = {doc.Status}).");

        doc.Status      = request.Decision.ToUpper();
        doc.RejectReason = request.RejectReason;
        doc.ReviewedAt  = DateTime.UtcNow;

        await _kycRepo.UpdateAsync(doc);

        // Đồng bộ KycStatus vào bảng Users
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == doc.UserId);
        if (user != null)
        {
            user.KycStatus = doc.Status;
            _context.Users.Update(user);
        }

        await _kycRepo.SaveChangesAsync();

        _logger.LogInformation("KYC #{KycId} reviewed: {Decision} by admin.", kycId, doc.Status);

        return MapToDetailResponse(doc);
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

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
        // Lưu vào wwwroot/kyc/{userId}/
        var folder = Path.Combine(_env.WebRootPath ?? "wwwroot", "kyc", userId);
        Directory.CreateDirectory(folder);

        var extension = Path.GetExtension(image.FileName);
        var fileName  = $"{Guid.NewGuid()}{extension}";
        var filePath  = Path.Combine(folder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await image.CopyToAsync(stream);

        // Trả về đường dẫn tương đối để lưu vào DB
        return Path.Combine("kyc", userId, fileName).Replace("\\", "/");
    }

    /// <summary>
    /// Gọi FPT.AI OCR API với multipart/form-data.
    /// Trả về data object đầu tiên trong mảng data, hoặc null nếu thất bại.
    /// </summary>
    private async Task<FptAiOcrData?> CallFptAiOcrAsync(IFormFile image)
    {
        var apiKey = _config["FptAi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("FPT.AI API Key chưa được cấu hình (FptAi:ApiKey).");

        var client = _httpClientFactory.CreateClient("FptAi");
        client.DefaultRequestHeaders.Add("api-key", apiKey);

        // Đọc lại stream từ đầu (stream đã bị đọc khi validate)
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

    // ─── Mappers ──────────────────────────────────────────────────────────────

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
}
