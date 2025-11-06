using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SmartMeter.Hubs
{
    public class FirstHub : Hub
    {
        public static double InitialBill { get; set; } = 50.00;

        // create dictionary to store readings from each client
        private static ConcurrentDictionary<string, List<double>> readings = new();

        private readonly IInMemoryDatabase _db;

        public FirstHub(IInMemoryDatabase db)
        {
            _db = db;
        }

        //runs as soon as a connection is detected
        public override async Task OnConnectedAsync()
        {
            // use connection ID to uniquely identify client, and initialise client's reading queue
            string clientID = Context.ConnectionId;
            _db.TryAddClient(clientID);

            await Clients.Caller.SendAsync("receiveInitialBill", _db.InitialBill);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            // optional: cleanup when client disconnects
            string clientID = Context.ConnectionId;
            _db.RemoveClient(clientID);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task CalculateNewBill(double currentTotalBill, double newReading)
        {
            // validate the reading is a positive decimal
            if (double.IsNaN(newReading) || double.IsInfinity(newReading) || newReading < 0)
            {
                await Clients.Caller.SendAsync("error", "Invalid reading- Must be a positive decimal.");
                return;
            }

            // store reading in the in-memory "database"
            string clientID = Context.ConnectionId;
            _db.AddReading(clientID, newReading);

            double newBill = currentTotalBill + newReading;
            await Clients.Caller.SendAsync("calculateBill", newBill);
        }
    }
}
