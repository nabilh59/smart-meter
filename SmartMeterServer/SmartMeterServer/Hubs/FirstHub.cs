using Microsoft.AspNetCore.SignalR;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SmartMeter.Hubs
{
    public class FirstHub : Hub
    {
        private readonly IMeterStore _store;

        public FirstHub(IMeterStore store)
        {
            _store = store;
        }

        // runs as soon as a connection is detected
        public override async Task OnConnectedAsync()
        {
            string clientID = Context.ConnectionId;

            // initialise Meter and register in singleton store
            var meter = new Meter { ID = clientID };
            _store.AddMeter(meter);

            var culture = CultureInfo.GetCultureInfo("en-GB");
            double initialNumeric = _store.InitialBill;
            string initialFormatted = initialNumeric.ToString("C2", culture);

            // send numeric + formatted initial bill
            await Clients.Caller.SendAsync("receiveInitialBill", initialNumeric, initialFormatted);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            string clientID = Context.ConnectionId;
            _store.RemoveMeter(clientID, out _);
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

            // store reading with server-generated timestamp (rounded inside Meter.AddReading)
            var meter = _store.GetOrCreateMeter(clientID);
            long timestamp = meter.AddReading(newReading);

            var sum = meter.SumReadings();
            var totalBill = _store.InitialBill + sum;

            var culture = CultureInfo.GetCultureInfo("en-GB");
            string formattedTotal = totalBill.ToString("C2", culture);     // "£50.00"
            string formattedReading = Math.Round(newReading, 2).ToString("F2", culture); // "xx.yy"

            // send numeric total, formatted total, numeric reading, formatted reading, and the server timestamp
            await Clients.Caller.SendAsync("calculateBill", totalBill, formattedTotal, Math.Round(newReading, 2), formattedReading, timestamp);
        }
    }
}
