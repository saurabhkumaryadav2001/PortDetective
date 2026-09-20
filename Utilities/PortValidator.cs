namespace PortDetective.Utilities
{
    /// <summary>
    /// Validates port numbers and ranges without executing any commands.
    /// </summary>
    public static class PortValidator
    {
        public const int MinPort = 1;
        public const int MaxPort = 65535;

        public static bool IsValidPort(int port) =>
            port >= MinPort && port <= MaxPort;

        public static bool IsValidPort(string input, out int port)
        {
            port = 0;
            return int.TryParse(input?.Trim(), out port) && IsValidPort(port);
        }

        public static bool IsValidRange(int start, int end) =>
            IsValidPort(start) && IsValidPort(end) && start <= end;

        public static bool IsValidRange(string startInput, string endInput, out int start, out int end)
        {
            start = end = 0;
            return IsValidPort(startInput, out start) &&
                   IsValidPort(endInput, out end) &&
                   start <= end;
        }

        public static string? ValidatePort(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Port number is required.";
            if (!int.TryParse(input.Trim(), out int port))
                return "Port must be a number.";
            if (!IsValidPort(port))
                return $"Port must be between {MinPort} and {MaxPort}.";
            return null;
        }

        public static string? ValidateRange(string startInput, string endInput)
        {
            if (string.IsNullOrWhiteSpace(startInput) || string.IsNullOrWhiteSpace(endInput))
                return "Both start and end ports are required.";

            if (!int.TryParse(startInput.Trim(), out int start))
                return "Start port must be a number.";
            if (!int.TryParse(endInput.Trim(), out int end))
                return "End port must be a number.";
            if (!IsValidPort(start))
                return $"Start port must be between {MinPort} and {MaxPort}.";
            if (!IsValidPort(end))
                return $"End port must be between {MinPort} and {MaxPort}.";
            if (start > end)
                return "Start port must be less than or equal to end port.";
            if (end - start > 10000)
                return "Port range cannot exceed 10,000 ports for performance reasons.";

            return null;
        }
    }
}
