using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PortDetective.Models;

namespace PortDetective.Interfaces
{
    /// <summary>
    /// Provides TCP port scanning capabilities using Windows networking APIs.
    /// </summary>
    public interface IPortScannerService
    {
        /// <summary>Gets all currently active TCP connections with their owning process info.</summary>
        Task<IReadOnlyList<PortInfo>> GetActivePortsAsync(CancellationToken cancellationToken = default);

        /// <summary>Checks whether a specific port is in use and returns its info, or null if free.</summary>
        Task<PortInfo?> GetPortInfoAsync(int port, CancellationToken cancellationToken = default);

        /// <summary>Scans a range of ports and returns only those currently in use.</summary>
        Task<IReadOnlyList<PortInfo>> ScanPortRangeAsync(int startPort, int endPort, CancellationToken cancellationToken = default);
    }
}
