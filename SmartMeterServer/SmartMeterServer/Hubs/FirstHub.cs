using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace SmartMeter.Hubs
{
    public class FirstHub : Hub
    {
        // server-side initial bill (authoritative)
        public static double InitialBill { get; set; } = 50.00;

        // server-side dictionary store (connectionId -> queue of readings)
        public static ConcurrentDictionary<string, ConcurrentQueue<double>> Readings { get; } = new();

        public FirstHub()
        {
        }

        // runs as soon as a connection is detected
        public override async Task OnConnectedAsync()
        {
            string clientID = Context.ConnectionId;
            Readings.TryAdd(clientID, new ConcurrentQueue<double>());

            await Clients.Caller.SendAsync("receiveInitialBill", InitialBill);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            string clientID = Context.ConnectionId;
            Readings.TryRemove(clientID, out _);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task CalculateNewBill(double currentTotalBill, double newReading)
        {
            if (double.IsNaN(newReading) || double.IsInfinity(newReading) || newReading < 0)
            {
                await Clients.Caller.SendAsync("error", "Invalid reading- Must be a positive decimal.");
                return;
            }

            string clientID = Context.ConnectionId;
            var q = Readings.GetOrAdd(clientID, _ => new ConcurrentQueue<double>());
            q.Enqueue(newReading);

            var readings = q.ToArray();
            var sumReadings = readings.Sum();
            var totalBill = InitialBill + sumReadings;

            await Clients.Caller.SendAsync("calculateBill", totalBill);
        }
    }
}
