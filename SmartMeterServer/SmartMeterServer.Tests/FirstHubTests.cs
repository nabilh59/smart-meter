using Xunit;
using SmartMeterServer.Hubs;
using SmartMeterServer.Models;
using SmartMeterServer.Logging;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System.IO;
using System.Threading.Tasks;

namespace SmartMeterServer.Tests
{
    public class FirstHubTests
    {
        [Fact]
        public async Task Log_Client_Disconnected()
        {

            var storeMock = new Mock<IMeterStore>();
            var hub = new FirstHub(storeMock.Object);

            var contextMock = new Mock<HubCallerContext>();
            contextMock.Setup(c => c.ConnectionId).Returns("TEST123");

            // Inject mock Context
            typeof(Hub).GetProperty("Context")!
                .SetValue(hub, contextMock.Object);

            // disconnection happens here
            await hub.OnDisconnectedAsync(null);

            // check if the first line of the error log csv has the expected results
            var lines = File.ReadAllLines("server_logs.csv");
            Assert.Contains("CLIENT_DISCONNECTED", lines[^1]);
            Assert.Contains("TEST123", lines[^1]);

            // delete the file just in case it already exists
            if (File.Exists("server_logs.csv"))
            {
                File.Delete("server_logs.csv");
            }
        }

        [Fact]
        public async Task Log_Invalid_Message()
        {
            var storeMock = new Mock<IMeterStore>();
            var hub = new FirstHub(storeMock.Object);

            // Mock Context
            var contextMock = new Mock<HubCallerContext>();
            contextMock.Setup(c => c.ConnectionId).Returns("TEST123");
            typeof(Hub).GetProperty("Context")!
                .SetValue(hub, contextMock.Object);

            // Mock Clients and Caller
            var clients = new Mock<IHubCallerClients>();
            var caller = new Mock<ISingleClientProxy>();

            // Caller.SendAsync must return a Task
            caller.Setup(c =>
                c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);

            clients.Setup(c => c.Caller).Returns(caller.Object);

            typeof(Hub).GetProperty("Clients")!
                .SetValue(hub, clients.Object);

            // Act: invalid reading (-5)
            await hub.CalculateNewBill("10.00", -5, 123456);

            // Assert
            var lines = File.ReadAllLines("server_logs.csv");
            Assert.Contains("INVALID_MESSAGE", lines[^1]);
            Assert.Contains("TEST123", lines[^1]);

            // delete the file just in case it already exists
            if (File.Exists("server_logs.csv"))
            {
                File.Delete("server_logs.csv");
            }
        }
    }
}