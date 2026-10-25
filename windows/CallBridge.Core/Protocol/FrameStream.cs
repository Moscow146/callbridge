using System.Buffers.Binary;

namespace CallBridge.Core.Protocol;

/// <summary>Reads and writes length-prefixed frames on a stream.</summary>
public sealed class FrameStream(Stream stream)
{
	public const int MaxPayload = 1 << 20;

	readonly SemaphoreSlim writeLock = new(1, 1);

	public async Task WriteAsync(FrameType type, ReadOnlyMemory<byte> payload, CancellationToken ct = default)
	{
		if (payload.Length > MaxPayload)
			throw new ArgumentException("Payload too large", nameof(payload));
		var buffer = new byte[5 + payload.Length];
		buffer[0] = (byte)type;
		BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(1), payload.Length);
		payload.Span.CopyTo(buffer.AsSpan(5));
		await writeLock.WaitAsync(ct);
		try
		{
			await stream.WriteAsync(buffer, ct);
			await stream.FlushAsync(ct);
		}
		finally
		{
			writeLock.Release();
		}
	}

	/// <summary>Returns null when the peer closed the stream between frames.</summary>
	public async Task<Frame?> ReadAsync(CancellationToken ct = default)
	{
		var header = new byte[5];
		if (!await ReadExactAsync(header, ct))
			return null;
		var length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(1));
		if (length < 0 || length > MaxPayload)
			throw new InvalidDataException($"Bad frame length {length}");
		var payload = new byte[length];
		if (length > 0 && !await ReadExactAsync(payload, ct))
			throw new EndOfStreamException();
		return new Frame((FrameType)header[0], payload);
	}

	async Task<bool> ReadExactAsync(byte[] buffer, CancellationToken ct)
	{
		var read = 0;
		while (read < buffer.Length)
		{
			var n = await stream.ReadAsync(buffer.AsMemory(read), ct);
			if (n == 0)
				return read == 0 ? false : throw new EndOfStreamException();
			read += n;
		}
		return true;
	}
}
