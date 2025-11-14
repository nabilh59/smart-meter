using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SmartMeter.Hubs
{
    public interface IMeterStore
    {
        ConcurrentDictionary<string, Meter> ByConnectionId { get; }
        double InitialBill { get; set; }

        bool AddMeter(Meter meter);
        bool RemoveMeter(string connectionId, out Meter? meter);
        Meter GetOrCreateMeter(string connectionId);
        IReadOnlyCollection<KeyValuePair<string, Meter>> GetAll();
    }

    public class MeterStore : IMeterStore
    {
        public ConcurrentDictionary<string, Meter> ByConnectionId { get; } = new();
        private readonly object _lock = new();

        public double InitialBill { get; set; } = 50.00;

        public bool AddMeter(Meter meter)
        {
            lock (_lock)
            {
                if (ByConnectionId.ContainsKey(meter.ID)) return false;
                ByConnectionId[meter.ID] = meter;
                return true;
            }
        }

        public bool RemoveMeter(string connectionId, out Meter? meter)
        {
            lock (_lock)
            {
                return ByConnectionId.TryRemove(connectionId, out meter);
            }
        }

        public Meter GetOrCreateMeter(string connectionId) =>
            ByConnectionId.GetOrAdd(connectionId, id => new Meter { ID = id });

        public IReadOnlyCollection<KeyValuePair<string, Meter>> GetAll() =>
            ByConnectionId.ToList().AsReadOnly();
    }
}