using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SmartMeterServer.Models
{
    public class Meter
    {
        public string ID = "";

        // readings indexed by Unix milliseconds timestamp 
        // timestamp -> reading value
        public ConcurrentDictionary<long, double> Readings { get; } = new();

        public Meter(string ID) 
        { 
            this.ID = ID;
        }

        // Add a reading and return the timestamp used (Unix ms)
        public void AddReading(double reading, long timestamp)
        {
            Readings.TryAdd(timestamp, reading);
        }

        public double SumReadings() =>
            Readings.Values.Sum();

        public int ReadingCount => Readings.Count;

        public IReadOnlyDictionary<long, double> Snapshot() =>
            Readings.ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}