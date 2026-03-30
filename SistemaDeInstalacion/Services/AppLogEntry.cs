using System;

namespace ConcesionaroCarros.Services
{
    public class AppLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string MachineName { get; set; }
        public string UserName { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public long? DurationMs { get; set; }
        public string LogFilePath { get; set; }
    }

    public class LogMachineOption
    {
        public string MachineName { get; set; }
        public string DisplayName { get; set; }
    }
}
