using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace envcontrol.module
{
    public class WifiNetworkInfo
    {
        public string Ssid { get; set; } = string.Empty;
        public string Bssid { get; set; } = string.Empty;
        public int SignalQuality { get; set; }      // 0 a 100%
        public int Rssi { get; set; }               // Aproximado em dBm
        public string Authentication { get; set; } = string.Empty;
        public string Encryption { get; set; } = string.Empty;
        public bool IsSecurityEnabled { get; set; }
        public bool IsConnected { get; set; }
        public string ProfileName { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{Ssid} ({SignalQuality}%)";
        }
    }

    public static class WifiScanner
    {
        private const uint WLAN_CLIENT_VERSION_2 = 2;
        private const uint WLAN_AVAILABLE_NETWORK_INCLUDE_ALL_ADHOC_PROFILES = 0x00000001;
        private const uint WLAN_AVAILABLE_NETWORK_INCLUDE_ALL_MANUAL_HIDDEN_PROFILES = 0x00000002;
        private const uint ERROR_SUCCESS = 0;

        public static List<WifiNetworkInfo> GetAvailableNetworks()
        {
            var networks = new List<WifiNetworkInfo>();
            IntPtr clientHandle = IntPtr.Zero;
            IntPtr interfaceListPtr = IntPtr.Zero;

            try
            {
                uint result = WlanOpenHandle(WLAN_CLIENT_VERSION_2, IntPtr.Zero, out _, out clientHandle);
                if (result != ERROR_SUCCESS || clientHandle == IntPtr.Zero)
                    return networks;

                result = WlanEnumInterfaces(clientHandle, IntPtr.Zero, out interfaceListPtr);
                if (result != ERROR_SUCCESS || interfaceListPtr == IntPtr.Zero)
                    return networks;

                var interfaceList = Marshal.PtrToStructure<WLAN_INTERFACE_INFO_LIST>(interfaceListPtr);
                int ifaceSize = Marshal.SizeOf<WLAN_INTERFACE_INFO>();
                IntPtr currentIfacePtr = new IntPtr(interfaceListPtr.ToInt64() + Marshal.OffsetOf<WLAN_INTERFACE_INFO_LIST>("InterfaceInfo").ToInt64());

                for (int i = 0; i < interfaceList.dwNumberOfItems; i++)
                {
                    var iface = Marshal.PtrToStructure<WLAN_INTERFACE_INFO>(currentIfacePtr);

                    // Solicita um novo scan antes de ler a lista disponível
                    WlanScan(clientHandle, ref iface.InterfaceGuid, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                    IntPtr availableListPtr = IntPtr.Zero;
                    result = WlanGetAvailableNetworkList(
                        clientHandle,
                        ref iface.InterfaceGuid,
                        WLAN_AVAILABLE_NETWORK_INCLUDE_ALL_ADHOC_PROFILES | WLAN_AVAILABLE_NETWORK_INCLUDE_ALL_MANUAL_HIDDEN_PROFILES,
                        IntPtr.Zero,
                        out availableListPtr);

                    if (result == ERROR_SUCCESS && availableListPtr != IntPtr.Zero)
                    {
                        var availableList = Marshal.PtrToStructure<WLAN_AVAILABLE_NETWORK_LIST>(availableListPtr);
                        int netSize = Marshal.SizeOf<WLAN_AVAILABLE_NETWORK>();
                        IntPtr currentNetPtr = new IntPtr(availableListPtr.ToInt64() + Marshal.OffsetOf<WLAN_AVAILABLE_NETWORK_LIST>("Network").ToInt64());

                        for (int j = 0; j < availableList.dwNumberOfItems; j++)
                        {
                            var net = Marshal.PtrToStructure<WLAN_AVAILABLE_NETWORK>(currentNetPtr);

                            string ssid = DecodeSsid(net.dot11Ssid);
                            bool isConnected = (net.dwFlags & WLAN_AVAILABLE_NETWORK_CONNECTED) != 0;
                            bool isHidden = !net.bNetworkConnectable && string.IsNullOrEmpty(ssid);

                            if (!string.IsNullOrWhiteSpace(ssid) || !isHidden)
                            {
                                var existing = networks.FirstOrDefault(n => n.Ssid.Equals(ssid, StringComparison.OrdinalIgnoreCase));

                                var info = new WifiNetworkInfo
                                {
                                    Ssid = string.IsNullOrWhiteSpace(ssid) ? "<Rede Oculta>" : ssid,
                                    SignalQuality = (int)net.wlanSignalQuality,
                                    Rssi = SignalQualityToRssi((int)net.wlanSignalQuality),
                                    Authentication = net.dot11DefaultAuthAlgorithm.ToString(),
                                    Encryption = net.dot11DefaultCipherAlgorithm.ToString(),
                                    IsSecurityEnabled = net.bSecurityEnabled,
                                    IsConnected = isConnected,
                                    ProfileName = net.strProfileName ?? string.Empty
                                };

                                if (existing == null)
                                {
                                    networks.Add(info);
                                }
                                else if (info.SignalQuality > existing.SignalQuality || info.IsConnected)
                                {
                                    networks.Remove(existing);
                                    networks.Add(info);
                                }
                            }

                            currentNetPtr = new IntPtr(currentNetPtr.ToInt64() + netSize);
                        }

                        WlanFreeMemory(availableListPtr);
                    }

                    currentIfacePtr = new IntPtr(currentIfacePtr.ToInt64() + ifaceSize);
                }
            }
            catch
            {
                // Em caso de falha na API nativa, retorna o que foi coletado até o momento
            }
            finally
            {
                if (interfaceListPtr != IntPtr.Zero)
                    WlanFreeMemory(interfaceListPtr);

                if (clientHandle != IntPtr.Zero)
                    WlanCloseHandle(clientHandle, IntPtr.Zero);
            }

            return networks
                .OrderByDescending(n => n.IsConnected)
                .ThenByDescending(n => n.SignalQuality)
                .ToList();
        }

        private static string DecodeSsid(DOT11_SSID dot11Ssid)
        {
            if (dot11Ssid.uSSIDLength == 0 || dot11Ssid.ucSSID == null)
                return string.Empty;

            int length = (int)Math.Min(dot11Ssid.uSSIDLength, 32);
            return Encoding.UTF8.GetString(dot11Ssid.ucSSID, 0, length).TrimEnd('\0');
        }

        private static int SignalQualityToRssi(int quality)
        {
            // Conversão aproximada da qualidade (0-100%) para dBm (-100 a -50 dBm)
            if (quality <= 0) return -100;
            if (quality >= 100) return -50;
            return (quality / 2) - 100;
        }

        #region Native Wi-Fi (wlanapi.dll)

        private const uint WLAN_AVAILABLE_NETWORK_CONNECTED = 0x00000001;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_INTERFACE_INFO
        {
            public Guid InterfaceGuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strInterfaceDescription;
            public WLAN_INTERFACE_STATE isState;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WLAN_INTERFACE_INFO_LIST
        {
            public uint dwNumberOfItems;
            public uint dwIndex;
            public WLAN_INTERFACE_INFO InterfaceInfo;
        }

        private enum WLAN_INTERFACE_STATE
        {
            wlan_interface_state_not_ready = 0,
            wlan_interface_state_connected = 1,
            wlan_interface_state_ad_hoc_network_formed = 2,
            wlan_interface_state_disconnecting = 3,
            wlan_interface_state_disconnected = 4,
            wlan_interface_state_associating = 5,
            wlan_interface_state_discovering = 6,
            wlan_interface_state_authenticating = 7
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DOT11_SSID
        {
            public uint uSSIDLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] ucSSID;
        }

        private enum DOT11_AUTH_ALGORITHM
        {
            DOT11_AUTH_ALGO_80211_OPEN = 1,
            DOT11_AUTH_ALGO_80211_SHARED_KEY = 2,
            DOT11_AUTH_ALGO_WPA = 3,
            DOT11_AUTH_ALGO_WPA_PSK = 4,
            DOT11_AUTH_ALGO_WPA_NONE = 5,
            DOT11_AUTH_ALGO_RSNA = 6,
            DOT11_AUTH_ALGO_RSNA_PSK = 7,
            DOT11_AUTH_ALGO_WPA3 = 8,
            DOT11_AUTH_ALGO_WPA3_SAE = 9,
            DOT11_AUTH_ALGO_IHV_START = unchecked((int)0x80000000),
            DOT11_AUTH_ALGO_IHV_END = unchecked((int)0xffffffff)
        }

        private enum DOT11_CIPHER_ALGORITHM
        {
            DOT11_CIPHER_ALGO_NONE = 0x00,
            DOT11_CIPHER_ALGO_WEP40 = 0x01,
            DOT11_CIPHER_ALGO_TKIP = 0x02,
            DOT11_CIPHER_ALGO_CCMP = 0x04,
            DOT11_CIPHER_ALGO_WEP104 = 0x05,
            DOT11_CIPHER_ALGO_WPA_USE_GROUP = 0x100,
            DOT11_CIPHER_ALGO_RSN_USE_GROUP = 0x100,
            DOT11_CIPHER_ALGO_WEP = 0x101,
            DOT11_CIPHER_ALGO_IHV_START = unchecked((int)0x80000000),
            DOT11_CIPHER_ALGO_IHV_END = unchecked((int)0xffffffff)
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_AVAILABLE_NETWORK
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strProfileName;
            public DOT11_SSID dot11Ssid;
            public DOT11_BSS_TYPE dot11BssType;
            public uint uNumberOfBssids;
            public bool bNetworkConnectable;
            public uint wlanNotConnectableReason;
            public uint uNumberOfPhyTypes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public DOT11_PHY_TYPE[] dot11PhyTypes;
            public bool bMorePhyTypes;
            public uint wlanSignalQuality;
            public bool bSecurityEnabled;
            public DOT11_AUTH_ALGORITHM dot11DefaultAuthAlgorithm;
            public DOT11_CIPHER_ALGORITHM dot11DefaultCipherAlgorithm;
            public uint dwFlags;
            public uint dwReserved;
        }

        private enum DOT11_BSS_TYPE
        {
            dot11_BSS_type_infrastructure = 1,
            dot11_BSS_type_independent = 2,
            dot11_BSS_type_any = 3
        }

        private enum DOT11_PHY_TYPE
        {
            dot11_phy_type_unknown = 0,
            dot11_phy_type_any = 0,
            dot11_phy_type_fhss = 1,
            dot11_phy_type_dsss = 2,
            dot11_phy_type_irbaseband = 3,
            dot11_phy_type_ofdm = 4,
            dot11_phy_type_hrdsss = 5,
            dot11_phy_type_erp = 6,
            dot11_phy_type_ht = 7,
            dot11_phy_type_vht = 8,
            dot11_phy_type_dmg = 9,
            dot11_phy_type_he = 10,
            dot11_phy_type_eht = 11
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WLAN_AVAILABLE_NETWORK_LIST
        {
            public uint dwNumberOfItems;
            public uint dwIndex;
            public WLAN_AVAILABLE_NETWORK Network;
        }

        [DllImport("wlanapi.dll")]
        private static extern uint WlanOpenHandle(
            uint dwClientVersion,
            IntPtr pReserved,
            out uint pdwNegotiatedVersion,
            out IntPtr phClientHandle);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanCloseHandle(
            IntPtr hClientHandle,
            IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanEnumInterfaces(
            IntPtr hClientHandle,
            IntPtr pReserved,
            out IntPtr ppInterfaceList);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanScan(
            IntPtr hClientHandle,
            ref Guid pInterfaceGuid,
            IntPtr pDot11Ssid,
            IntPtr pIeData,
            IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanGetAvailableNetworkList(
            IntPtr hClientHandle,
            ref Guid pInterfaceGuid,
            uint dwFlags,
            IntPtr pReserved,
            out IntPtr ppAvailableNetworkList);

        [DllImport("wlanapi.dll")]
        private static extern void WlanFreeMemory(IntPtr pMemory);

        #endregion
    }
}