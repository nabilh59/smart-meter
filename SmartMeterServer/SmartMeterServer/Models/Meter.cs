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
        public double AddReading(double reading, long timestamp)
        {            
            double rounded = Math.Round(reading, 2, MidpointRounding.AwayFromZero);
            Readings.TryAdd(timestamp, rounded);
            return rounded; // return the rounded reading, not the timestamp
        }

        public double SumReadings() =>
            Readings.Values.Sum();

        public int ReadingCount => Readings.Count;

        public IReadOnlyDictionary<long, double> Snapshot() =>
            Readings.ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}