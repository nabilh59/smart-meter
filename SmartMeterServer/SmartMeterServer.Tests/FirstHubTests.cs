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
    internal class FirstHubTests
    {
        public async Task OnDisconnectedAsync_ShouldLogClientDisconnected()
        {
            // Arrange
            File.Delete("server_logs.csv");

            var storeMock = new Mock<IMeterStore>();
            var hub = new FirstHub(storeMock.Object);

            var contextMock = new Mock<HubCallerContext>();
            contextMock.Setup(c => c.ConnectionId).Returns("TEST123");

            // Inject mock Context
            typeof(Hub)
                .GetProperty("Context")
                .SetValue(hub, contextMock.Object);

            // Act
            await hub.OnDisconnectedAsync(null);

            // Assert
            var lines = File.ReadAllLines("server_logs.csv");
            Assert.Contains("CLIENT_DISCONNECTED", lines[1]);
            Assert.Contains("TEST123", lines[1]);
        }
}
