# CallBridge protocol v1

One TCP connection. The PC listens (default port `47800`), the phone connects. Over USB the phone reaches the PC through `adb reverse tcp:47800 tcp:47800`.

All integers are little-endian.

## Frame

| Field   | Size | Meaning                  |
|---------|------|--------------------------|
| type    | 1    | `FrameType`              |
| length  | 4    | payload length in bytes  |
| payload | n    | depends on type          |

Max payload: 1 MiB. A larger frame closes the connection.

## Frame types

| Value | Name       | Direction | Payload                       |
|-------|------------|-----------|-------------------------------|
| 0x01  | Hello      | phone→PC  | JSON `{version, device, token}` |
| 0x02  | HelloAck   | PC→phone  | JSON `{version, ok, error?}`   |
| 0x10  | Downlink   | phone→PC  | one Opus packet, call audio from the remote side |
| 0x11  | Uplink     | PC→phone  | one Opus packet, PC microphone to the remote side |
| 0x20  | Control    | both      | JSON, see below                |
| 0x30  | Ping       | both      | 8 bytes, sender timestamp      |
| 0x31  | Pong       | both      | the same 8 bytes               |

## Audio

Opus, 48 kHz, mono, 20 ms frames, VoIP application, in-band FEC on, 32 kbit/s. One frame is one packet.

## Control messages

JSON object with a `t` field.

Phone→PC:

- `{"t":"state","call":"idle|ringing|dialing|active|held","number":"+7...","name":"..."}`
- `{"t":"audio","route":"ok|unavailable","reason":"..."}`

PC→phone:

- `{"t":"answer"}`
- `{"t":"hangup"}`
- `{"t":"reject"}`
- `{"t":"dial","number":"+7..."}`

## Pairing

The PC shows a QR code with the URI

```
callbridge://pair?h=127.0.0.1,192.168.1.5&p=47800&c=<16 hex chars>&n=<PC name>
```

`h` is a comma-separated list of PC addresses, the phone tries them in order (1.5 s each). `c` is the pairing token, the phone sends it in `Hello.token`. A wrong token gets `HelloAck.ok=false, error="wrong code"` and the connection is closed. The token is stored on the PC and survives restarts until the user presses "New code".
