using Microsoft.AspNetCore.SignalR;

namespace backend_api.Api.Hubs;

public class MarketHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
