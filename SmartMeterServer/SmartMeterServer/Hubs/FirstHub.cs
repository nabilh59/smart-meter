using SmartMeterServer.Logging;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using SmartMeterServer.Models; 
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SmartMeterServer.Hubs
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

            var meter = _store.GetOrCreateMeter(clientID);

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

            try
            {
                await Clients.Caller.SendAsync("gridStatus", msg);
            }
            catch
            {
                ServerErrorLog.Write(clientID, "SEND_FAILURE");
            }

            try
            {
                string errorBillTimestamp = DateTime.Now.ToString("H:mm ddd, dd MMM yyyy");
                await Clients.Caller.SendAsync("receiveInitialBill", _store.initialBill, errorBillTimestamp);
            }
            catch
            {
                ServerErrorLog.Write(clientID, "SEND_FAILURE");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            string clientID = Context.ConnectionId;
            ServerErrorLog.Write(clientID , "CLIENT_DISCONNECTED");
            _store.RemoveMeter(clientID);
            await base.OnDisconnectedAsync(exception);
        }

        private List<string> formatBillForClientUse(double bill)
        {
            // format before sending back to client with timestamp
            string formattedNewTotal = bill.ToString("0.00");
            string billTimestamp = DateTime.Now.ToString("H:mm ddd, dd MMM yyyy");

            return new List<string> { formattedNewTotal, billTimestamp };
        }

        private void storeReadings(string currentTotalBill, double newReading, long readingTimestamp)
        {
                string clientID = Context.ConnectionId;
                var meter = _store.GetOrCreateMeter(clientID);

                // store the reading (for history/debug)
                meter.AddReading(newReading, readingTimestamp);  
        }

        public async Task CalculateNewBill(string currentTotalBill, double newReading, long readingTimestamp)
        {
            string clientID = Context.ConnectionId;
            // validate new reading
            if (double.IsNaN(newReading) || double.IsInfinity(newReading) || newReading < 0)
            {
                ServerErrorLog.Write(clientID, "INVALID_MESSAGE");
                try
                {
                    await Clients.Caller.SendAsync("error", "Invalid reading - must be a positive decimal.");
                }
                catch
                {
                    ServerErrorLog.Write(clientID, "SEND_FAILURE");
                }
                return;
            }

            try
            {
                storeReadings(currentTotalBill, newReading, readingTimestamp);
            }
            
            catch
            {
                ServerErrorLog.Write(clientID, "PROCESSING_ERROR");
                return;
            }

            // compute cost of this single reading
            double cost = newReading * _store.PricePerKwh;

            // add cost to client-supplied total
            double newTotal = Convert.ToDouble(currentTotalBill) + cost;
            newTotal = System.Math.Round(newTotal, 2);

            // format before sending back to client with timestamp
            List<string> formattedValues;
            formattedValues = formatBillForClientUse(newTotal);
            string total = formattedValues[0];
            string timestamp = formattedValues[1];

            try
            {                
                await Clients.Caller.SendAsync("calculateBill", total, timestamp);
            }
            catch
            {
                ServerErrorLog.Write(clientID, "SEND_FAILURE");
            }
        }
    }
}
