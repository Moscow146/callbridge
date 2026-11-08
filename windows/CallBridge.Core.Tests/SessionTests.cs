using System.Net;
using System.Net.Sockets;
using CallBridge.Core.Protocol;
using CallBridge.Core.Session;

namespace CallBridge.Core.Tests;

public class SessionTests
{
	static async Task<HelloAck?> Connect(CallBridgeServer server, string token)
	{
		using var client = new TcpClient();
		await client.ConnectAsync(IPAddress.Loopback, server.Port);
		var frames = new FrameStream(client.GetStream());
		await frames.WriteAsync(FrameType.Hello, Json.Bytes(new Hello(Json.ProtocolVersion, "test", token)));
		var reply = await frames.ReadAsync();
		return Json.Parse<HelloAck>(reply!.Value.Payload);
	}

	[Fact]
	public async Task AcceptsCorrectCode()
	{
		using var server = new CallBridgeServer("0123456789abcdef", 47811);
		server.Start();
		var connected = new TaskCompletionSource();
		server.PhoneConnected += _ => connected.TrySetResult();
		var ack = await Connect(server, server.PairingCode);
		Assert.True(ack!.Ok);
		await connected.Task.WaitAsync(TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task ReceivesContacts()
	{
		using var server = new CallBridgeServer("0123456789abcdef", 47813);
		server.Start();
		var received = new TaskCompletionSource<IReadOnlyList<Contact>>();
		server.PhoneConnected += s => s.ContactsChanged += list => received.TrySetResult(list);

		using var client = new TcpClient();
		await client.ConnectAsync(IPAddress.Loopback, server.Port);
		var frames = new FrameStream(client.GetStream());
		await frames.WriteAsync(FrameType.Hello, Json.Bytes(new Hello(Json.ProtocolVersion, "test", server.PairingCode)));
		await frames.ReadAsync();
		await Task.Delay(200);
		var message = """{"t":"contacts","items":[{"name":"Anna Smith","number":"+1 555 0100"}]}""";
		await frames.WriteAsync(FrameType.Control, System.Text.Encoding.UTF8.GetBytes(message));

		var list = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(new Contact("Anna Smith", "+1 555 0100"), Assert.Single(list));
	}

	[Fact]
	public async Task MeasuresRoundTripFromPong()
	{
		using var server = new CallBridgeServer("0123456789abcdef", 47815);
		server.Start();
		var session = new TaskCompletionSource<PhoneSession>();
		server.PhoneConnected += s => session.TrySetResult(s);

		using var client = new TcpClient();
		await client.ConnectAsync(IPAddress.Loopback, server.Port);
		var frames = new FrameStream(client.GetStream());
		await frames.WriteAsync(FrameType.Hello, Json.Bytes(new Hello(Json.ProtocolVersion, "test", server.PairingCode)));
		await frames.ReadAsync();
		var phone = await session.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Frame? ping;
		do
			ping = await frames.ReadAsync();
		while (ping is { Type: not FrameType.Ping });
		await frames.WriteAsync(FrameType.Pong, ping!.Value.Payload);

		for (var i = 0; i < 50 && phone.Metrics.RoundTripSamples == 0; i++)
			await Task.Delay(100);
		Assert.Equal(1, phone.Metrics.RoundTripSamples);
		Assert.InRange(phone.Metrics.RoundTripMs, 0, 2000);
	}

	[Fact]
	public async Task ExchangesCallMode()
	{
		using var server = new CallBridgeServer("0123456789abcdef", 47814);
		server.Start();
		var session = new TaskCompletionSource<PhoneSession>();
		var received = new TaskCompletionSource<CallMode>();
		server.PhoneConnected += s =>
		{
			s.CallModeChanged += mode => received.TrySetResult(mode);
			session.TrySetResult(s);
		};

		using var client = new TcpClient();
		await client.ConnectAsync(IPAddress.Loopback, server.Port);
		var frames = new FrameStream(client.GetStream());
		await frames.WriteAsync(FrameType.Hello, Json.Bytes(new Hello(Json.ProtocolVersion, "test", server.PairingCode)));
		await frames.ReadAsync();
		var phone = await session.Task.WaitAsync(TimeSpan.FromSeconds(5));

		await frames.WriteAsync(FrameType.Control, System.Text.Encoding.UTF8.GetBytes("""{"t":"mode","mode":"phone"}"""));
		Assert.Equal(CallMode.Phone, await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));

		await phone.SetCallModeAsync(CallMode.Pc);
		Frame? request;
		do
			request = await frames.ReadAsync().WaitAsync(TimeSpan.FromSeconds(5));
		while (request is { Type: FrameType.Ping });
		Assert.Equal("""{"t":"mode","mode":"pc"}""", System.Text.Encoding.UTF8.GetString(request!.Value.Payload));
	}

	[Fact]
	public async Task SendsHoldAndDtmf()
	{
		using var server = new CallBridgeServer("0123456789abcdef", 47815);
		server.Start();
		var session = new TaskCompletionSource<PhoneSession>();
		server.PhoneConnected += s => session.TrySetResult(s);

		using var client = new TcpClient();
		await client.ConnectAsync(IPAddress.Loopback, server.Port);
		var frames = new FrameStream(client.GetStream());
		await frames.WriteAsync(FrameType.Hello, Json.Bytes(new Hello(Json.ProtocolVersion, "test", server.PairingCode)));
		await frames.ReadAsync();
		var phone = await session.Task.WaitAsync(TimeSpan.FromSeconds(5));

		async Task<string> Next()
		{
			Frame? frame;
			do
				frame = await frames.ReadAsync().WaitAsync(TimeSpan.FromSeconds(5));
			while (frame is { Type: FrameType.Ping });
			return System.Text.Encoding.UTF8.GetString(frame!.Value.Payload);
		}

		await phone.SetHoldAsync(true);
		Assert.Equal("""{"t":"hold","on":true}""", await Next());
		await phone.SetHoldAsync(false);
		Assert.Equal("""{"t":"hold","on":false}""", await Next());
		await phone.SendDtmfAsync('5');
		Assert.Equal("""{"t":"dtmf","digit":"5"}""", await Next());
		await phone.SendDtmfAsync('#');
		Assert.Equal("""{"t":"dtmf","digit":"#"}""", await Next());
		await phone.SendDtmfAsync('x');
		await phone.SendDtmfAsync('*');
		Assert.Equal("""{"t":"dtmf","digit":"*"}""", await Next());
	}

	[Fact]
	public void ValidatesDtmfSymbols()
	{
		foreach (var c in "0123456789*#abcdABCD")
			Assert.True(Dtmf.IsValid(c));
		foreach (var c in "eE+ -x")
			Assert.False(Dtmf.IsValid(c));
	}

	[Fact]
	public async Task ReceivesHeldState()
	{
		using var server = new CallBridgeServer("0123456789abcdef", 47816);
		server.Start();
		var received = new TaskCompletionSource<PhoneState>();
		server.PhoneConnected += s => s.StateChanged += state => received.TrySetResult(state);

		using var client = new TcpClient();
		await client.ConnectAsync(IPAddress.Loopback, server.Port);
		var frames = new FrameStream(client.GetStream());
		await frames.WriteAsync(FrameType.Hello, Json.Bytes(new Hello(Json.ProtocolVersion, "test", server.PairingCode)));
		await frames.ReadAsync();
		await Task.Delay(200);
		await frames.WriteAsync(FrameType.Control, System.Text.Encoding.UTF8.GetBytes("""{"t":"state","call":"held","number":"+1 555 0100"}"""));

		var state = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(CallState.Held, state.Call);
	}

	[Fact]
	public async Task ReceivesHoldFailed()
	{
		using var server = new CallBridgeServer("0123456789abcdef", 47822);
		server.Start();
		var received = new TaskCompletionSource<bool>();
		server.PhoneConnected += s => s.HoldFailed += () => received.TrySetResult(true);

		using var client = new TcpClient();
		await client.ConnectAsync(IPAddress.Loopback, server.Port);
		var frames = new FrameStream(client.GetStream());
		await frames.WriteAsync(FrameType.Hello, Json.Bytes(new Hello(Json.ProtocolVersion, "test", server.PairingCode)));
		await frames.ReadAsync();
		await Task.Delay(200);
		await frames.WriteAsync(FrameType.Control, System.Text.Encoding.UTF8.GetBytes("""{"t":"holdFailed"}"""));

		Assert.True(await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
	}

	[Fact]
	public async Task AnswersDiscoveryQuery()
	{
		using var responder = new DiscoveryResponder("MY-PC", 47800, 47821);
		responder.Start();
		using var client = new UdpClient();
		var query = System.Text.Encoding.UTF8.GetBytes(DiscoveryResponder.Query);
		await client.SendAsync(query, new IPEndPoint(IPAddress.Loopback, 47821));
		var answer = await client.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal("CALLBRIDGE 47800 MY-PC", System.Text.Encoding.UTF8.GetString(answer.Buffer));
	}

	[Fact]
	public async Task RejectsWrongCode()
	{
		using var server = new CallBridgeServer("0123456789abcdef", 47812);
		server.Start();
		var ack = await Connect(server, "not-the-code");
		Assert.False(ack!.Ok);
		Assert.Equal("wrong code", ack.Error);
	}
}
