using System.IO;
using Xunit;
using SmartMeterServer.Logging;
namespace SmartMeterServer.Tests
{
   public class ServerErrorLogTests
    {
        [Fact]
        public void Write_ShouldAppendLogEntry()
        {
            // delete the file to ensure everything
            File.Delete("server_logs.csv");
            // write error log
            ServerErrorLog.Write("ABCD123", "SEND_FAILURE");
            // read all the lines
            var lines = File.ReadAllLines("server_logs.csv");
            Assert.Equal("log_id,timestamp,connection_id,event", lines[0]);
            Assert.Contains("ABCD123", lines[^1]);
            Assert.Contains("SEND_FAILURE", lines[^1]);
        }
    }
}