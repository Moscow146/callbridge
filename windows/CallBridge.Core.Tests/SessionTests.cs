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
	public async Task RejectsWrongCode()
	{
		using var server = new CallBridgeServer("0123456789abcdef", 47812);
		server.Start();
		var ack = await Connect(server, "not-the-code");
		Assert.False(ack!.Ok);
		Assert.Equal("wrong code", ack.Error);
	}
}
