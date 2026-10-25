using System.Net.Sockets;
using System.Text;

namespace CallBridge.Core.Session;

/// <summary>Answers the phone's broadcast "CALLBRIDGE?" so it can find this PC after its address changed.</summary>
public sealed class DiscoveryResponder : IDisposable
{
	public const int Port = 47801;
	public const string Query = "CALLBRIDGE?";

	readonly UdpClient udp;
	readonly byte[] reply;
	readonly CancellationTokenSource cts = new();

	public DiscoveryResponder(string name, int tcpPort, int port = Port)
	{
		udp = new UdpClient(port);
		reply = Encoding.UTF8.GetBytes($"CALLBRIDGE {tcpPort} {name}");
	}

	public void Start() => _ = RunAsync();

	async Task RunAsync()
	{
		try
		{
			while (!cts.IsCancellationRequested)
			{
				var received = await udp.ReceiveAsync(cts.Token);
				if (Encoding.UTF8.GetString(received.Buffer) == Query)
					await udp.SendAsync(reply, received.RemoteEndPoint, cts.Token);
			}
		}
		catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException)
		{
		}
	}

	public void Dispose()
	{
		cts.Cancel();
		udp.Dispose();
	}
}
