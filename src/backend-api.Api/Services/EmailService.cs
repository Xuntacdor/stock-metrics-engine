using System.Net;
using System.Net.Mail;

namespace backend_api.Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendMarginCallAsync(string toEmail, string userId, decimal rtt)
    {
        var subject = "[QuantIQ] Cảnh báo ký quỹ – Call Margin";
        var body = $@"Kính gửi khách hàng,

Tỷ lệ tài sản đảm bảo tài khoản của bạn hiện tại là {rtt:P2}, đã xuống dưới ngưỡng cảnh báo 85%.

Vui lòng nộp thêm tiền ký quỹ hoặc cắt giảm vị thế để tránh bị giải chấp tự động.

Trân trọng,
Đội ngũ QuantIQ";
        await SendAsync(toEmail, subject, body);
    }

    public async Task SendForceSellAsync(string toEmail, string userId, decimal rtt)
    {
        var subject = "[QuantIQ] Thông báo giải chấp tự động – Force Sell";
        var body = $@"Kính gửi khách hàng,

Tỷ lệ tài sản đảm bảo tài khoản của bạn là {rtt:P2}, đã xuống dưới ngưỡng giải chấp 80%.

Hệ thống đã tự động bán một phần hoặc toàn bộ vị thế để bảo vệ tài khoản của bạn.

Trân trọng,
Đội ngũ QuantIQ";
        await SendAsync(toEmail, subject, body);
    }

    private async Task SendAsync(string toEmail, string subject, string body)
    {
        var smtp = _config.GetSection("Smtp");
        var host = smtp["Host"] ?? "localhost";
        var port = int.Parse(smtp["Port"] ?? "25");
        var user = smtp["User"] ?? "";
        var pass = smtp["Password"] ?? "";
        var from = smtp["From"] ?? "noreply@quantiq.vn";
        var enableSsl = bool.Parse(smtp["EnableSsl"] ?? "false");

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = string.IsNullOrEmpty(user)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(user, pass),
                EnableSsl = enableSsl
            };
            var msg = new MailMessage(from, toEmail, subject, body) { IsBodyHtml = false };
            await client.SendMailAsync(msg);
            _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}: {Subject}", toEmail, subject);
        }
    }
}
