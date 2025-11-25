using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartMeterServer.Models;
using System.Linq;

namespace SmartMeterServer.Tests
{
    [TestClass]
    public class MeterTests
    {
        [TestMethod]
        public void Constructor_SetsConnectionId()
        {
            // Arrange & Act
            var meter = new Meter("test-connection-123");

            // Assert
            Assert.AreEqual("test-connection-123", meter.ID);
            Assert.AreEqual(0, meter.Readings.Count);
        }

        [TestMethod]
        public void AddReading_StoresRoundedValue()
        {
            // Arrange
            var meter = new Meter("test-id");
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Act
            double stored = meter.AddReading(1.23456789, timestamp);

            // Assert
            Assert.AreEqual(1.23, stored);
            Assert.AreEqual(1, meter.Readings.Count);
            Assert.IsTrue(meter.Readings.Values.Contains(1.23));
        }

        [TestMethod]
        public void AddReading_RoundsToTwoDecimalPlaces()
        {
            // Arrange
            var meter = new Meter("test-id");
            long ts1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long ts2 = ts1 + 1000;

            // Act
            double result1 = meter.AddReading(0.124, ts1); // rounds to 0.12
            double result2 = meter.AddReading(0.125, ts2); // rounds to 0.13

            // Assert
            Assert.AreEqual(0.12, result1);
            Assert.AreEqual(0.13, result2);
        }

        [TestMethod]
        public void AddReading_UsesProvidedTimestamp()
        {
            // Arrange
            var meter = new Meter("test-id");
            long customTimestamp = 1234567890000L;

            // Act
            meter.AddReading(1.0, customTimestamp);

            // Assert
            Assert.IsTrue(meter.Readings.ContainsKey(customTimestamp));
            Assert.AreEqual(1.0, meter.Readings[customTimestamp]);
        }

        [TestMethod]
        public void SumReadings_ReturnsZeroForEmptyMeter()
        {
            // Arrange
            var meter = new Meter("test-id");

            // Act
            double sum = meter.SumReadings();

            // Assert
            Assert.AreEqual(0.0, sum);
        }

        [TestMethod]
        public void SumReadings_SumsAllStoredReadings()
        {
            // Arrange
            var meter = new Meter("test-id");
            long baseTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            meter.AddReading(1.50, baseTimestamp);
            meter.AddReading(2.25, baseTimestamp + 1000);
            meter.AddReading(0.75, baseTimestamp + 2000);

            // Act
            double sum = meter.SumReadings();

            // Assert
            Assert.AreEqual(4.50, sum);
        }

        [TestMethod]
        public void ReadingCount_ReturnsCorrectCount()
        {
            // Arrange
            var meter = new Meter("test-id");
            long baseTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Act & Assert
            Assert.AreEqual(0, meter.ReadingCount);

            meter.AddReading(1.0, baseTimestamp);
            Assert.AreEqual(1, meter.ReadingCount);

            meter.AddReading(2.0, baseTimestamp + 1000);
            meter.AddReading(3.0, baseTimestamp + 2000);
            Assert.AreEqual(3, meter.ReadingCount);
        }

        [TestMethod]
        public void Snapshot_ReturnsReadOnlyDictionary()
        {
            // Arrange
            var meter = new Meter("test-id");
            long baseTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            meter.AddReading(1.5, baseTimestamp);
            meter.AddReading(2.5, baseTimestamp + 1000);

            // Act
            var snapshot = meter.Snapshot();

            // Assert
            Assert.AreEqual(2, snapshot.Count);
            Assert.IsTrue(snapshot.Values.Contains(1.5));
            Assert.IsTrue(snapshot.Values.Contains(2.5));
        }

        [DataTestMethod]
        [DataRow(0.0, 0.0)]
        [DataRow(1.234, 1.23)]
        [DataRow(9.999, 10.0)]
        [DataRow(0.005, 0.01)]
        public void AddReading_VariousInputs_RoundsCorrectly(double input, double expected)
        {
            // Arrange
            var meter = new Meter("test-id");
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Act
            double result = meter.AddReading(input, timestamp);

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}