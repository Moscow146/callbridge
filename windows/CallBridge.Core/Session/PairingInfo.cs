using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CallBridge.Core.Session;

/// <summary>What the phone needs to reach this PC, carried by the pairing QR code.</summary>
public static class PairingInfo
{
	public static IReadOnlyList<string> LocalAddresses() =>
		NetworkInterface.GetAllNetworkInterfaces()
			.Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
			.SelectMany(n => n.GetIPProperties().UnicastAddresses)
			.Select(a => a.Address)
			.Where(a => a.AddressFamily == AddressFamily.InterNetwork && !IsLinkLocal(a))
			.Select(a => a.ToString())
			.OrderBy(a => a.StartsWith("192.168.") ? 0 : a.StartsWith("10.") ? 1 : 2)
			.ToList();

	static bool IsLinkLocal(IPAddress a)
	{
		var b = a.GetAddressBytes();
		return b[0] == 169 && b[1] == 254;
	}

	/// <summary>callbridge://pair?h=a,b&amp;p=47800&amp;c=token&amp;n=name</summary>
	public static string BuildUri(string code, int port, IEnumerable<string> hosts, string name) =>
		$"callbridge://pair?h={string.Join(',', hosts)}&p={port}&c={code}&n={Uri.EscapeDataString(name)}";
}
