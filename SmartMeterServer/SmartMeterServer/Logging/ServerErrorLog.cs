using System;
using System.IO;
using System.Text;

namespace SmartMeterServer.Logging
{
    public static class ServerErrorLog
    {
        private static readonly string LogFilePath = "server_logs.csv";
        private static readonly object lockObj = new();

        static ServerErrorLog()
        {
            // Create header if the file does not exist
            if (!File.Exists(LogFilePath))
            {
                File.WriteAllText(LogFilePath,
                    "Timestamp,Connection_id,Error\n");
            }
        }

        public static void Write(string connectionId, string eventType)
        {
            lock (lockObj)
            {

                string timestamp = DateTime.UtcNow.ToString("o"); // ISO8601

                string row = $"{timestamp},{connectionId},{eventType}\n";

                File.AppendAllText(LogFilePath, row, Encoding.UTF8);
            }
        }
    }
}