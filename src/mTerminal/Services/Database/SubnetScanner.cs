using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace mTerminal.Services.Database;

public static class SubnetScanner
{
    public struct SubnetInfo
    {
        public uint NetworkAddress;
        public uint SubnetMask;
    }

    public static List<IPAddress> GetLoopbackAddresses() => [IPAddress.Loopback];

    public static List<SubnetInfo> GetLocalSubnets()
    {
        var result = new List<SubnetInfo>();
        foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (iface.OperationalStatus != OperationalStatus.Up) continue;
            if (iface.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

            var props = iface.GetIPProperties();
            foreach (var addr in props.UnicastAddresses)
            {
                if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                byte[] bytes = addr.Address.GetAddressBytes();
                if (bytes[0] == 169 && bytes[1] == 254) continue;
                if (addr.IPv4Mask == null) continue;

                uint ip = IpToUint(addr.Address);
                uint mask = IpToUint(addr.IPv4Mask);
                if (mask >= 0xFFFFFFFE) continue;
                if (mask < 0xFFFFFF00) mask = 0xFFFFFF00;

                result.Add(new SubnetInfo { NetworkAddress = ip & mask, SubnetMask = mask });
            }
        }
        return result;
    }

    public static List<IPAddress> GetAddressesInSubnet(SubnetInfo subnet)
    {
        var result = new List<IPAddress>();
        uint broadcast = subnet.NetworkAddress | ~subnet.SubnetMask;
        for (uint addr = subnet.NetworkAddress + 1; addr < broadcast; addr++)
            result.Add(UintToIp(addr));
        return result;
    }

    public static IPAddress GetBroadcastAddress(SubnetInfo subnet) =>
        UintToIp(subnet.NetworkAddress | ~subnet.SubnetMask);

    public static bool ScanPort(IPAddress ip, int port, int timeoutMs)
    {
        try
        {
            using var client = new TcpClient();
            var ar = client.BeginConnect(ip, port, null, null);
            bool connected = ar.AsyncWaitHandle.WaitOne(timeoutMs);
            if (connected)
            {
                try { client.EndConnect(ar); } catch { return false; }
                return client.Connected;
            }
            return false;
        }
        catch { return false; }
    }

    private static uint IpToUint(IPAddress ip)
    {
        byte[] b = ip.GetAddressBytes();
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }

    private static IPAddress UintToIp(uint addr) =>
        new([
            (byte)(addr >> 24), (byte)(addr >> 16),
            (byte)(addr >> 8), (byte)addr
        ]);
}
