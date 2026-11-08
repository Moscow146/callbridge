# Changelog

## Unreleased

- Phone app: ringback tones (the beeps while the other side is being called) now come through to the PC for outgoing calls. The audio bridge starts once the call is off hook instead of only after the answer, and during the ringing the phone is quietened by turning the call volume to its minimum (a full device mute would silence the capture too); the full mute is applied after the answer as before.

## 0.5.1

- Magisk module: on Xiaomi/HyperOS the autostart permission (MIUI op 10008) was reset to "ignore" on every boot, so the link to the PC did not start after a reboot. The module now allows it at each boot, adds the app to the battery allowlist and, once the phone is unlocked, starts the link itself.

Reflash the Magisk module and reboot; after the first unlock the PC link starts by itself.

## 0.5.0

PC app
- "Start with Windows" (starts minimized in the tray), "Add to Start menu" and "Add to desktop" switches under Startup. They only touch the current user's Run key and folders, so nothing needs admin rights.
- Hold and a DTMF keypad in the call card. Keys also play a short local tone. The network can refuse hold (Android reports CALL_HOLD_FAILED, as on the 0890 test number); the window now says so instead of showing a hold that never happened.
- Optional echo cancellation for the microphone, off by default, now an Apple-style switch with an explanation. All switches get a pressed state and a springy knob.
- The microphone level bar moves with your voice, also with a phone connected (it used to stay empty).
- The status bar shows the estimated delay, jitter and late frames during a call, and a summary is written to `%APPDATA%\CallBridge\audio.log` every 5 seconds.
- Outgoing calls show the number instead of "Unknown number" and say "Dialing..." / "Calling..." until the other side answers, then "In call".
- When the phone does not confirm "To phone" / "To PC" within 3 seconds the window says so.
- SF Pro is used when it is already installed (it is not bundled), spring motion tied to frame time for 120 to 240 Hz displays, and new screenshots.

Phone app
- The link to the PC now comes back by itself after a reboot, an app update or a kill, and a stalled connection (Wi-Fi roaming, router sleep) is detected within 9 seconds on both sides and retried at once when the network changes.
- Fixed two crashes introduced by the new animations: one on launch and one when a call started.
- Hold, unhold and DTMF through a non-UI in-call service. Reflash the Magisk module: it adds the CONTROL_INCALL_EXPERIENCE permission to the privileged allowlist.
- The log shows which networks are up and whether the bridge socket is bound to Wi-Fi (see docs/NETWORK-CHECK.md for checking calls over Wi-Fi with a VPN).
- SF Pro is used when installed, and the call pill and screens get spring motion at the highest refresh rate the display offers.

Website
- Motion tied to frame time that respects reduced motion, SF Pro when installed, and refreshed screenshots.

Not verified yet: successful hold on a number where the carrier allows it, audible DTMF at the other end, echo cancellation on real speakers, and calls over Wi-Fi with a VPN (issues #11 and #12 stay open).

Reflash the Magisk module and reboot to update the phone app, then replace `CallBridge.exe`.

## 0.4.0

- Glass by the look of Apple materials on all three: the PC window, the phone app and the website. It is only the control layer; cards, lists and text stay solid.
- PC app: on Windows 11 the window uses the system Mica material, and the header and the call buttons bar are floating rounded capsules of glass above the content, inspired by Apple's materials: the cards scroll under them and show through as a blurred copy with a soft edge highlight and shadow. Cards, lists and text stay solid. Windows 10, high contrast and the "Transparency effects" switch in Windows colour settings give a plain solid window, and the app follows that switch while it runs.
- Phone app: the header and the bottom panel are floating glass capsules with a gradient rim and a soft shadow, and the content scrolls under them, inspired by Apple materials. Without window blur they fall back to the solid surface colour.
- Phone app: the call mode pill over the call screen is layered glass without window blur, which would blur the whole system call screen on some phones.
- Website: the header and the buttons are floating glass capsules over coloured bands, with a solid fallback when the browser has no blur or the reader asks for reduced transparency or more contrast.
- The workflow actions run on Node 24 and the PC app uses NAudio 2.4.0.

Reflash the Magisk module and reboot to update the phone app, then replace `CallBridge.exe`.

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
