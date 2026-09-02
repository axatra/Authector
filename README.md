# Authector

A secure, privacy-first TOTP authenticator for Windows built with WinUI 3.

No cloud sync. No telemetry. Your codes stay on your device, encrypted with Windows DPAPI and protected by Windows Hello.

## Features

- **TOTP codes** — Generate 6-digit Time-based One-Time Passwords (RFC 6238)
- **Windows Hello** — Biometric or PIN lock on app launch
- **DPAPI encryption** — Secrets encrypted with your Windows user credentials
- **Brand-aware cards** — Dynamic grid layout with brand colors and service logos
- **Custom logos** — Upload your own logo for any account
- **Import/Export** — Encrypted backup and restore with password protection
- **Copy to clipboard** — One-click TOTP code copying
- **Per-account eye toggle** — Show/hide individual codes
- **Screen capture protection** — Blocks screen capture on the code overlay

## Supported Services

30+ built-in brand logos including Google, GitHub, Microsoft, Discord, X (Twitter), Steam, Apple, Facebook, Amazon, Slack, and more.

## Requirements

- Windows 10 (build 19041) or later
- .NET 10 runtime

## Building from Source

```bash
dotnet build -p:Platform=x64
```

Or open `Authector.slnx` in Visual Studio 2022+.

## Project Structure

```
Authector/
├── Models/           # Account data model
├── Services/         # Core logic (TOTP, storage, crypto, logos)
├── Assets/           # Icons, logos, splash screen
│   └── Logos/        # Built-in service brand logos
├── MainWindow        # Main card grid UI
├── AddAccountDialog  # Manual account entry
├── EditAccountDialog # Edit account + custom logo upload
├── PasswordDialog    # Password prompt for backups
├── SettingsDialog    # Settings, backup, import/export
└── ScreenCaptureOverlay
```

## Contributing

Contributions are welcome! Please open an issue first to discuss what you'd like to change.

## License

[MIT](LICENSE)

## Author

[@ehsanlax](https://www.instagram.com/ehsanlax/)
