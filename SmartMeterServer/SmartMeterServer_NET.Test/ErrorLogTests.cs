using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SmartMeterServer.Hubs;
using SmartMeterServer.Logging;
using SmartMeterServer.Models;
using System.IO;
using System.Threading.Tasks;

namespace SmartMeterServer.Tests
{
    [TestClass]
    public class ServerErrorLogTests
    {
        private const string LogFile = "server_logs.csv";
        [TestMethod]
        public void LogEntryBasic()
        {
            // write 
            ServerErrorLog.Write("TEST123", "SEND_FAILURE");
            Assert.IsTrue(File.Exists(LogFile));
            var lines = File.ReadAllLines(LogFile);

            Assert.AreEqual("log_id,timestamp,connection_id,event", lines[0]);


            string lastLine = lines[^1];

            StringAssert.Contains(lastLine, "TEST123");
            StringAssert.Contains(lastLine, "SEND_FAILURE");
        }
        [TestMethod]
        public async Task LogClientDisconnected()
        {
            var storeMock = new Mock<IMeterStore>();
            var hub = new FirstHub(storeMock.Object);

            var contextMock = new Mock<HubCallerContext>();
            contextMock.Setup(c => c.ConnectionId).Returns("TEST123");

            typeof(Hub).GetProperty("Context")!
                .SetValue(hub, contextMock.Object);

            // Act
            await hub.OnDisconnectedAsync(null);

            // Assert
            Assert.IsTrue(File.Exists(LogFile));

            var lines = File.ReadAllLines(LogFile);
            var lastLine = lines[^1];    // last entry

            StringAssert.Contains(lastLine, "CLIENT_DISCONNECTED");
            StringAssert.Contains(lastLine, "TEST123");
        }
        [TestMethod]
        public async Task LogInvalidMessage()
        {

            var storeMock = new Mock<IMeterStore>();
            var hub = new FirstHub(storeMock.Object);


            var context = new Mock<HubCallerContext>();
            context.Setup(c => c.ConnectionId).Returns("TEST123");
            typeof(Hub).GetProperty("Context")!.SetValue(hub, context.Object);


            var clients = new Mock<IHubCallerClients>();
            var caller = new Mock<ISingleClientProxy>();


            caller.As<IClientProxy>()
                .Setup(c => c.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    default))
                .Returns(Task.CompletedTask);

            clients.Setup(c => c.Caller).Returns(caller.Object);

            typeof(Hub).GetProperty("Clients")!
                .SetValue(hub, clients.Object);


            await hub.CalculateNewBill("10.00", -5, 11111);

            Assert.IsTrue(File.Exists(LogFile), "Log file was not created!");

            var lines = File.ReadAllLines(LogFile);
            var last = lines[^1];

            StringAssert.Contains(last, "INVALID_MESSAGE");
            StringAssert.Contains(last, "TEST123");
        }
    }
}
