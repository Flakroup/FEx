using FEx.Agnostics.Abstractions.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public static Dictionary<NetworkInterfaceType, HashSet<string>> GetAllLocalIPv4(bool omitLoopbacks = true)
    {
        var dictionary = new Dictionary<NetworkInterfaceType, HashSet<string>>();

        foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (item.OperationalStatus == OperationalStatus.Up
                && (!omitLoopbacks || item.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                && item.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            {
                var ipAddresses = item.GetIPProperties()
                    .UnicastAddresses.Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(x => x.Address.ToString())
                    .ToList();

                if (!dictionary.ContainsKey(item.NetworkInterfaceType))
                    dictionary.Add(item.NetworkInterfaceType, [.. ipAddresses]);
                else
                    foreach (var ip in ipAddresses)
                        dictionary[item.NetworkInterfaceType].Add(ip);
            }
        }

        return dictionary;
    }

    /// <summary>
    /// Creates network credentials from username and password, or returns default credentials if not provided.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="password">The password for authentication.</param>
    /// <returns>Network credentials or default credentials.</returns>
    public static ICredentials GetCredentials(string username = "", string password = "") =>
        username.IsNotNullOrWhiteSpace() && password.IsNotNullOrWhiteSpace()
            ? new(username, password)
            : CredentialCache.DefaultNetworkCredentials;
}