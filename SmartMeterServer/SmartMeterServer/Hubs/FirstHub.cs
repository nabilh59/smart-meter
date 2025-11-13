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

        public override async Task OnConnectedAsync()
        {
            string clientID = Context.ConnectionId;

            // consistent creation (use returned meter if you need to set metadata)
            var meter = _store.GetOrCreateMeter(clientID);

            // optionally set metadata only when newly created
            if (meter.ReadingCount == 0)
            {
                // e.g. meter.SomeMeta = "...";
            }

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
            // validate new reading
            if (double.IsNaN(newReading) || double.IsInfinity(newReading) || newReading < 0)
            {
                await Clients.Caller.SendAsync("error", "Invalid reading - must be a positive decimal.");
                return;
            }

            // check if connection is still active and if not then don't proceed
            if (Context.ConnectionAborted.IsCancellationRequested)
            {
                return;
            }

            string clientID = Context.ConnectionId;

            // use store factory consistently
            var meter = _store.GetOrCreateMeter(clientID);

            // add reading; AddReading returns (timestamp, storedRoundedValue)
            var (timestamp, storedValue) = meter.AddReading(newReading);

            // calculation required: client-sent currentTotalBill + this single new reading
            double newTotal = currentTotalBill + storedValue;

            // send numeric total only (client formats with "£")
            await Clients.Caller.SendAsync("calculateBill", newTotal);
        }
    }
}
