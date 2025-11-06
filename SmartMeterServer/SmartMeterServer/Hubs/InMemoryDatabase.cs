using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SmartMeter.Hubs
{
    public interface IInMemoryDatabase
    {
        ConcurrentDictionary<string, ConcurrentQueue<double>> Readings { get; }
        bool TryAddClient(string clientId);
        bool RemoveClient(string clientId);
        void AddReading(string clientId, double reading);
        IEnumerable<double> GetReadings(string clientId);
        double InitialBill { get; set; }
    }

    public class InMemoryDatabase : IInMemoryDatabase
    {
        public ConcurrentDictionary<string, ConcurrentQueue<double>> Readings { get; } = new();

        // default initial bill
        public double InitialBill { get; set; } = 50.00;

        public bool TryAddClient(string clientId) =>
            Readings.TryAdd(clientId, new ConcurrentQueue<double>());

        public bool RemoveClient(string clientId) =>
            Readings.TryRemove(clientId, out _);

        public void AddReading(string clientId, double reading)
        {
            var q = Readings.GetOrAdd(clientId, _ => new ConcurrentQueue<double>());
            q.Enqueue(reading);
        }

        public IEnumerable<double> GetReadings(string clientId)
        {
            if (Readings.TryGetValue(clientId, out var q)) return q.ToArray();
            return Enumerable.Empty<double>();
        }
    }
}