using Microsoft.AspNetCore.SignalR;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SmartMeter.Hubs
{
    public class FirstHub : Hub
    {
        private readonly IMeterStore _store;

        // set your electricity price per kWh here
        private const double PricePerKwh = 0.15;

        public FirstHub(IMeterStore store)
        {
            _store = store;
        }

        public override async Task OnConnectedAsync()
        {
            string clientID = Context.ConnectionId;

            // consistent creation
            var meter = _store.GetOrCreateMeter(clientID);

            var culture = CultureInfo.GetCultureInfo("en-GB");
            double initialNumeric = _store.InitialBill;
            string initialFormatted = initialNumeric.ToString("C2", culture);

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
                await Clients.Caller.SendAsync("error", "Invalid reading - must be a positive decimal.");
                return;
            }

            string clientID = Context.ConnectionId;
            var meter = _store.GetOrCreateMeter(clientID);

            // store the reading (for history/debug)
            double storedValue = meter.AddReading(newReading);

            // compute cost of this single reading
            double cost = storedValue * PricePerKwh;

            // incremental: add cost to client-supplied total
            double newTotal = currentTotalBill + cost;
            newTotal = System.Math.Round(newTotal, 2);

            await Clients.Caller.SendAsync("calculateBill", newTotal);
        }
    }
}
