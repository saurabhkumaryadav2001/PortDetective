using System;

namespace PortDetective.Models
{
    /// <summary>
    /// Extended details about a process associated with a port.
    /// </summary>
    public class ProcessDetails
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string CommandLine { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsSystemProcess { get; set; }
        public string UserName { get; set; } = string.Empty;

        public string StartTimeDisplay => StartTime.HasValue
            ? StartTime.Value.ToString("yyyy-MM-dd HH:mm:ss")
            : "N/A";
    }
}
