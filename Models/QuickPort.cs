namespace PortDetective.Models
{
    /// <summary>
    /// Represents a well-known developer port in the quick-access bar.
    /// </summary>
    public class QuickPort
    {
        public int Port { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsInUse { get; set; }

        public static readonly QuickPort[] DefaultPorts =
        {
            new() { Port = 3000,  Label = "3000",  Description = "Node.js / React" },
            new() { Port = 4200,  Label = "4200",  Description = "Angular CLI" },
            new() { Port = 5000,  Label = "5000",  Description = ".NET Kestrel" },
            new() { Port = 5173,  Label = "5173",  Description = "Vite Dev Server" },
            new() { Port = 5432,  Label = "5432",  Description = "PostgreSQL" },
            new() { Port = 6379,  Label = "6379",  Description = "Redis" },
            new() { Port = 8000,  Label = "8000",  Description = "Python / Django" },
            new() { Port = 8080,  Label = "8080",  Description = "HTTP Alt / Tomcat" },
            new() { Port = 8081,  Label = "8081",  Description = "HTTP Alt" },
            new() { Port = 1433,  Label = "1433",  Description = "SQL Server" },
            new() { Port = 3306,  Label = "3306",  Description = "MySQL" },
        };
    }
}
