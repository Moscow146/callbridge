using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace CallBridge.Core.Session;

/// <summary>Listens for the phone and keeps at most one active session.</summary>
public sealed class CallBridgeServer : IDisposable
{
	public const int DefaultPort = 47800;

	readonly TcpListener listener;
	readonly CancellationTokenSource cts = new();

	public string PairingCode { get; } = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

	public int Port { get; }

	public PhoneSession? Current { get; private set; }

	public event Action<PhoneSession>? PhoneConnected;

	public event Action? PhoneDisconnected;

	public CallBridgeServer(int port = DefaultPort)
	{
		listener = new TcpListener(IPAddress.Any, port);
		Port = port;
	}

	public void Start()
	{
		listener.Start();
		_ = AcceptLoopAsync();
	}

	async Task AcceptLoopAsync()
	{
		while (!cts.IsCancellationRequested)
		{
			TcpClient client;
			try
			{
				client = await listener.AcceptTcpClientAsync(cts.Token);
			}
			catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException)
			{
				return;
			}
			_ = ServeAsync(client);
		}
	}

	async Task ServeAsync(TcpClient client)
	{
		var session = new PhoneSession(client);
		try
		{
			using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
			timeout.CancelAfter(TimeSpan.FromSeconds(5));
			if (!await session.HandshakeAsync(PairingCode, timeout.Token))
			{
				session.Dispose();
				return;
			}
		}
		catch (Exception e) when (e is IOException or OperationCanceledException or EndOfStreamException or InvalidDataException or SocketException)
		{
			session.Dispose();
			return;
		}

		var previous = Current;
		Current = session;
		previous?.Dispose();
		session.Closed += () =>
		{
			if (Current == session)
			{
				Current = null;
				PhoneDisconnected?.Invoke();
			}
		};
		PhoneConnected?.Invoke(session);
		await session.RunAsync();
		session.Dispose();
	}

	public void Dispose()
	{
		cts.Cancel();
		listener.Stop();
		Current?.Dispose();
	}
}
