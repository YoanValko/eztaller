# Contributing to Eztaller

Thanks for your interest! Eztaller is intentionally small — its job is **list → install → reboot**, nothing more. Please keep that scope in mind when contributing.

## How to contribute

1. **Fork the repo** and create a feature branch (`git checkout -b feature/your-idea`).
2. **Make your change** and test it on Windows with .NET 8.
3. **Commit** with a clear message.
4. **Open a pull request** describing what changed and why.

## Good first contributions

- Add new official app entries to `apps.json` (verify the URL points to the publisher's own domain).
- Update silent-install arguments for newer installer versions.
- Improve the WPF UI (theme, layout, accessibility).
- Add SHA256 checksum verification as a fallback alongside signature checks.

## What to avoid

- Cloud sync, user accounts, auto-updates, or telemetry.
- Third-party download mirrors or unverified URLs.
- Features outside the install-and-reboot flow.

## Code style

- Follow the existing C# naming and project structure.
- Keep UI logic in the ViewModel and installer logic in the `IInstaller` implementations.
- Every app in `apps.json` must have an official source (wingetId or officialUrl).

## License

By contributing, you agree your changes are licensed under the MIT License.