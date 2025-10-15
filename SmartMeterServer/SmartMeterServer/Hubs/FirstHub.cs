using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace SmartMeter.Hubs
{
    public class FirstHub : Hub
    {
        public static double InitialBill { get; set; } = 0.00;

        // runs as soon as a connection is detected
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("receiveInitialBill", InitialBill);
            await base.OnConnectedAsync();
        }
        public async Task CalculateNewBill(double currentTotalBill, double newReading)
        {
            double newBill = currentTotalBill + newReading;
            await Clients.Caller.SendAsync("calculateBill", newBill);
        }
    }
}
