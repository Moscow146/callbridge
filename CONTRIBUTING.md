# Contributing

- Windows: .NET 10 SDK, `dotnet test windows\CallBridge.sln`, `dotnet run --project windows\CallBridge.App`.
- Android: JDK 17+, Android SDK with platform 36, `.\scripts\build-android.ps1 -Module`.
- Screenshots: use `CallBridge.exe --demo <pairing|idle|ringing|call|suggest>` on Windows, and crop the status bar on Android. Never commit a real pairing QR, contacts or device names.
- Releases: `.\scripts\bump-version.ps1 0.3.0`, commit, then `git tag v0.3.0` and push the tag. CI builds the files and creates the release.
- Signing: CI signs the APK with a permanent PKCS12 key (alias `callbridge`) when the `CALLBRIDGE_KEYSTORE_B64` and `CALLBRIDGE_KEYSTORE_PASSWORD` repository secrets exist, and with a debug key otherwise. For a local build with the same key, set `CALLBRIDGE_KEYSTORE` to the key file and `CALLBRIDGE_KEYSTORE_PASSWORD` to its password.
- Protocol changes go into [docs/PROTOCOL.md](docs/PROTOCOL.md) in the same commit.
