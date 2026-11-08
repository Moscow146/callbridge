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
| 0x11  | Uplink     | PC→phone  | 960 little-endian int16 samples, PC microphone to the remote side |
| 0x20  | Control    | both      | JSON, see below                |
| 0x30  | Ping       | both      | 8 bytes, sender timestamp      |
| 0x31  | Pong       | both      | the same 8 bytes               |

## Audio

48 kHz, mono, 20 ms frames.

Downlink (caller to PC) is Opus, VoIP application, 32 kbit/s, one frame per packet. Uplink (PC microphone to caller) is raw 16-bit PCM, because the phone only has to hand it to the telephony transmit path and the link is a LAN or USB.

## Control messages

JSON object with a `t` field.

Phone→PC:

- `{"t":"state","call":"idle|ringing|dialing|active|held","number":"+7...","name":"...","stage":"connecting|dialing"}`: `stage` is optional and only sent with `dialing`: `connecting` while the call is being set up, `dialing` once the network is placing it (the other side rings). `active` is sent only after the call is answered. Older PC apps ignore `stage`. The number also comes for outgoing calls, taken from the call itself
- `{"t":"audio","route":"ok|unavailable","reason":"..."}`
- `{"t":"contacts","items":[{"name":"...","number":"..."}]}`: sent on connect, when contacts change, when the contacts permission is granted, and when the PC asks for it
- `{"t":"photo","key":"9123456789","data":"<base64 JPEG>"|null}`: the answer to a `photo` request. `data` is a contact photo thumbnail, about 96x96, JPEG, at most 8 KiB before base64, or `null` when the number has no contact photo. `key` is repeated from the request. Photos are never part of `contacts`, so the list stays small. Older PC apps never ask and ignore the message
- `{"t":"mode","mode":"pc|phone"}`: where the call audio is now. The phone owns this value and sends it on connect and on every change

PC→phone:

- `{"t":"answer"}`
- `{"t":"hangup"}`
- `{"t":"reject"}`
- `{"t":"dial","number":"+7..."}`
- `{"t":"hold","on":true|false}`: puts the current call on hold or takes it off hold. The phone confirms with a `state` message, `held` while on hold and `active` after
- `{"t":"dtmf","digit":"5"}`: plays one DTMF tone into the call, `digit` is one character of `0-9 * # A-D`. The phone ignores anything else and does nothing when no call is active
- `{"t":"contacts"}`: asks the phone to send its contacts again
- `{"t":"photo","key":"9123456789","number":"+7 912 345-67-89"}`: asks for the photo of the contact with this number. `key` is the number reduced to its digits, keeping the last 10 (a number with fewer than 4 digits is never asked for), and is what the PC caches by. `number` is the number as the phone sent it, which the phone looks up with the same matching it uses for caller names; when `number` is missing the phone uses `key`. The PC asks only for the contacts it is about to show (the current call and the dial suggestions), once per connection per key. Older phone apps ignore the command
- `{"t":"mode","mode":"pc|phone"}`: asks the phone to move the current call. `pc` silences the phone's speaker and microphone and streams the call, `phone` is an ordinary call with no streaming. Every new call starts in `pc`, and the phone falls back to ordinary audio when the connection is lost

## Pairing

The PC shows a QR code with the URI

```
callbridge://pair?h=127.0.0.1,192.168.1.5&p=47800&c=<16 hex chars>&n=<PC name>
```

`h` is a comma-separated list of PC addresses, the phone tries them in order (1.5 s each). `c` is the pairing token, the phone sends it in `Hello.token`. A wrong token gets `HelloAck.ok=false, error="wrong code"` and the connection is closed. The token is stored on the PC and survives restarts until the user presses "New code".
