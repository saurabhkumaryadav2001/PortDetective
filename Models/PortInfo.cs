using System;
using System.Net.NetworkInformation;

namespace PortDetective.Models
{
    /// <summary>
    /// Represents information about a TCP port and the process occupying it.
    /// </summary>
    public class PortInfo
    {
        public int Port { get; set; }
        public string Protocol { get; set; } = "TCP";
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public TcpState ConnectionState { get; set; }
        public string LocalAddress { get; set; } = "127.0.0.1";
        public string RemoteAddress { get; set; } = string.Empty;

        public string ConnectionStateDisplay => ConnectionState switch
        {
            TcpState.Listen      => "Listening",
            TcpState.Established => "Established",
            TcpState.TimeWait    => "Time Wait",
            TcpState.CloseWait   => "Close Wait",
            TcpState.SynSent     => "Syn Sent",
            TcpState.SynReceived => "Syn Received",
            TcpState.FinWait1    => "Fin Wait 1",
            TcpState.FinWait2    => "Fin Wait 2",
            TcpState.Closed      => "Closed",
            TcpState.Closing     => "Closing",
            TcpState.LastAck     => "Last Ack",
            TcpState.DeleteTcb   => "Delete TCB",
            _                    => "Unknown"
        };
    }
}
