using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace WhoEatWifi.Core
{
    public class TcpConnectionSnapshot
    {
        public string ConnectionKey { get; set; }
        public int ProcessId { get; set; }
        public ulong DataBytesIn { get; set; }
        public ulong DataBytesOut { get; set; }
    }

    public class PidNetworkActivity
    {
        public int ProcessId { get; set; }
        public int ActiveTcpConnectionCount { get; set; }
        public int ListeningTcpCount { get; set; }
        public int ActiveUdpSocketCount { get; set; }

        public int TotalActiveSockets
        {
            get { return ActiveTcpConnectionCount + ActiveUdpSocketCount; }
        }
    }

    public class NetworkSnapshotResult
    {
        public Dictionary<int, PidNetworkActivity> Activities { get; set; }
        public List<TcpConnectionSnapshot> TcpConnections { get; set; }
        public bool IsEStatsActive { get; set; }

        public NetworkSnapshotResult()
        {
            Activities = new Dictionary<int, PidNetworkActivity>();
            TcpConnections = new List<TcpConnectionSnapshot>();
            IsEStatsActive = false;
        }
    }

    public static class IphlpNetworkMonitor
    {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize, bool bOrder, int ulAf, int tableClass, uint reserved);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int pdwSize, bool bOrder, int ulAf, int tableClass, uint reserved);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint SetPerTcpConnectionEStats(
            ref MIB_TCPROW row,
            int estatsType,
            ref TCP_ESTATS_DATA_RW_v0 rw,
            uint rwVersion,
            uint rwSize,
            uint offset);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetPerTcpConnectionEStats(
            ref MIB_TCPROW row,
            int estatsType,
            IntPtr rw,
            uint rwVersion,
            uint rwSize,
            IntPtr ros,
            uint rosVersion,
            uint rosSize,
            ref TCP_ESTATS_DATA_ROD_v0 rod,
            uint rodVersion,
            uint rodSize);

        private const int AF_INET = 2;
        private const int AF_INET6 = 23;
        private const int TCP_TABLE_OWNER_PID_ALL = 5;
        private const int UDP_TABLE_OWNER_PID = 1;
        private const int TcpConnectionEstatsData = 0;

        // TCP State enum constants
        private const uint MIB_TCP_STATE_LISTEN = 2;
        private const uint MIB_TCP_STATE_ESTAB = 5;

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW
        {
            public uint dwState;
            public uint dwLocalAddr;
            public uint dwLocalPort;
            public uint dwRemoteAddr;
            public uint dwRemotePort;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public uint dwState;
            public uint dwLocalAddr;
            public uint dwLocalPort;
            public uint dwRemoteAddr;
            public uint dwRemotePort;
            public uint dwOwningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCP6ROW_OWNER_PID
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] ucLocalAddr;
            public uint dwLocalScopeId;
            public uint dwLocalPort;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] ucRemoteAddr;
            public uint dwRemoteScopeId;
            public uint dwRemotePort;
            public uint dwState;
            public uint dwOwningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPROW_OWNER_PID
        {
            public uint dwLocalAddr;
            public uint dwLocalPort;
            public uint dwOwningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDP6ROW_OWNER_PID
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] ucLocalAddr;
            public uint dwLocalScopeId;
            public uint dwLocalPort;
            public uint dwOwningPid;
        }

        private enum TCP_BOOLEAN_OPTIONAL : int
        {
            TcpBoolOptDisabled = 0,
            TcpBoolOptEnabled = 1,
            TcpBoolOptUnchanged = -1
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TCP_ESTATS_DATA_RW_v0
        {
            public TCP_BOOLEAN_OPTIONAL EnableCollection;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TCP_ESTATS_DATA_ROD_v0
        {
            public ulong DataBytesOut;
            public ulong DataSegsOut;
            public ulong DataBytesIn;
            public ulong DataSegsIn;
            public ulong SegsOut;
            public ulong SegsIn;
            public uint SoftErrors;
            public uint SoftErrorReason;
            public uint SndUna;
            public uint SndNxt;
            public uint SndMax;
            public ulong ThruBytesAcked;
            public uint RcvNxt;
            public ulong ThruBytesReceived;
        }

        private static readonly HashSet<string> _enabledConnections = new HashSet<string>();
        private static bool? _isEStatsSupported = null;
        private static int _scanCycle = 0;

        public static bool IsEStatsActive
        {
            get
            {
                if (_isEStatsSupported.HasValue)
                {
                    return _isEStatsSupported.Value;
                }
                return IsRunningAsAdmin();
            }
        }

        public static bool IsRunningAsAdmin()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        public static NetworkSnapshotResult GetNetworkSnapshot()
        {
            NetworkSnapshotResult result = new NetworkSnapshotResult();
            HashSet<string> currentActiveKeys = new HashSet<string>();

            _scanCycle++;

            // 1. TCP IPv4 (includes EStats for established connections)
            ScanTcpIpv4(result, currentActiveKeys);

            // 2. TCP IPv6 (differentiate active vs listening)
            ScanTcpIpv6(result.Activities);

            // 3. UDP IPv4
            ScanUdpIpv4(result.Activities);

            // 4. UDP IPv6
            ScanUdpIpv6(result.Activities);

            // Periodically prune stale connections from _enabledConnections
            if (_scanCycle % 60 == 0 && _enabledConnections.Count > 1000)
            {
                _enabledConnections.IntersectWith(currentActiveKeys);
            }

            result.IsEStatsActive = IsEStatsActive;
            return result;
        }

        private static void ScanTcpIpv4(NetworkSnapshotResult result, HashSet<string> activeKeys)
        {
            int size = 0;
            uint res = GetExtendedTcpTable(IntPtr.Zero, ref size, false, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);
            if (size <= 0) return;

            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                res = GetExtendedTcpTable(buffer, ref size, false, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);
                if (res != 0)
                {
                    Debug.WriteLine(string.Format("GetExtendedTcpTable IPv4 failed with code: {0}", res));
                    return;
                }

                int numEntries = Marshal.ReadInt32(buffer);
                IntPtr rowPtr = new IntPtr(buffer.ToInt64() + 4);
                int rowSize = Marshal.SizeOf(typeof(MIB_TCPROW_OWNER_PID));

                for (int i = 0; i < numEntries; i++)
                {
                    MIB_TCPROW_OWNER_PID row = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));
                    int pid = (int)row.dwOwningPid;
                    if (pid > 0)
                    {
                        PidNetworkActivity act;
                        if (!result.Activities.TryGetValue(pid, out act))
                        {
                            act = new PidNetworkActivity { ProcessId = pid };
                            result.Activities[pid] = act;
                        }

                        // Filter out listening sockets
                        if (row.dwState == MIB_TCP_STATE_LISTEN)
                        {
                            act.ListeningTcpCount++;
                        }
                        else
                        {
                            act.ActiveTcpConnectionCount++;

                            // Only query EStats for ESTABLISHED connections
                            if (row.dwState == MIB_TCP_STATE_ESTAB)
                            {
                                string connKey = string.Format("{0}:{1}->{2}:{3}",
                                    row.dwLocalAddr, row.dwLocalPort, row.dwRemoteAddr, row.dwRemotePort);
                                activeKeys.Add(connKey);

                                MIB_TCPROW baseRow = new MIB_TCPROW
                                {
                                    dwState = row.dwState,
                                    dwLocalAddr = row.dwLocalAddr,
                                    dwLocalPort = row.dwLocalPort,
                                    dwRemoteAddr = row.dwRemoteAddr,
                                    dwRemotePort = row.dwRemotePort
                                };

                                // Enable collection once per connection
                                if (!_enabledConnections.Contains(connKey))
                                {
                                    TCP_ESTATS_DATA_RW_v0 rw = new TCP_ESTATS_DATA_RW_v0 { EnableCollection = TCP_BOOLEAN_OPTIONAL.TcpBoolOptEnabled };
                                    uint setRes = SetPerTcpConnectionEStats(
                                        ref baseRow,
                                        TcpConnectionEstatsData,
                                        ref rw,
                                        0,
                                        (uint)Marshal.SizeOf(typeof(TCP_ESTATS_DATA_RW_v0)),
                                        0);

                                    if (setRes == 0)
                                    {
                                        _enabledConnections.Add(connKey);
                                        _isEStatsSupported = true;
                                    }
                                    else
                                    {
                                        if (setRes == 5) // ERROR_ACCESS_DENIED
                                        {
                                            _isEStatsSupported = false;
                                        }
                                        Debug.WriteLine(string.Format("SetPerTcpConnectionEStats failed for {0}: {1}", connKey, setRes));
                                    }
                                }

                                // Read EStats metrics if enabled
                                if (_enabledConnections.Contains(connKey))
                                {
                                    TCP_ESTATS_DATA_ROD_v0 rod = new TCP_ESTATS_DATA_ROD_v0();
                                    uint getRes = GetPerTcpConnectionEStats(
                                        ref baseRow,
                                        TcpConnectionEstatsData,
                                        IntPtr.Zero,
                                        0,
                                        0,
                                        IntPtr.Zero,
                                        0,
                                        0,
                                        ref rod,
                                        0,
                                        (uint)Marshal.SizeOf(typeof(TCP_ESTATS_DATA_ROD_v0)));

                                    if (getRes == 0)
                                    {
                                        result.TcpConnections.Add(new TcpConnectionSnapshot
                                        {
                                            ConnectionKey = connKey,
                                            ProcessId = pid,
                                            DataBytesIn = rod.DataBytesIn,
                                            DataBytesOut = rod.DataBytesOut
                                        });
                                    }
                                }
                            }
                        }
                    }
                    rowPtr = new IntPtr(rowPtr.ToInt64() + rowSize);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ScanTcpIpv4 exception: " + ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static void ScanTcpIpv6(Dictionary<int, PidNetworkActivity> dict)
        {
            int size = 0;
            uint res = GetExtendedTcpTable(IntPtr.Zero, ref size, false, AF_INET6, TCP_TABLE_OWNER_PID_ALL, 0);
            if (size <= 0) return;

            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                res = GetExtendedTcpTable(buffer, ref size, false, AF_INET6, TCP_TABLE_OWNER_PID_ALL, 0);
                if (res != 0) return;

                int numEntries = Marshal.ReadInt32(buffer);
                IntPtr rowPtr = new IntPtr(buffer.ToInt64() + 4);
                int rowSize = Marshal.SizeOf(typeof(MIB_TCP6ROW_OWNER_PID));

                for (int i = 0; i < numEntries; i++)
                {
                    MIB_TCP6ROW_OWNER_PID row = (MIB_TCP6ROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCP6ROW_OWNER_PID));
                    int pid = (int)row.dwOwningPid;
                    if (pid > 0)
                    {
                        PidNetworkActivity act;
                        if (!dict.TryGetValue(pid, out act))
                        {
                            act = new PidNetworkActivity { ProcessId = pid };
                            dict[pid] = act;
                        }

                        // Filter out listening sockets
                        if (row.dwState == MIB_TCP_STATE_LISTEN)
                        {
                            act.ListeningTcpCount++;
                        }
                        else
                        {
                            act.ActiveTcpConnectionCount++;
                        }
                    }
                    rowPtr = new IntPtr(rowPtr.ToInt64() + rowSize);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ScanTcpIpv6 exception: " + ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static void ScanUdpIpv4(Dictionary<int, PidNetworkActivity> dict)
        {
            int size = 0;
            uint res = GetExtendedUdpTable(IntPtr.Zero, ref size, false, AF_INET, UDP_TABLE_OWNER_PID, 0);
            if (size <= 0) return;

            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                res = GetExtendedUdpTable(buffer, ref size, false, AF_INET, UDP_TABLE_OWNER_PID, 0);
                if (res != 0) return;

                int numEntries = Marshal.ReadInt32(buffer);
                IntPtr rowPtr = new IntPtr(buffer.ToInt64() + 4);
                int rowSize = Marshal.SizeOf(typeof(MIB_UDPROW_OWNER_PID));

                for (int i = 0; i < numEntries; i++)
                {
                    MIB_UDPROW_OWNER_PID row = (MIB_UDPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_UDPROW_OWNER_PID));
                    int pid = (int)row.dwOwningPid;
                    if (pid > 0)
                    {
                        PidNetworkActivity act;
                        if (!dict.TryGetValue(pid, out act))
                        {
                            act = new PidNetworkActivity { ProcessId = pid };
                            dict[pid] = act;
                        }

                        // Only count active sockets (non-zero port)
                        if (row.dwLocalPort > 0)
                        {
                            act.ActiveUdpSocketCount++;
                        }
                    }
                    rowPtr = new IntPtr(rowPtr.ToInt64() + rowSize);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ScanUdpIpv4 exception: " + ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static void ScanUdpIpv6(Dictionary<int, PidNetworkActivity> dict)
        {
            int size = 0;
            uint res = GetExtendedUdpTable(IntPtr.Zero, ref size, false, AF_INET6, UDP_TABLE_OWNER_PID, 0);
            if (size <= 0) return;

            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                res = GetExtendedUdpTable(buffer, ref size, false, AF_INET6, UDP_TABLE_OWNER_PID, 0);
                if (res != 0) return;

                int numEntries = Marshal.ReadInt32(buffer);
                IntPtr rowPtr = new IntPtr(buffer.ToInt64() + 4);
                int rowSize = Marshal.SizeOf(typeof(MIB_UDP6ROW_OWNER_PID));

                for (int i = 0; i < numEntries; i++)
                {
                    MIB_UDP6ROW_OWNER_PID row = (MIB_UDP6ROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_UDP6ROW_OWNER_PID));
                    int pid = (int)row.dwOwningPid;
                    if (pid > 0)
                    {
                        PidNetworkActivity act;
                        if (!dict.TryGetValue(pid, out act))
                        {
                            act = new PidNetworkActivity { ProcessId = pid };
                            dict[pid] = act;
                        }

                        if (row.dwLocalPort > 0)
                        {
                            act.ActiveUdpSocketCount++;
                        }
                    }
                    rowPtr = new IntPtr(rowPtr.ToInt64() + rowSize);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ScanUdpIpv6 exception: " + ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
