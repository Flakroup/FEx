using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace FEx.Webx.Utilities;

/// <summary>
/// Network-related utilities.
/// </summary>
public static class NetworkUtilities
{
    /// <summary>
    /// Gets the local ip addresses.
    /// </summary>
    /// <returns></returns>
    public static Dictionary<NetworkInterfaceType, HashSet<string>> GetAllLocalIPv4(bool ommitLoopbacks = true)
    {
        var dictionary = new Dictionary<NetworkInterfaceType, HashSet<string>>();

        foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (item.OperationalStatus == OperationalStatus.Up
                && (!ommitLoopbacks || item.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                && item.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            {
                var ipAddresses = item.GetIPProperties()
                    .UnicastAddresses.Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(x => x.Address.ToString())
                    .ToList();

                if (!dictionary.ContainsKey(item.NetworkInterfaceType))
                    dictionary.Add(item.NetworkInterfaceType, [.. ipAddresses]);
                else
                    foreach (string ip in ipAddresses)
                        dictionary[item.NetworkInterfaceType].Add(ip);
            }
        }

        return dictionary;
    }
}