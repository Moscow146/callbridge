using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CallBridge.Core.Protocol;
using CallBridge.Core.Session;

namespace CallBridge.Core.Tests;

public class ContactPhotoTests
{
	[Theory]
	[InlineData("+7 (912) 345-67-89", "9123456789")]
	[InlineData("89123456789", "9123456789")]
	[InlineData("+1 555 0100", "15550100")]
	[InlineData("555-0100", "5550100")]
	public void NormalizesNumbersToTheirTail(string number, string expected) =>
		Assert.Equal(expected, PhotoKey.Normalize(number));

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("112")]
	[InlineData("*#")]
	public void RejectsNumbersTooShortToIdentifyAContact(string? number) =>
		Assert.Null(PhotoKey.Normalize(number));

	[Fact]
	public void ParsesPhotoReply()
	{
		var bytes = new byte[] { 1, 2, 3, 255 };
		using var doc = JsonDocument.Parse($$"""{"t":"photo","key":"123456","data":"{{Convert.ToBase64String(bytes)}}"}""");
		Assert.True(PhotoKey.TryParseReply(doc.RootElement, out var key, out var data));
		Assert.Equal("123456", key);
		Assert.Equal(bytes, data);
	}

	[Theory]
	[InlineData("""{"t":"photo","key":"123456","data":null}""")]
	[InlineData("""{"t":"photo","key":"123456"}""")]
	[InlineData("""{"t":"photo","key":"123456","data":"not base64!"}""")]
	[InlineData("""{"t":"photo","key":"123456","data":""}""")]
	public void ReplyWithoutUsablePhotoIsAnEmptyAnswer(string json)
	{
		using var doc = JsonDocument.Parse(json);
		Assert.True(PhotoKey.TryParseReply(doc.RootElement, out var key, out var data));
		Assert.Equal("123456", key);
		Assert.Null(data);
	}

	[Theory]
	[InlineData("""{"t":"photo","data":"AQID"}""")]
	[InlineData("""{"t":"photo","key":"","data":"AQID"}""")]
	[InlineData("""{"t":"photo","key":5,"data":"AQID"}""")]
	public void ReplyWithoutKeyIsIgnored(string json)
	{
		using var doc = JsonDocument.Parse(json);
		Assert.False(PhotoKey.TryParseReply(doc.RootElement, out _, out _));
	}

	[Fact]
	public void CacheRemembersPhotosAndMissesOnDisk()
	{
		var dir = Path.Combine(Path.GetTempPath(), "cb-photos-" + Guid.NewGuid().ToString("N"));
		try
		{
			var cache = new ContactPhotoCache(dir);
			Assert.False(cache.TryGet("111111", out _));
			cache.Put("111111", [9, 8, 7]);
			cache.Put("222222", null);

			var reopened = new ContactPhotoCache(dir);
			Assert.True(reopened.TryGet("111111", out var data));
			Assert.Equal(new byte[] { 9, 8, 7 }, data);
			Assert.False(reopened.TryGet("222222", out _));
			Assert.True(cache.TryGet("222222", out var missing));
			Assert.Null(missing);
		}
		finally
		{
			if (Directory.Exists(dir))
				Directory.Delete(dir, true);
		}
	}

	[Fact]
	public void CacheDropsOldestFilesOverTheLimit()
	{
		var dir = Path.Combine(Path.GetTempPath(), "cb-photos-" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(dir);
			for (var i = 0; i < 5; i++)
			{
				var path = Path.Combine(dir, $"{100000 + i}.jpg");
				File.WriteAllBytes(path, new byte[10]);
				File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-i));
			}

			_ = new ContactPhotoCache(dir, maxFiles: 3);

			var left = Directory.GetFiles(dir).Select(f => Path.GetFileNameWithoutExtension(f)!).Order().ToArray();
			Assert.Equal(["100000", "100001", "100002"], left);
		}
		finally
		{
			if (Directory.Exists(dir))
				Directory.Delete(dir, true);
		}
	}

	[Fact]
	public async Task SessionRequestsAndReceivesPhoto()
	{
		using var server = new CallBridgeServer("0123456789abcdef", 47817);
		server.Start();
		var sessionReady = new TaskCompletionSource<PhoneSession>();
		server.PhoneConnected += s => sessionReady.TrySetResult(s);

		using var client = new TcpClient();
		await client.ConnectAsync(IPAddress.Loopback, server.Port);
		var frames = new FrameStream(client.GetStream());
		await frames.WriteAsync(FrameType.Hello, Json.Bytes(new Hello(Json.ProtocolVersion, "test", server.PairingCode)));
		await frames.ReadAsync();
		var session = await sessionReady.Task.WaitAsync(TimeSpan.FromSeconds(5));

		var received = new TaskCompletionSource<(string, byte[]?)>();
		session.PhotoReceived += (key, data) => received.TrySetResult((key, data));

		await session.RequestPhotoAsync("9123456789", "+7 912 345-67-89");
		Frame? control;
		do
			control = await frames.ReadAsync();
		while (control!.Value.Type != FrameType.Control);
		using var request = JsonDocument.Parse(control.Value.Payload);
		Assert.Equal("photo", Json.TypeOf(request.RootElement));
		Assert.Equal("9123456789", request.RootElement.GetProperty("key").GetString());
		Assert.Equal("+7 912 345-67-89", request.RootElement.GetProperty("number").GetString());

		var reply = $$"""{"t":"photo","key":"9123456789","data":"{{Convert.ToBase64String([1, 2, 3])}}"}""";
		await frames.WriteAsync(FrameType.Control, Encoding.UTF8.GetBytes(reply));
		var (key, data) = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal("9123456789", key);
		Assert.Equal(new byte[] { 1, 2, 3 }, data);
	}
}
