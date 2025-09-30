using Microsoft.AspNetCore.SignalR;

namespace SmartMeter.Hubs
{
    public class FirstHub : Hub
    {
        public static int TotalViews { get; set; } = 0;

        public async Task NewWindowLoaded()
        {
            TotalViews++;
            await Clients.All.SendAsync("updateTotalViews", TotalViews);
        }
    }
}
