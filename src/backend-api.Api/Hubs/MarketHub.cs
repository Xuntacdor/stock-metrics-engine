using backend_api.Api.Metrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace backend_api.Api.Hubs;

/// <summary>
/// Real-time hub for market price updates and user-specific alert notifications.
///
/// Authenticated clients are automatically addressable by UserId via
/// IHubContext&lt;MarketHub&gt;.Clients.User(userId).SendAsync(...)
/// because ASP.NET Core SignalR maps ClaimTypes.NameIdentifier → userId.
///
/// Events pushed to clients:
///   PriceUpdated      { symbol, currentPrice, totalVolume }   — broadcast to all
///   AlertTriggered    AlertTriggeredNotification               — sent to owning user only
/// </summary>
[Authorize]
public class MarketHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        AppMetrics.SignalRConnections.Inc();
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        AppMetrics.SignalRConnections.Dec();
        await base.OnDisconnectedAsync(exception);
    }
}
