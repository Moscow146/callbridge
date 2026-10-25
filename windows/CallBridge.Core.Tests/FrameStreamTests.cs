using CallBridge.Core.Protocol;

namespace CallBridge.Core.Tests;

public class FrameStreamTests
{
	[Fact]
	public async Task RoundTrip()
	{
		var ms = new MemoryStream();
		var writer = new FrameStream(ms);
		await writer.WriteAsync(FrameType.Downlink, new byte[] { 1, 2, 3 });
		await writer.WriteAsync(FrameType.Control, ReadOnlyMemory<byte>.Empty);
		ms.Position = 0;
		var reader = new FrameStream(ms);

		var a = await reader.ReadAsync();
		Assert.Equal(FrameType.Downlink, a!.Value.Type);
		Assert.Equal(new byte[] { 1, 2, 3 }, a.Value.Payload);
		var b = await reader.ReadAsync();
		Assert.Equal(FrameType.Control, b!.Value.Type);
		Assert.Empty(b.Value.Payload);
		Assert.Null(await reader.ReadAsync());
	}

	[Fact]
	public async Task RejectsOversizedFrame()
	{
		var ms = new MemoryStream(new byte[] { 0x10, 0xFF, 0xFF, 0xFF, 0x7F });
		await Assert.ThrowsAsync<InvalidDataException>(() => new FrameStream(ms).ReadAsync());
	}

	[Fact]
	public async Task TruncatedPayloadThrows()
	{
		var ms = new MemoryStream(new byte[] { 0x10, 0x05, 0, 0, 0, 1, 2 });
		await Assert.ThrowsAsync<EndOfStreamException>(() => new FrameStream(ms).ReadAsync());
	}
}
