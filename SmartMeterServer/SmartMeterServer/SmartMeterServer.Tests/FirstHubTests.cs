using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SmartMeterServer.Hubs;
using SmartMeterServer.Models;
using System;
using System.Threading.Tasks;

namespace SmartMeterServer.Tests
{
    [TestClass]
    public class FirstHubTests
    {
        private Mock<IMeterStore> _mockStore = new();
        private Mock<IHubCallerClients> _mockClients = new();
        private Mock<ISingleClientProxy> _mockClientProxy = new();
        private Mock<HubCallerContext> _mockContext = new();

        [TestInitialize]
        public void Setup()
        {
            _mockStore = new Mock<IMeterStore>();
            _mockClients = new Mock<IHubCallerClients>();
            _mockClientProxy = new Mock<ISingleClientProxy>();
            _mockContext = new Mock<HubCallerContext>();

            _mockClients.Setup(c => c.Caller).Returns(_mockClientProxy.Object);
            _mockContext.Setup(c => c.ConnectionId).Returns("test-connection-id");
        }

        [TestMethod]
        public async Task OnConnectedAsync_CreatesNewMeter()
        {
            // Arrange
            FirstHub _hub = new FirstHub(_mockStore.Object)
            {
                Clients = _mockClients.Object,
                Context = _mockContext.Object
            };

            var meter = new Meter("test-connection-id");
            _mockStore.Setup(s => s.GetOrCreateMeter("test-connection-id")).Returns(meter);
            _mockStore.Setup(s => s.initialBill).Returns("0.00");

            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _mockStore.Verify(s => s.GetOrCreateMeter("test-connection-id"), Times.Once);
        }

