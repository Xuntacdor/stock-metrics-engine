namespace backend_api.Api.Services;

public interface IEmailService
{
    Task SendMarginCallAsync(string toEmail, string userId, decimal rtt);
    Task SendForceSellAsync(string toEmail, string userId, decimal rtt);
}
