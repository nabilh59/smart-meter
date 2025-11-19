using System;
using System.IO;
using System.Text;

namespace SmartMeterServer.Logging
{
    public static class ServerErrorLog
    {
        private static readonly string LogFilePath = "server_logs.csv";
        private static int logCounter = 0;
        private static readonly object lockObj = new();

        static ServerErrorLog()
        {
            // Create header if the file does not exist
            if (!File.Exists(LogFilePath))
            {
                File.WriteAllText(LogFilePath,
                    "log_id,timestamp,connection_id,event\n");
            }
        }

        public static void Write(string connectionId, string eventType)
        {
            lock (lockObj)
            {
                logCounter++;

                string timestamp = DateTime.UtcNow.ToString("o"); // ISO8601

                string row = $"{logCounter},{timestamp},{connectionId},{eventType}\n";

                File.AppendAllText(LogFilePath, row, Encoding.UTF8);
            }
        }
    }
}