using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PortDetective.Interfaces;
using PortDetective.Models;
using PortDetective.Utilities;

namespace PortDetective.Services
{
    /// <summary>
    /// Scans TCP ports using Windows IP Helper API (iphlpapi.dll) via P/Invoke
    /// for accurate PID-to-port mapping, falling back to System.Net.NetworkInformation
    /// for connection state data.
    /// </summary>
    public sealed class PortScannerService : IPortScannerService
    {
        private readonly ILogger<PortScannerService> _logger;

        public PortScannerService(ILogger<PortScannerService> logger)
        {
            _logger = logger;
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        public Task<IReadOnlyList<PortInfo>> GetActivePortsAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                _logger.LogDebug("Fetching all active TCP ports");
                var results = GetAllTcpConnections();
                _logger.LogDebug("Found {Count} active TCP ports", results.Count);
                return (IReadOnlyList<PortInfo>)results;
            }, cancellationToken);
        }

        public Task<PortInfo?> GetPortInfoAsync(int port, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                _logger.LogDebug("Looking up port {Port}", port);
                var all = GetAllTcpConnections();
                return all.FirstOrDefault(p => p.Port == port);
            }, cancellationToken);
        }

        public Task<IReadOnlyList<PortInfo>> ScanPortRangeAsync(int startPort, int endPort, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                _logger.LogInformation("Scanning port range {Start}-{End}", startPort, endPort);
                var all = GetAllTcpConnections();
                var inRange = all
                    .Where(p => p.Port >= startPort && p.Port <= endPort)
                    .OrderBy(p => p.Port)
                    .ToList();
                _logger.LogInformation("Found {Count} active ports in range {Start}-{End}", inRange.Count, startPort, endPort);
                return (IReadOnlyList<PortInfo>)inRange;
            }, cancellationToken);
        }

        // ---------------------------------------------------------------
        // Core implementation using Windows IP Helper API
        // ---------------------------------------------------------------

        private List<PortInfo> GetAllTcpConnections()
        {
            var results = new List<PortInfo>();

            try
            {
                // Use Windows API for PID information
                var extendedTable = IpHlpApiHelper.GetExtendedTcpTable();

                // Enrich with TcpState from System.Net.NetworkInformation
                var stateMap = BuildStateMap();

                foreach (var row in extendedTable)
                {
                    var portInfo = new PortInfo
                    {
                        Port          = row.LocalPort,
                        Protocol      = "TCP",
                        ProcessId     = (int)row.OwningPid,
                        LocalAddress  = row.LocalAddress,
                        RemoteAddress = row.RemoteAddress,
                        ConnectionState = row.State,
                    };

                    // Get state from NetworkInformation if available (more reliable for state display)
                    if (stateMap.TryGetValue(row.LocalPort, out var state))
                        portInfo.ConnectionState = state;

                    EnrichWithProcessInfo(portInfo);
                    results.Add(portInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving TCP connections via IP Helper API");
            }

            return results.OrderBy(p => p.Port).ToList();
        }

        private static Dictionary<int, TcpState> BuildStateMap()
        {
            var map = new Dictionary<int, TcpState>();
            try
            {
                var props = IPGlobalProperties.GetIPGlobalProperties();
                foreach (var conn in props.GetActiveTcpConnections())
                {
                    int port = conn.LocalEndPoint.Port;
                    if (!map.ContainsKey(port))
                        map[port] = conn.State;
                }
                foreach (var listener in props.GetActiveTcpListeners())
                {
                    int port = listener.Port;
                    if (!map.ContainsKey(port))
                        map[port] = TcpState.Listen;
                }
            }
            catch
            {
                // Non-critical — state display will use the API value
            }
            return map;
        }

        private void EnrichWithProcessInfo(PortInfo portInfo)
        {
            if (portInfo.ProcessId <= 0) return;

            try
            {
                using var process = Process.GetProcessById(portInfo.ProcessId);
                portInfo.ProcessName = process.ProcessName;

                try
                {
                    portInfo.ExecutablePath = process.MainModule?.FileName ?? string.Empty;
                }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
                {
                    portInfo.ExecutablePath = string.Empty;
                }
            }
            catch (ArgumentException)
            {
                // Process no longer exists
                portInfo.ProcessName = "(exited)";
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Could not get process info for PID {Pid}", portInfo.ProcessId);
                portInfo.ProcessName = "(access denied)";
            }
        }
    }
}
