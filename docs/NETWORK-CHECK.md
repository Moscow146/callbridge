# Checking calls over Wi-Fi with a VPN on

This is the manual check for issue #12. It needs the real phone, the PC and a VPN app on the phone, so it is not covered by the automated tests.

## How the phone picks the network

The phone looks for a Wi-Fi network that is not a VPN and binds the bridge socket to it, so the traffic to the PC stays on the LAN and does not go through the tunnel. Addresses starting with `127.` (USB with `adb reverse`) are never bound. If the connection drops, the phone reconnects after 3 seconds and picks the Wi-Fi network again, and searches for the PC by broadcast when the addresses from the QR code no longer answer. Binding to a network the VPN excludes needs `CONNECTIVITY_USE_RESTRICTED_NETWORKS`, which the Magisk module grants.

## What to log

Connect the phone over USB and run:

```
adb logcat -s CallBridge
```

The service writes these lines:

- `connecting: active=wifi+vpn, bound=<network>, networks=[...]`: the networks at connect time. `bound=none` means no non-VPN Wi-Fi was found.
- `connected to <host>, bound to wifi=true|false`: the address that answered and whether the socket was bound to Wi-Fi.
- `connected: active=..., networks=[...]`: the networks after the handshake.
- `network available`, `network <id> is wifi`, `network lost`: every network change while the service runs.

## Steps

1. Flash the current Magisk module, reboot, pair by QR over Wi-Fi.
2. Turn the VPN on, then connect again. Expect `bound to wifi=true` and a connected PC window.
3. Make a call and let it run for several minutes. Expect no drops and audio both ways.
4. Turn Wi-Fi off during the call: the call continues on the phone, the log shows `network lost`, and the bridge reconnects when Wi-Fi returns.
5. Turn Wi-Fi back on, with the VPN still on. Expect a reconnect within a few seconds and `bound to wifi=true` again.
6. Repeat with the VPN turned on and off in the middle of a call, and with mobile data on and off.

Write what happened in issue #12: the VPN app, whether the log showed `bound to wifi=true`, and how long each reconnect took.
