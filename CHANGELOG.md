# Changelog

## 0.2.0

- The phone's speaker and microphone are silenced during a call on the PC, using the audio HAL's `device_mute`. The old mute was silently refused by Android, so the call was heard on both devices.
- The PC microphone really reaches the call: the app now holds `MODIFY_PHONE_STATE`, without which Android refused the track to the telephony transmit device and the caller heard the phone's microphone instead.
- Call modes: move a running call between the PC and the phone from the PC app, a floating button over the phone's call screen, or the notification.
- The call falls back to ordinary phone audio when the connection to the PC is lost.
- The microphone meter is in decibels, so quiet speech moves it.
- `service.sh` and the other module files keep LF line endings on Windows checkouts, so permissions are granted at boot.

Reflash the Magisk module and reboot: the new permission only applies to the system copy of the app.

## 0.1.1

- Wi-Fi works while a VPN is on: the connection is bound to the Wi-Fi network (needs `CONNECTIVITY_USE_RESTRICTED_NETWORKS`, granted by the Magisk module).
- The phone finds the PC by UDP broadcast when its address changed.
- Russian interface on Windows and Android.
- The audio delay is capped in both directions.

## 0.1.0

- Windows app with device pickers, volume, mute, answer / reject / hang up / dial, tray and contact suggestions.
- Android app: call state, caller audio to the PC, PC microphone into the call, QR pairing.
- Magisk module installing the app as a privileged system app.
