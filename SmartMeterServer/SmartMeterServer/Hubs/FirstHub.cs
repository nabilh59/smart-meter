using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using SmartMeterServer.Models; 

namespace SmartMeterServer.Hubs
{
    public class FirstHub : Hub
    {
        public static double InitialBill { get; set; } = 50.00;

        // create dictionary to store readings from each client
        private static ConcurrentDictionary<string, List<double>> readings = new();

        // runs as soon as a connection is detected
        public override async Task OnConnectedAsync()
        {
            // use connection ID to uniquely identify client, and initialise client's reading list
            string clientID = Context.ConnectionId;
            readings.TryAdd(clientID, new List<double>());

            var status = GridState.Current;

            var msg = status == "DOWN"
                ? new GridStatusMessage(
                    "grid.status", "1.0", "DOWN", "PAUSE_READINGS",
                    "Temporary grid interruption",
                    "We can’t receive readings right now due to a grid issue. No action is needed.",
                    DateTime.UtcNow
                  )
                : new GridStatusMessage(
                    "grid.status", "1.0", "UP", "RESUME_READINGS",
                    "Grid back to normal",
                    "Readings will resume automatically.",
                    DateTime.UtcNow
                  );

            await Clients.Caller.SendAsync("gridStatus", msg);

            await Clients.Caller.SendAsync("receiveInitialBill", InitialBill);

            await base.OnConnectedAsync();
        }

        public async Task CalculateNewBill(double currentTotalBill, double newReading)
        {
            // validate the reading is a positive decimal
            if (double.IsNaN(newReading) || double.IsInfinity(newReading) || newReading < 0)
            {
                await Clients.Caller.SendAsync("error", "Server-side validation failed: Invalid reading- Must be a positive decimal.");
                return;
            }

            double newBill = currentTotalBill + newReading;
            await Clients.Caller.SendAsync("calculateBill", newBill);
        }
    }
}