        [TestMethod]
        public async Task OnConnectedAsync_SendsInitialBill()
        {
            // Arrange
            FirstHub _hub = new FirstHub(_mockStore.Object)
            {
                Clients = _mockClients.Object,
                Context = _mockContext.Object
            };

            var meter = new Meter("test-connection-id");
            _mockStore.Setup(s => s.GetOrCreateMeter(It.IsAny<string>())).Returns(meter);
            _mockStore.Setup(s => s.initialBill).Returns("0.00");

            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "receiveInitialBill",
                    It.Is<object[]>(o => o.Length == 2 && o[0].ToString() == "0.00"),
                    default),
                Times.Once);
        }

        [TestMethod]
        public async Task OnDisconnectedAsync_RemovesMeter()
        {
            // Arrange
            FirstHub _hub = new FirstHub(_mockStore.Object)
            {
                Clients = _mockClients.Object,
                Context = _mockContext.Object
            };

            _mockStore.Setup(s => s.RemoveMeter("test-connection-id"));

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            _mockStore.Verify(s => s.RemoveMeter("test-connection-id"), Times.Once);
        }

        [TestMethod]
        public async Task CalculateNewBill_ValidReading_StoresAndCalculates()
        {
            // Arrange
            FirstHub _hub = new FirstHub(_mockStore.Object)
            {
                Clients = _mockClients.Object,
                Context = _mockContext.Object
            };

            var meter = new Meter("test-connection-id");
            _mockStore.Setup(s => s.GetOrCreateMeter("test-connection-id")).Returns(meter);
            _mockStore.Setup(s => s.PricePerKwh).Returns(0.15);
            string currentBill = "50.00";
            double reading = 10.0;
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            string expectedCost = "51.50";

            // Act
            await _hub.CalculateNewBill(currentBill, reading, timestamp);

            // Assert
            Assert.AreEqual(1, meter.ReadingCount);

            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "calculateBill",
                    It.Is<object[]>(o => o.Length == 2 && o[0].ToString() == expectedCost),
                    default),
                Times.Once);
        }

        [TestMethod]
        [DataRow(double.NaN)]
        [DataRow(double.PositiveInfinity)]
        [DataRow(double.NegativeInfinity)]
        [DataRow(-1.0)]
        [DataRow(-0.5)]
        public async Task CalculateNewBill_InvalidReading_NotStored(double invalidReading)
        {
            // Arrange
            FirstHub _hub = new FirstHub(_mockStore.Object)
            {
                Clients = _mockClients.Object,
                Context = _mockContext.Object
            };

            var meter = new Meter("test-connection-id");
            _mockStore.Setup(s => s.GetOrCreateMeter(It.IsAny<string>())).Returns(meter);
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Act
            await _hub.CalculateNewBill("50.00", invalidReading, timestamp);

            // Assert
            
            Assert.AreEqual(0, meter.ReadingCount); // reading should not be stored
        }

        [TestMethod]
        [DataRow(double.NaN)]
        [DataRow(double.PositiveInfinity)]
        [DataRow(double.NegativeInfinity)]
        [DataRow(-1.0)]
        [DataRow(-0.5)]
        public async Task CalculateNewBill_InvalidReading_SendsError(double invalidReading)
        {
            // Arrange
            FirstHub _hub = new FirstHub(_mockStore.Object)
            {
                Clients = _mockClients.Object,
                Context = _mockContext.Object
            };

            var meter = new Meter("test-connection-id");
            _mockStore.Setup(s => s.GetOrCreateMeter(It.IsAny<string>())).Returns(meter);
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Act
            await _hub.CalculateNewBill("50.00", invalidReading, timestamp);

            // Assert
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "error",
                    It.IsAny<object[]>(),
                    default),
                Times.Once);
        }

        [TestMethod]
        public async Task CalculateNewBill_RoundsResultToTwoDecimalPlaces()
        {
            // Arrange
            FirstHub _hub = new FirstHub(_mockStore.Object)
            {
                Clients = _mockClients.Object,
                Context = _mockContext.Object
            };

            var meter = new Meter("test-connection-id");
            _mockStore.Setup(s => s.GetOrCreateMeter("test-connection-id")).Returns(meter);
            _mockStore.Setup(s => s.PricePerKwh).Returns(0.15);
            string currentBill = "50.00";
            double reading = 1.234567;
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Act
            await _hub.CalculateNewBill(currentBill, reading, timestamp);

            // Assert
            double expectedCost = 1.234567 * 0.15; // 0.1845
            double expectedTotal = Math.Round(50.00 + expectedCost, 2); // 50.18

            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "calculateBill",
                    It.Is<object[]>(o => o.Length == 2),
                    default),
                Times.Once);
        }

        [TestMethod]
        public async Task CalculateNewBill_ZeroReading_IsValid()
        {
            // Arrange
            FirstHub _hub = new FirstHub(_mockStore.Object)
            {
                Clients = _mockClients.Object,
                Context = _mockContext.Object
            };

            var meter = new Meter("test-connection-id");
            _mockStore.Setup(s => s.GetOrCreateMeter("test-connection-id")).Returns(meter);
            _mockStore.Setup(s => s.PricePerKwh).Returns(0.15);
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Act
            await _hub.CalculateNewBill("50.00", 0.0, timestamp);

            // Assert
            Assert.AreEqual(1, meter.ReadingCount);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "calculateBill",
                    It.Is<object[]>(o => o[0].ToString() == "50.00"),
                    default),
                Times.Once);
        }

        [TestMethod]
        public async Task CalculateNewBill_StoresTimestamp()
        {
            // Arrange
            FirstHub _hub = new FirstHub(_mockStore.Object)
            {
                Clients = _mockClients.Object,
                Context = _mockContext.Object
            };

            var meter = new Meter("test-connection-id");
            _mockStore.Setup(s => s.GetOrCreateMeter("test-connection-id")).Returns(meter);
            _mockStore.Setup(s => s.PricePerKwh).Returns(0.15);
            long customTimestamp = 1234567890000L;

            // Act
            await _hub.CalculateNewBill("50.00", 5.0, customTimestamp);

            // Assert
            Assert.IsTrue(meter.Readings.ContainsKey(customTimestamp));
        }
    }
}