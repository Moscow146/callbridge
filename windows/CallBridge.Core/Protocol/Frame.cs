namespace CallBridge.Core.Protocol;

/// <summary>Frame type byte, see docs/PROTOCOL.md.</summary>
public enum FrameType : byte
{
	Hello = 0x01,
	HelloAck = 0x02,
	Downlink = 0x10,
	Uplink = 0x11,
	Control = 0x20,
	Ping = 0x30,
	Pong = 0x31,
}

public readonly record struct Frame(FrameType Type, byte[] Payload);
