using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace WhoEatWifi.Core
{
    public class WifiInfo
    {
        public string Ssid { get; set; }
        public string InterfaceName { get; set; }
        public string InterfaceDescription { get; set; }
        public bool IsConnected { get; set; }
        public bool IsMetered { get; set; }
        public int SignalQuality { get; set; }

        public WifiInfo()
        {
            Ssid = "未连接";
            InterfaceName = "无可用网卡";
            InterfaceDescription = string.Empty;
            IsConnected = false;
            IsMetered = false;
            SignalQuality = 0;
        }
    }

    public static class WifiHelper
    {
        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern uint WlanOpenHandle(uint dwClientVersion, IntPtr pReserved, out uint pdwNegotiatedVersion, out IntPtr phClientHandle);

        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern uint WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);

        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern uint WlanEnumInterfaces(IntPtr hClientHandle, IntPtr pReserved, out IntPtr ppInterfaceList);

        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern uint WlanQueryInterface(IntPtr hClientHandle, ref Guid pInterfaceGuid, int opCode, IntPtr pReserved, out uint pdwDataSize, out IntPtr ppData, IntPtr pWlanOpcodeValueType);

        [DllImport("wlanapi.dll")]
        private static extern void WlanFreeMemory(IntPtr pMemory);

        private const int WLAN_INTF_OPCODE_CURRENT_CONNECTION = 7;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_INTERFACE_INFO
        {
            public Guid InterfaceGuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strInterfaceDescription;
            public int isState;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WLAN_INTERFACE_INFO_LIST
        {
            public uint dwNumberOfItems;
            public uint dwIndex;
            public WLAN_INTERFACE_INFO InterfaceInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DOT11_SSID
        {
            public uint uSSIDLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] ucSSID;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_ASSOCIATION_ATTRIBUTES
        {
            public DOT11_SSID dot11Ssid;
            public int dot11BssType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] dot11Bssid;
            public int dot11PhyType;
            public uint uDot11PhyIndex;
            public uint wlanSignalQuality;
            public uint ulRxRate;
            public uint ulTxRate;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_CONNECTION_ATTRIBUTES
        {
            public int isState;
            public int wlanConnectionMode;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strProfileName;
            public WLAN_ASSOCIATION_ATTRIBUTES wlanAssociationAttributes;
            // Security attributes follow
        }

        public static WifiInfo GetCurrentWifiInfo()
        {
            WifiInfo info = new WifiInfo();

            // 1. Try Native WlanApi first
            try
            {
                WifiInfo wlanResult = QueryWlanApi();
                if (wlanResult != null && wlanResult.IsConnected)
                {
                    return wlanResult;
                }
            }
            catch
            {
                // Fallback to NetworkInterface
            }

            // 2. Fallback to System.Net.NetworkInformation
            try
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface ni in interfaces)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                         ni.Description.IndexOf("Wi-Fi", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         ni.Description.IndexOf("Wireless", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         ni.Name.IndexOf("Wi-Fi", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         ni.Name.IndexOf("WLAN", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        info.IsConnected = true;
                        info.InterfaceName = ni.Name;
                        info.InterfaceDescription = ni.Description;
                        info.Ssid = !string.IsNullOrEmpty(ni.Name) ? ni.Name : "已连接无线网络";
                        break;
                    }
                }

                // If no wireless interface found, check for active Ethernet or other up interfaces
                if (!info.IsConnected)
                {
                    foreach (NetworkInterface ni in interfaces)
                    {
                        if (ni.OperationalStatus == OperationalStatus.Up &&
                            ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        {
                            info.IsConnected = true;
                            info.InterfaceName = ni.Name;
                            info.InterfaceDescription = ni.Description;
                            info.Ssid = ni.Name;
                            break;
                        }
                    }
                }
            }
            catch
            {
                // Ignore fallback errors
            }

            return info;
        }

        private static WifiInfo QueryWlanApi()
        {
            IntPtr clientHandle = IntPtr.Zero;
            uint negotiatedVersion;
            uint res = WlanOpenHandle(2, IntPtr.Zero, out negotiatedVersion, out clientHandle);
            if (res != 0 || clientHandle == IntPtr.Zero)
            {
                return null;
            }

            IntPtr pIfList = IntPtr.Zero;
            try
            {
                res = WlanEnumInterfaces(clientHandle, IntPtr.Zero, out pIfList);
                if (res != 0 || pIfList == IntPtr.Zero)
                {
                    return null;
                }

                WLAN_INTERFACE_INFO_LIST ifList = (WLAN_INTERFACE_INFO_LIST)Marshal.PtrToStructure(pIfList, typeof(WLAN_INTERFACE_INFO_LIST));
                if (ifList.dwNumberOfItems == 0)
                {
                    return null;
                }

                int itemSize = Marshal.SizeOf(typeof(WLAN_INTERFACE_INFO));
                IntPtr pItem = new IntPtr(pIfList.ToInt64() + 8); // Skip dwNumberOfItems (4) + dwIndex (4)

                for (int i = 0; i < ifList.dwNumberOfItems; i++)
                {
                    WLAN_INTERFACE_INFO info = (WLAN_INTERFACE_INFO)Marshal.PtrToStructure(pItem, typeof(WLAN_INTERFACE_INFO));

                    // wlan_interface_state_connected = 1
                    if (info.isState == 1)
                    {
                        uint dataSize = 0;
                        IntPtr pData = IntPtr.Zero;
                        res = WlanQueryInterface(clientHandle, ref info.InterfaceGuid, WLAN_INTF_OPCODE_CURRENT_CONNECTION, IntPtr.Zero, out dataSize, out pData, IntPtr.Zero);
                        if (res == 0 && pData != IntPtr.Zero)
                        {
                            try
                            {
                                WLAN_CONNECTION_ATTRIBUTES conn = (WLAN_CONNECTION_ATTRIBUTES)Marshal.PtrToStructure(pData, typeof(WLAN_CONNECTION_ATTRIBUTES));
                                string ssid = Encoding.UTF8.GetString(conn.wlanAssociationAttributes.dot11Ssid.ucSSID, 0, (int)conn.wlanAssociationAttributes.dot11Ssid.uSSIDLength);

                                if (string.IsNullOrEmpty(ssid))
                                {
                                    ssid = conn.strProfileName;
                                }

                                WifiInfo wifi = new WifiInfo();
                                wifi.Ssid = !string.IsNullOrEmpty(ssid) ? ssid : "当前热点/无线网络";
                                wifi.InterfaceName = "Wi-Fi";
                                wifi.InterfaceDescription = info.strInterfaceDescription;
                                wifi.IsConnected = true;
                                wifi.SignalQuality = (int)conn.wlanAssociationAttributes.wlanSignalQuality;

                                return wifi;
                            }
                            finally
                            {
                                WlanFreeMemory(pData);
                            }
                        }
                    }

                    pItem = new IntPtr(pItem.ToInt64() + itemSize);
                }
            }
            finally
            {
                if (pIfList != IntPtr.Zero) WlanFreeMemory(pIfList);
                WlanCloseHandle(clientHandle, IntPtr.Zero);
            }

            return null;
        }

        public static bool ToggleMeteredConnection(string profileName, bool enable)
        {
            if (string.IsNullOrEmpty(profileName)) return false;

            try
            {
                // Sanitize profileName against parameter injection and quotes
                string safeProfile = profileName.Replace("\"", "").Replace("&", "").Replace("|", "").Trim();
                if (string.IsNullOrEmpty(safeProfile)) return false;

                string cost = enable ? "Fixed" : "Unrestricted";
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "netsh";
                psi.Arguments = string.Format("wlan set profileparameter name=\"{0}\" cost={1}", safeProfile, cost);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(3000);
                    return p.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ToggleMeteredConnection error: " + ex.Message);
                return false;
            }
        }

        public static void OpenWifiSettings()
        {
            try
            {
                Process.Start("ms-settings:network-wifi");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("OpenWifiSettings error: " + ex.Message);
            }
        }
    }
}
