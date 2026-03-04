using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using backend_api.Api.Data;
using PayOS;
using PayOS.Models.Webhooks;
using PayOS.Models.V2.PaymentRequests;

namespace backend_api.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly IDepositRepository _depositRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly QuantIQContext _context;
    private readonly PayOSClient _payOS;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IDepositRepository depositRepo,
        IWalletRepository walletRepo,
        ITransactionRepository transactionRepo,
        QuantIQContext context,
        PayOSClient payOS,
        ILogger<PaymentService> logger)
    {
        _depositRepo     = depositRepo;
        _walletRepo      = walletRepo;
        _transactionRepo = transactionRepo;
        _context         = context;
        _payOS           = payOS;
        _logger          = logger;
    }

    // ─── Tạo link nạp tiền ────────────────────────────────────────────────────

    public async Task<CreateDepositResponse> CreateDepositLinkAsync(string userId, CreateDepositRequest request)
    {
        if (request.Amount < 1000)
            throw new ArgumentException("Số tiền nạp tối thiểu là 1.000 VNĐ.");

        if (string.IsNullOrWhiteSpace(request.ReturnUrl) || string.IsNullOrWhiteSpace(request.CancelUrl))
            throw new ArgumentException("Phải cung cấp ReturnUrl và CancelUrl.");

        // OrderCode: số nguyên dương, unique, max 9,999,999,999,999
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 9_999_999_999_999L;

        // Tạo phiếu nạp trong DB với trạng thái PENDING trước khi gọi PayOS
        var deposit = new DepositRequest
        {
            UserId    = userId,
            OrderCode = orderCode,
            Amount    = request.Amount,
            Status    = "PENDING",
            CreatedAt = DateTime.UtcNow
        };
        await _depositRepo.AddAsync(deposit);
        await _depositRepo.SaveChangesAsync();

        // Tạo link thanh toán qua PayOS SDK v2
        var paymentLinkRequest = new CreatePaymentLinkRequest
        {
            OrderCode   = orderCode,
            Amount      = (int)request.Amount,    // PayOS nhận int (VNĐ)
            Description = "Nap tien QuantIQ",      // max 25 ký tự, không dấu
            ReturnUrl   = request.ReturnUrl,
            CancelUrl   = request.CancelUrl
        };

        var paymentLink = await _payOS.PaymentRequests.CreateAsync(paymentLinkRequest);

        // Lưu checkout URL vào phiếu nạp
        deposit.CheckoutUrl = paymentLink.CheckoutUrl;
        await _depositRepo.UpdateAsync(deposit);
        await _depositRepo.SaveChangesAsync();

        _logger.LogInformation(
            "DepositRequest created: UserId={UserId}, OrderCode={OrderCode}, Amount={Amount}",
            userId, orderCode, request.Amount);

        return new CreateDepositResponse
        {
            DepositId   = deposit.DepositId,
            OrderCode   = orderCode,
            Amount      = request.Amount,
            CheckoutUrl = paymentLink.CheckoutUrl,
            Status      = "PENDING",
            CreatedAt   = deposit.CreatedAt
        };
    }

    // ─── Xử lý Webhook từ PayOS ────────────────────────────────────────────────

    public async Task HandleWebhookAsync(string rawBody, string payosSignature)
    {
        // Deserialise toàn bộ webhook payload (kiểu Webhook của SDK)
        Webhook webhookPayload;
        try
        {
            webhookPayload = System.Text.Json.JsonSerializer.Deserialize<Webhook>(rawBody,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Webhook body rỗng.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to deserialise PayOS webhook: {Msg}", ex.Message);
            throw new UnauthorizedAccessException("Webhook payload không hợp lệ.");
        }

        // Xác minh chữ ký → nhận lại WebhookData đã được verify
        WebhookData webhookData;
        try
        {
            webhookData = await _payOS.Webhooks.VerifyAsync(webhookPayload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("PayOS webhook signature invalid: {Msg}", ex.Message);
            throw new UnauthorizedAccessException("Webhook signature không hợp lệ.");
        }

        _logger.LogInformation(
            "PayOS webhook received: OrderCode={Code}, PayloadCode={PCode}",
            webhookData.OrderCode, webhookPayload.Code);

        // Chỉ xử lý khi thanh toán thành công (code == "00")
        if (webhookPayload.Code != "00")
        {
            _logger.LogInformation("Webhook ignored (non-success code={Code})", webhookPayload.Code);
            return;
        }

        // ─── Idempotency: Tra cứu OrderCode, không xử lý lần 2 ──────────────
        var deposit = await _depositRepo.GetByOrderCodeAsync(webhookData.OrderCode);
        if (deposit == null)
        {
            _logger.LogWarning("DepositRequest not found for OrderCode={Code}", webhookData.OrderCode);
            return;
        }

        if (deposit.Status == "PAID")
        {
            _logger.LogWarning(
                "Duplicate webhook for OrderCode={Code}, already PAID. Ignored.",
                webhookData.OrderCode);
            return;  // Không cộng tiền lần 2
        }

        // ─── Cộng tiền vào ví — DB Transaction nguyên tử ─────────────────────
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var wallet = await _walletRepo.GetByUserIdAsync(deposit.UserId)
                ?? throw new InvalidOperationException($"Wallet not found for user {deposit.UserId}");

            var balanceBefore  = wallet.Balance ?? 0;
            wallet.Balance     = balanceBefore + deposit.Amount;
            wallet.LastUpdated = DateTime.UtcNow;
            _walletRepo.Update(wallet);

            // Đánh dấu phiếu nạp là PAID
            deposit.Status = "PAID";
            deposit.PaidAt = DateTime.UtcNow;
            await _depositRepo.UpdateAsync(deposit);

            // Ghi lịch sử giao dịch (Audit Trail)
            var transaction = new Transaction
            {
                UserId        = deposit.UserId,
                RefId         = deposit.OrderCode.ToString(),
                TransType     = "DEPOSIT",
                Amount        = deposit.Amount,
                BalanceBefore = balanceBefore,
                BalanceAfter  = wallet.Balance ?? 0,
                Description   = $"Nạp tiền qua PayOS - Mã GD: {deposit.OrderCode}",
                TransTime     = DateTime.UtcNow
            };
            await _transactionRepo.AddAsync(transaction);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogInformation(
                "Payment processed: UserId={UserId}, OrderCode={Code}, Amount={Amount}, NewBalance={Balance}",
                deposit.UserId, deposit.OrderCode, deposit.Amount, wallet.Balance);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Failed to process payment for OrderCode={Code}", webhookData.OrderCode);
            throw;
        }
    }

    // ─── Huỷ lệnh nạp ─────────────────────────────────────────────────────────

    public async Task CancelDepositAsync(long orderCode)
    {
        var deposit = await _depositRepo.GetByOrderCodeAsync(orderCode);
        if (deposit == null) return;

        if (deposit.Status == "PENDING")
        {
            deposit.Status = "CANCELLED";
            await _depositRepo.UpdateAsync(deposit);
            await _depositRepo.SaveChangesAsync();
            _logger.LogInformation("DepositRequest cancelled: OrderCode={Code}", orderCode);
        }
    }

    // ─── Lịch sử nạp tiền ─────────────────────────────────────────────────────

    public async Task<IEnumerable<DepositDetailResponse>> GetDepositHistoryAsync(string userId)
    {
        var deposits = await _depositRepo.GetByUserIdAsync(userId);
        return deposits.Select(d => new DepositDetailResponse
        {
            DepositId   = d.DepositId,
            UserId      = d.UserId,
            OrderCode   = d.OrderCode,
            Amount      = d.Amount,
            Status      = d.Status,
            CheckoutUrl = d.CheckoutUrl,
            CreatedAt   = d.CreatedAt,
            PaidAt      = d.PaidAt
        });
    }
}
