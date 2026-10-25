# callbridge

Take phone calls on your Windows PC: pick any mic and headphones, mute, and more. Android app + Windows .exe

The phone stays in your pocket. Its cellular call audio is streamed to the PC, so you hear the caller in the headphones you choose and talk into the microphone you choose.

## Status

| Part | State |
|------|-------|
| Windows app (WPF, WASAPI, Opus) | works: device pickers, volume, mute, answer / reject / hang up / dial |
| Android app: call state, answer, hang up, dial | implemented |
| Android app: caller audio to PC | implemented (`VOICE_DOWNLINK`), needs the app installed as a system app |
| PC microphone into the call | not implemented yet, see below |

### PC microphone into the call

The audio HAL of Snapdragon devices exposes an `incall_music_uplink` output (`AUDIO_OUTPUT_FLAG_INCALL_MUSIC`) routed to `Telephony Tx`. Regular Java `AudioTrack` cannot request that flag, so it needs a native helper. That is the next step.

## Requirements

- Android 12+ with root (Magisk). Tested on Redmi K50 Ultra, HyperOS 3 (Android 15).
- Windows 10/11.

## Build

Windows (.NET 10 SDK):

```powershell
dotnet build windows\CallBridge.sln
dotnet run --project windows\CallBridge.App
```

Android (JDK 17+, Android SDK, Gradle wrapper in `android\`):

```powershell
.\scripts\build-android.ps1 -Module
```

The result is `build\callbridge-magisk.zip`. Flash it in the Magisk app and reboot. The module installs the app into `/system/priv-app`, grants `CAPTURE_AUDIO_OUTPUT` and the runtime permissions.

## Use

1. Start the Windows app. It shows a 6-digit pairing code and the PC address.
2. Open CallBridge on the phone, enter the PC address and the code, tap Connect.
3. Over USB instead of Wi-Fi: `adb reverse tcp:47800 tcp:47800` and use `127.0.0.1` as the address.

Protocol: [docs/PROTOCOL.md](docs/PROTOCOL.md).
