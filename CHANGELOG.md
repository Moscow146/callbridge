# Changelog

## Unreleased

- PC app: the window uses the Windows 11 Mica material, and the header and the call buttons sit on a glass layer in the control layer only, inspired by Apple's materials. Cards, lists and text stay solid, and text on the glass keeps a contrast of at least 4.5:1 in light and dark.
- PC app: on Windows 10, in high contrast, and when transparency effects are off in Windows settings, everything is solid. The app follows that setting while it runs.
- The call buttons moved from the call card to a bar at the bottom of the window.

## 0.3.0

- New interface on the phone, the PC and the website, inspired by Apple's Human Interface Guidelines: system fonts, grouped sections, and no more animated blurred background.
- The apps follow the system's light or dark appearance, and the PC app follows Windows high contrast too.
- PC app: call buttons have text labels, the sliders and pickers work from the keyboard with a visible focus ring, and new shortcuts cover accepting, ending and muting a call.
- Phone app: permissions are requested when you connect instead of at launch, and the screen says what is missing and why. The connection only starts once microphone access is allowed.
- The QR scanner explains itself, has a Cancel button and offers Settings when camera access is off.
- The floating call button has a spoken label and a standard click action for TalkBack.
- CI can sign the APK with a permanent key when the repository has one, and the build workflow's default token is read-only.

Reflash the Magisk module and reboot to update the phone app.

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
