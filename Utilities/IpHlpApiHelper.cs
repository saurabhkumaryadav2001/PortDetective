using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace PortDetective.Utilities
{
    /// <summary>
    /// P/Invoke wrapper for Windows IP Helper API (iphlpapi.dll).
    /// Used to obtain the TCP extended table which maps ports to owning PIDs.
    /// </summary>
    public static class IpHlpApiHelper
    {
        // ---------------------------------------------------------------
        // Native structures
        // ---------------------------------------------------------------

        [StructLayout(LayoutKind.Sequential)]
        private struct MibTcpRowOwnerPid
        {
            public uint DwState;
            public uint DwLocalAddr;
            public uint DwLocalPort;
            public uint DwRemoteAddr;
            public uint DwRemotePort;
            public uint DwOwningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MibTcpTableOwnerPid
        {
            public uint DwNumEntries;
            // Variable-length array follows
        }

        // TCP_TABLE_CLASS enum value for extended (owner PID) table
        private const int TCP_TABLE_OWNER_PID_ALL = 5;
        private const int AF_INET = 2;

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable,
            ref int pdwSize,
            bool bOrder,
            int ulAf,
            int tableClass,
            int reserved);

        // ---------------------------------------------------------------
        // Public result type
        // ---------------------------------------------------------------

        public record TcpConnectionRow(
            int LocalPort,
            string LocalAddress,
            int RemotePort,
            string RemoteAddress,
            uint OwningPid,
            TcpState State);

        // ---------------------------------------------------------------
        // Public method
        // ---------------------------------------------------------------

        public static IReadOnlyList<TcpConnectionRow> GetExtendedTcpTable()
        {
            int bufferSize = 0;
            // First call to get required buffer size
            GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);

            var tableBuffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                uint result = GetExtendedTcpTable(tableBuffer, ref bufferSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);
                if (result != 0)
                    throw new InvalidOperationException($"GetExtendedTcpTable failed with error code {result}");

                var table = Marshal.PtrToStructure<MibTcpTableOwnerPid>(tableBuffer);
                int rowCount = (int)table.DwNumEntries;

                var rows = new List<TcpConnectionRow>(rowCount);
                var rowPtr = IntPtr.Add(tableBuffer, Marshal.SizeOf<MibTcpTableOwnerPid>());
                int rowSize = Marshal.SizeOf<MibTcpRowOwnerPid>();

                for (int i = 0; i < rowCount; i++)
                {
                    var row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(rowPtr);

                    int localPort  = (int)NetworkToHostPort(row.DwLocalPort);
                    int remotePort = (int)NetworkToHostPort(row.DwRemotePort);

                    string localAddr  = new IPAddress(row.DwLocalAddr).ToString();
                    string remoteAddr = new IPAddress(row.DwRemoteAddr).ToString();

                    var state = MapState(row.DwState);

                    rows.Add(new TcpConnectionRow(localPort, localAddr, remotePort, remoteAddr, row.DwOwningPid, state));
                    rowPtr = IntPtr.Add(rowPtr, rowSize);
                }

                return rows;
            }
            finally
            {
                Marshal.FreeHGlobal(tableBuffer);
            }
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static uint NetworkToHostPort(uint netPort)
        {
            // Port bytes are in big-endian order in the structure
            byte[] bytes = BitConverter.GetBytes(netPort);
            return (uint)((bytes[1] << 8) | bytes[0]);
        }

        private static TcpState MapState(uint state) => state switch
        {
            1  => TcpState.Closed,
            2  => TcpState.Listen,
            3  => TcpState.SynSent,
            4  => TcpState.SynReceived,
            5  => TcpState.Established,
            6  => TcpState.FinWait1,
            7  => TcpState.FinWait2,
            8  => TcpState.CloseWait,
            9  => TcpState.Closing,
            10 => TcpState.LastAck,
            11 => TcpState.TimeWait,
            12 => TcpState.DeleteTcb,
            _  => TcpState.Unknown,
        };
    }
}
