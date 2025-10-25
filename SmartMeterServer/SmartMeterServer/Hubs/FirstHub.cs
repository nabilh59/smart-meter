using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace SmartMeter.Hubs
{
    public class FirstHub : Hub
    {
        public static double InitialBill { get; set; } = 50.00;

        // create dictionary to store readings from each client
        private static ConcurrentDictionary<string, List<double>> readings = new();

        //runs as soon as a connection is detected
        public override async Task OnConnectedAsync()
        {
            // use connection ID to uniquely identify client, and initialise client's reading list
            string clientID = Context.ConnectionId;
            readings.TryAdd(clientID, new List<double>());

            await Clients.Caller.SendAsync("receiveInitialBill", InitialBill);
            await base.OnConnectedAsync();
        }
        public async Task CalculateNewBill(double currentTotalBill, double newReading)
        {
            // validate the reading
            if (newReading < 0 || newReading > 50)
            {
                await Clients.Caller.SendAsync("error", "Invalid reading: Must be between 0 and 50 kWh.");
                return;
            }
            
            double newBill = currentTotalBill + newReading;
            await Clients.Caller.SendAsync("calculateBill", newBill);
        }
    }
}
