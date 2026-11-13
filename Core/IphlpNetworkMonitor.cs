using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WhoEatWifi.Core
{
    public class PidNetworkActivity
    {
        public int ProcessId { get; set; }
        public int TcpConnectionCount { get; set; }
        public int UdpSocketCount { get; set; }
        public ulong TcpBytesIn { get; set; }
        public ulong TcpBytesOut { get; set; }

        public int TotalSocketCount
        {
            get { return TcpConnectionCount + UdpSocketCount; }
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

        [StructLayout(LayoutKind.Sequential)]
        private struct TCP_ESTATS_DATA_RW_v0
        {
            public bool EnableCollection;
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

        public static Dictionary<int, PidNetworkActivity> GetActiveNetworkActivity()
        {
            Dictionary<int, PidNetworkActivity> dict = new Dictionary<int, PidNetworkActivity>();

            // 1. TCP IPv4
            ScanTcpIpv4(dict);

            // 2. TCP IPv6
            ScanTcpIpv6(dict);

            // 3. UDP IPv4
            ScanUdpIpv4(dict);

            // 4. UDP IPv6
            ScanUdpIpv6(dict);

            return dict;
        }

        private static void ScanTcpIpv4(Dictionary<int, PidNetworkActivity> dict)
        {
            try
            {
                int size = 0;
                uint res = GetExtendedTcpTable(IntPtr.Zero, ref size, false, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);
                if (size <= 0) return;

                IntPtr buffer = Marshal.AllocHGlobal(size);
                try
                {
                    res = GetExtendedTcpTable(buffer, ref size, false, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);
                    if (res == 0)
                    {
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
                                if (!dict.TryGetValue(pid, out act))
                                {
                                    act = new PidNetworkActivity { ProcessId = pid };
                                    dict[pid] = act;
                                }
                                act.TcpConnectionCount++;

                                // Query TCP EStats (state == 5: ESTABLISHED)
                                if (row.dwState == 5)
                                {
                                    try
                                    {
                                        MIB_TCPROW baseRow = new MIB_TCPROW
                                        {
                                            dwState = row.dwState,
                                            dwLocalAddr = row.dwLocalAddr,
                                            dwLocalPort = row.dwLocalPort,
                                            dwRemoteAddr = row.dwRemoteAddr,
                                            dwRemotePort = row.dwRemotePort
                                        };

                                        // Enable collection
                                        TCP_ESTATS_DATA_RW_v0 rw = new TCP_ESTATS_DATA_RW_v0 { EnableCollection = true };
                                        SetPerTcpConnectionEStats(ref baseRow, TcpConnectionEstatsData, ref rw, 0, (uint)Marshal.SizeOf(typeof(TCP_ESTATS_DATA_RW_v0)), 0);

                                        // Read collection
                                        TCP_ESTATS_DATA_ROD_v0 rod = new TCP_ESTATS_DATA_ROD_v0();
                                        uint estatRes = GetPerTcpConnectionEStats(ref baseRow, TcpConnectionEstatsData, IntPtr.Zero, 0, 0, IntPtr.Zero, 0, 0, ref rod, 0, (uint)Marshal.SizeOf(typeof(TCP_ESTATS_DATA_ROD_v0)));
                                        if (estatRes == 0)
                                        {
                                            act.TcpBytesIn += rod.DataBytesIn;
                                            act.TcpBytesOut += rod.DataBytesOut;
                                        }
                                    }
                                    catch
                                    {
                                        // Ignore EStats failure for individual connection
                                    }
                                }
                            }
                            rowPtr = new IntPtr(rowPtr.ToInt64() + rowSize);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch
            {
                // Ignore API failure
            }
        }

        private static void ScanTcpIpv6(Dictionary<int, PidNetworkActivity> dict)
        {
            try
            {
                int size = 0;
                uint res = GetExtendedTcpTable(IntPtr.Zero, ref size, false, AF_INET6, TCP_TABLE_OWNER_PID_ALL, 0);
                if (size <= 0) return;

                IntPtr buffer = Marshal.AllocHGlobal(size);
                try
                {
                    res = GetExtendedTcpTable(buffer, ref size, false, AF_INET6, TCP_TABLE_OWNER_PID_ALL, 0);
                    if (res == 0)
                    {
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
                                act.TcpConnectionCount++;
                            }
                            rowPtr = new IntPtr(rowPtr.ToInt64() + rowSize);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch
            {
                // Ignore API failure
            }
        }

        private static void ScanUdpIpv4(Dictionary<int, PidNetworkActivity> dict)
        {
            try
            {
                int size = 0;
                uint res = GetExtendedUdpTable(IntPtr.Zero, ref size, false, AF_INET, UDP_TABLE_OWNER_PID, 0);
                if (size <= 0) return;

                IntPtr buffer = Marshal.AllocHGlobal(size);
                try
                {
                    res = GetExtendedUdpTable(buffer, ref size, false, AF_INET, UDP_TABLE_OWNER_PID, 0);
                    if (res == 0)
                    {
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
                                act.UdpSocketCount++;
                            }
                            rowPtr = new IntPtr(rowPtr.ToInt64() + rowSize);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch
            {
                // Ignore API failure
            }
        }

        private static void ScanUdpIpv6(Dictionary<int, PidNetworkActivity> dict)
        {
            try
            {
                int size = 0;
                uint res = GetExtendedUdpTable(IntPtr.Zero, ref size, false, AF_INET6, UDP_TABLE_OWNER_PID, 0);
                if (size <= 0) return;

                IntPtr buffer = Marshal.AllocHGlobal(size);
                try
                {
                    res = GetExtendedUdpTable(buffer, ref size, false, AF_INET6, UDP_TABLE_OWNER_PID, 0);
                    if (res == 0)
                    {
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
                                act.UdpSocketCount++;
                            }
                            rowPtr = new IntPtr(rowPtr.ToInt64() + rowSize);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch
            {
                // Ignore API failure
            }
        }
    }
}
