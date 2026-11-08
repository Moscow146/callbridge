# Changelog

## Unreleased

- Outgoing calls: the PC no longer shows "Unknown number" and no longer says "In call" before the other side answers. The phone took the number only from the phone-state broadcast, which carries no number for outgoing calls (and none for incoming ones without call log access), and reported every off-hook call as active. It now follows the Telecom call itself: the number comes from the call handle, and the state is "Dialing…" while connecting, "Calling…" while it rings, "In call" only after the answer, then "On hold". The `state` message gets an optional `stage` field, older PC apps ignore it. Audio capture still starts only in the active state.

- Phone app: the link to the PC now comes back on its own. The service is started again after the phone boots and after the app is updated (a new `StartReceiver`; it only starts when the link was on and the phone is still paired, and "Stop" turns that off). Before, installing a new build killed the service and it stayed dead until the app was opened, so the PC showed "Phone not connected". The in-call service also starts the link when a call begins, in case MIUI killed it without restarting it. A call that is already in progress when the service starts is picked up too. If Android refuses the microphone foreground type from the background, the service starts with a plain special-use type and switches to the microphone type when call audio starts.
- Phone app and PC app: a connection that stalls (Wi-Fi roaming, router sleep, the PC going to sleep) is now detected within 9 seconds on both sides, because each side drops the socket when it has heard nothing, although the PC pings every 2 seconds. The phone retries with a growing delay (3 to 15 seconds) and retries at once when a network appears or disappears, and holds a Wi-Fi lock while the link is on. The reason is shown in the status ("PC stopped answering. Trying again").
- PC app: when the phone does not confirm "To phone" / "To PC" within 3 seconds (or the command cannot be sent), the window says so instead of silently staying in the old mode.
- PC app: pressing a key on the on-screen keypad now plays a short local DTMF tone (two sine waves per the DTMF table, 140 ms with a soft fade in and out, kept quiet) on the selected output device, including * and #. The tone is played on the PC only and never goes into the microphone path of the call. The demo has a new `--demo keypad` scene.
- PC app: the Keypad button lost its icon when switched on, leaving a plain blue circle, because it had no icon for the on state. It now keeps its icon in the contrasting theme color in the Dark, Light and High contrast themes, like Hold.
- PC app: the microphone level bar under the microphone picker now moves when you speak, with or without a phone connected. It used to read the level only from the capture opened for a call, so it stayed empty until a call session existed. The window now measures the selected microphone itself while it is visible and releases it when the window is hidden to the tray or minimized, or when the phone connects. The bar takes the Gain slider into account, rises quickly and falls smoothly, and has an outline and a highlight color in high contrast.
- PC app: echo cancellation is now an Apple-style switch with a short explanation instead of a standard checkbox. All switches get a pressed state (wider knob) and a springy knob slide, which is skipped when "Show animations in Windows" is off.
- Phone app: fixed two crashes caused by the new animations: one on launch (an invalid view tag key) and one when a call started (reading the display from a service).

- SF Pro in the PC app, the phone app and the website, used only when it is already installed on the device (SF is Apple's proprietary font, so it is not bundled or hosted anywhere). Without it the PC app uses Segoe UI Variable, the phone app uses Roboto and the website uses the system font, with the same sizes and spacing.
- Motion on all three: spring and eased transitions tied to frame time instead of a fixed rate, so they stay smooth at 120 to 240 Hz. The phone app springs its header, sections, status badge and buttons in, fades the call mode pill in and out, and asks Android for the display mode with the highest refresh rate. The PC window fades and slides cards and call buttons in, presses buttons with a spring, and asks WPF for up to 240 frames per second while animating. The website has hover lifts, scroll reveal and a staggered entrance, using only transform and opacity. All of it is switched off by the system "remove animations" setting on Android, "Show animations in Windows" on the PC and "prefers-reduced-motion" on the website.
- Hold in the PC window: the phone does receive the command and asks Android to hold the call, but the network can refuse it (Android reports CALL_HOLD_FAILED), and the PC then showed a hold that never happened. The phone app now reports back when the network refuses it (the PC app puts the button back and says so, instead of showing a hold that never happened); DTMF tones now last 250 ms instead of 160 ms. Some operator numbers such as 0890 refuse hold on this phone, so hold is tested on them only as far as the refusal.
- Phone app: the log (`adb logcat -s CallBridge`) now shows which networks are up and whether the bridge socket is bound to Wi-Fi, and docs/NETWORK-CHECK.md has the steps for checking calls over Wi-Fi with a VPN.
- Hold and a keypad for DTMF tones in the PC window, with matching `hold` and `dtmf` control messages. The phone app now registers a non-UI in-call service for them, so reflash the Magisk module (it adds the `CONTROL_INCALL_EXPERIENCE` permission) and reboot.
- PC app: the status bar shows the estimated audio delay, jitter and late frames during a call. The PC pings the phone every two seconds to measure the round trip, and a summary line (delay, round trip, jitter, late frames, buffer trims) is written to `%APPDATA%\CallBridgeudio.log` every five seconds.
- PC app: optional echo cancellation for the microphone (a checkbox under Microphone, off by default). It subtracts the phone's voice, as played on the PC speakers, from the microphone with an adaptive filter, so the caller hears less of themselves when you use speakers instead of headphones. Headphones are still the best fix.
- PC app: a "Start with Windows" switch under Startup starts CallBridge at sign-in, minimized to the tray. It is kept in the current user's Run key, so nothing needs admin rights.
- PC app: "Add to Start menu" and "Add to desktop" switches under Startup create or remove a CallBridge shortcut for the running CallBridge.exe in the current user's folders, so nothing needs admin rights.

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
