using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SmartMeterServer.Controllers;
using SmartMeterServer.Hubs;
using SmartMeterServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMeterServer.Tests
{
    [TestClass]
    public class GridDownTests
    {
        private Mock<IHubContext<FirstHub>> _hubMock = new();
        private Mock<IHubClients> _clientsMock = new();
        private Mock<IClientProxy> _clientProxyMock = new();

        [TestInitialize]
        public void Setup()
        {
            _hubMock = new Mock<IHubContext<FirstHub>>();
            _clientsMock = new Mock<IHubClients>();
            _clientProxyMock = new Mock<IClientProxy>();

            _hubMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
            _clientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
        }
        [TestMethod]
        public async Task DownSuccess()
        {
            var controller = new GridController(_hubMock.Object);
            var result = await controller.Down() as OkObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            Assert.AreEqual("DOWN", GridState.Current);

            _clientProxyMock.Verify(
                c => c.SendCoreAsync(
                    "gridStatus",
                    It.IsAny<object[]>(),
                    default
                ),
                Times.Once
            );

        }
        [TestMethod]
        public async Task UpSuccess()
        {
            var controller = new GridController(_hubMock.Object);
            var result = await controller.Up() as OkObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            Assert.AreEqual("UP", GridState.Current);

            _clientProxyMock.Verify(
                c => c.SendCoreAsync(
                    "gridStatus",
                    It.IsAny<object[]>(),
                    default
                ),
                Times.Once
            );
        }
        [TestMethod]
        public async Task DownFailure()
        {
            _clientProxyMock
                .Setup(c => c.SendCoreAsync(
                    "gridStatus",
                    It.IsAny<object[]>(),
                    default
                ))
                .ThrowsAsync(new Exception("fail"));


            var controller = new GridController(_hubMock.Object);
            var result = await controller.Down() as ObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(500, result.StatusCode);
            var body = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(
                Newtonsoft.Json.JsonConvert.SerializeObject(result.Value)
            );

            Assert.IsFalse((bool?)body?["ok"]);
        }
        [TestMethod]
        public async Task UpFailure()
        {
            _clientProxyMock
                .Setup(c => c.SendCoreAsync(
                    "gridStatus",
                    It.IsAny<object[]>(),
                    default
                ))
                .ThrowsAsync(new Exception("fail"));
            var controller = new GridController(_hubMock.Object);
            var result = await controller.Up() as ObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(500, result.StatusCode);
            var body = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(
                Newtonsoft.Json.JsonConvert.SerializeObject(result.Value)
            );

            Assert.IsFalse((bool?)body?["ok"]);
        }
        [TestMethod]
        public void InvalidCommand()
        {
            var controller = new GridController(_hubMock.Object);
            var result = controller.InvalidCommand() as BadRequestObjectResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.StatusCode);
            var body = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(
                Newtonsoft.Json.JsonConvert.SerializeObject(result.Value)
            );
            Assert.IsFalse((bool?)body?["ok"]);

        }
    }
}
