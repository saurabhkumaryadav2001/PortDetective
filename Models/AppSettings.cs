namespace PortDetective.Models
{
    /// <summary>
    /// Application settings persisted to disk.
    /// </summary>
    public class AppSettings
    {
        public int AutoRefreshIntervalSeconds { get; set; } = 10;
        public bool AutoRefreshEnabled { get; set; } = true;
        public int DefaultPortRangeStart { get; set; } = 1;
        public int DefaultPortRangeEnd { get; set; } = 65535;
        public bool StartMinimized { get; set; } = false;
        public bool StartWithWindows { get; set; } = false;
        public string Theme { get; set; } = "Dark";
        public bool ShowSystemProcesses { get; set; } = true;
    }
}
