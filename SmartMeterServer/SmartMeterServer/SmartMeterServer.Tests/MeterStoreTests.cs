using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartMeterServer.Models;
using System;
using System.Linq;

namespace SmartMeterServer.Tests
{
    [TestClass]
    public class MeterStoreTests
    {
        [TestMethod]
        public void GetInstance_ReturnsSameInstance()
        {
            // Act
            var instance1 = MeterStore.getInstance();
            var instance2 = MeterStore.getInstance();

            // Assert
            Assert.AreSame(instance1, instance2);
        }

        [TestMethod]
        public void PricePerKwh_HasExpectedValue()
        {
            // Arrange
            var store = MeterStore.getInstance();

            // Act & Assert
            Assert.AreEqual(0.15, store.PricePerKwh);
        }

        [TestMethod]
        public void InitialBill_HasExpectedValue()
        {
            // Arrange
            var store = MeterStore.getInstance();

            // Act & Assert
            Assert.AreEqual("0.00", store.initialBill);
        }

        [TestMethod]
        public void GetOrCreateMeter_CreatesNewMeterIfNotExists()
        {
            // Arrange
            var store = MeterStore.getInstance();
            string connectionId = $"test-{Guid.NewGuid()}";

            // Act
            var meter = store.GetOrCreateMeter(connectionId);

            // Assert
            Assert.IsNotNull(meter);
            Assert.AreEqual(connectionId, meter.ID);
            Assert.IsTrue(store.Meters.ContainsKey(connectionId));
        }

        [TestMethod]
        public void GetOrCreateMeter_ReturnsSameMeterForSameConnectionId()
        {
            // Arrange
            var store = MeterStore.getInstance();
            string connectionId = $"test-{Guid.NewGuid()}";

            // Act
            var meter1 = store.GetOrCreateMeter(connectionId);
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            meter1.AddReading(1.5, timestamp);
            var meter2 = store.GetOrCreateMeter(connectionId);

            // Assert
            Assert.AreSame(meter1, meter2);
            Assert.AreEqual(1, meter2.ReadingCount);
        }

        [TestMethod]
        public void RemoveMeter_RemovesMeterFromStore()
        {
            // Arrange
            var store = MeterStore.getInstance();
            string connectionId = $"test-{Guid.NewGuid()}";
            store.GetOrCreateMeter(connectionId);

            // Act
            store.RemoveMeter(connectionId);

            // Assert
            Assert.IsFalse(store.Meters.ContainsKey(connectionId));
        }

        [TestMethod]
        public void RemoveMeter_DoesNotThrowIfMeterDoesNotExist()
        {
            // Arrange
            var store = MeterStore.getInstance();
            string nonExistentId = $"non-existent-{Guid.NewGuid()}";

            // Act & Assert - no exception should be thrown
            store.RemoveMeter(nonExistentId);
        }

        [TestMethod]
        public void GetAll_ReturnsAllMeters()
        {
            // Arrange
            var store = MeterStore.getInstance();
            var id1 = $"test-1-{Guid.NewGuid()}";
            var id2 = $"test-2-{Guid.NewGuid()}";
            store.GetOrCreateMeter(id1);
            store.GetOrCreateMeter(id2);

            // Act
            var all = store.GetAll();

            // Assert
            Assert.IsGreaterThanOrEqualTo(2, all.Count); // may contain meters from other tests
            Assert.IsTrue(all.Any(kvp => kvp.Key == id1));
            Assert.IsTrue(all.Any(kvp => kvp.Key == id2));
        }

        [TestMethod]
        public void GetAll_ReturnsReadOnlyCollection()
        {
            // Arrange
            var store = MeterStore.getInstance();

            // Act
            var all = store.GetAll();

            // Assert
            Assert.IsInstanceOfType(all, typeof(System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, Meter>>));
        }
    }
}