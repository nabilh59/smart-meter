using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartMeterServer.Logging;
using System.IO;

namespace SmartMeterServer.Tests
{
    [TestClass]
    public class ServerErrorLogTests
    {
        private const string LogFile = "server_logs.csv";

        [TestInitialize]
        public void Setup()
        {
            // Ensure clean log before each test
            if (File.Exists(LogFile))
                File.Delete(LogFile);
        }

        [TestMethod]
        public void Write_ShouldAppendLogEntry()
        {
            // Act — write log entry
            ServerErrorLog.Write("ABCD123", "SEND_FAILURE");

            // Assert — file exists
            Assert.IsTrue(File.Exists(LogFile));

            // Read all lines
            var lines = File.ReadAllLines(LogFile);

            // Header must be correct
            Assert.AreEqual("log_id,timestamp,connection_id,event", lines[0]);

            // Last line should contain correct fields
            string lastLine = lines[^1];

            StringAssert.Contains(lastLine, "ABCD123");
            StringAssert.Contains(lastLine, "SEND_FAILURE");
        }
    }
}
