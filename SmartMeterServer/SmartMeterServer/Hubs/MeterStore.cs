using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SmartMeter.Hubs
{
    public interface IMeterStore
    {
        // stores a mock representation of a meter, indexed by connection ID
        ConcurrentDictionary<string, Meter> Meters { get; set; }

        double PricePerKwh { get; }

        string initialBill { get; }

        void RemoveMeter(string connectionId);
        Meter GetOrCreateMeter(string connectionId);
        IReadOnlyCollection<KeyValuePair<string, Meter>> GetAll();
    }

    public class MeterStore : IMeterStore
    {        
        public ConcurrentDictionary<string, Meter> Meters { get; set; } = new();

        public double PricePerKwh { get; } = 0.15;

        public string initialBill { get; } = "0.00";

        private static readonly object _lock = new();

        private static MeterStore _instance;

        public static MeterStore getInstance()
        {
            // thread safe implementation of singleton,
            // using lock so that checks for existence of MeterStore only happen one after the other
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new MeterStore();
                    }
                }
            }
            return _instance;
        }

        public void RemoveMeter(string connectionId)
        {
            // not using the value returned by Remove so using _
            Meters.Remove(connectionId, out _);        
        }

        public Meter GetOrCreateMeter(string connectionId)
        {
            Meter meter = Meters.GetOrAdd(connectionId, id => new Meter(connectionId));
            return meter;
        }           

        public IReadOnlyCollection<KeyValuePair<string, Meter>> GetAll()
        {
            return Meters.ToList().AsReadOnly();
        }
            
    }
}