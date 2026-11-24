# Eztaller — One-Click App Installer & Auto-Reboot for Windows

**Eztaller** is a self-contained Windows desktop program that lets you pick popular applications from a checklist, installs them silently from their official publishers, and restarts your PC when finished — no command lines, no config files, no third-party mirrors.

Launch → tick the apps you want → click **Install selected** → walk away. When every installer finishes (or fails gracefully), Eztaller shows a summary and schedules a reboot with a cancel-able countdown.

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| **Official sources only** | Every app is downloaded from its publisher's official website or the official Windows Package Manager (`winget`) repository — never third-party mirrors. |
| **Signature verification** | Each downloaded installer is cryptographically verified with Windows `WinVerifyTrust` (valid publisher digital signature) before it is executed. |
| **Simple selection UI** | Checkbox list of apps, Select All / Select None, and a live "X of Y selected" counter. |
| **One-button install** | A single **Install selected** button starts the unattended batch. |
| **Silent installation** | Uses quiet/silent installer switches where supported (MSI `/qn`, EXE `/S`, etc.). |
| **Graceful interaction handling** | Installers that require user input are noted in the progress log instead of hanging the batch. |
| **Never abort on failure** | If one app fails, the reason is logged and the rest continue. All failures are summarized before reboot. |
| **Auto-reboot** | A 60-second cancel-able countdown popup appears after installs; click **Cancel Reboot** to abort, otherwise the PC restarts. |
| **Admin elevation** | Requests administrator rights at launch (required for installs and reboot) and explains why in one sentence. |
| **Self-contained** | Bundles the .NET 8 runtime — runs on any Windows 10/11 PC without installing anything first. |

---

## 📦 Included applications

**Browsers & media**
- Google Chrome
- Mozilla Firefox
- VLC media player

**Utilities**
- 7-Zip
- Notepad++
- LibreOffice
- OBS Studio
- WinSCP
- FileZilla
- PuTTY
- Audacity
- Process Hacker 2

**Communication**
- Discord
- Spotify
- Zoom
- Steam

**Adobe**
- Adobe Acrobat Reader

**Gaming launchers**
- Epic Games Launcher
- GOG Galaxy
- Battle.net (Blizzard App)
- EA App
- Ubisoft Connect
- Roblox
- Minecraft Launcher

**Developer tools**
- Visual Studio Code
- Visual Studio Community 2022
- Eclipse Temurin JDK 21 (Java)
- Python 3.12
- Node.js LTS
- Git for Windows
- Docker Desktop
- Microsoft PowerToys
- qBittorrent
- GIMP

*Want more apps? Edit `apps.json` — see [Customizing the app list](#customizing-the-app-list).*

---

## 🚀 Quick start

### 1. Download
Grab the latest release: **`Eztaller.exe`** (single self-contained executable) and keep `apps.json` in the same folder.

### 2. Run as administrator
Right-click `Eztaller.exe` → **Run as administrator**.

Eztaller needs elevation because it installs software and reboots the computer. You will see a one-sentence explanation on first launch if it is not running elevated.

### 3. Select and install
1. Tick the applications you want (or click **Select all**).
2. Click **Install selected**.
3. Watch the plain-language progress log as each app downloads, is signature-verified, and installs.
4. When finished, review the summary (successes and any failures).
5. A 60-second reboot countdown appears — click **Cancel Reboot** to abort, or let it restart to complete installations.

---

## 🛠 Requirements

- **Windows 10 or Windows 11** (64-bit)
- **Administrator rights** (required for installation and reboot)
- **Internet connection** (to download installers from official sources)
- **`winget`** (Windows Package Manager) is used as a fallback for some apps; if not present, Eztaller uses direct official downloads instead

No .NET installation is needed on the target PC — the executable is self-contained.

---

## 🔒 Security & verification

- **Official sources only:** All downloads come from the software publisher's own domain or the official Microsoft `winget` source. No third-party file mirrors, no bundled adware.
- **Digital signature check:** Every downloaded executable is verified with `WinVerifyTrust` against the publisher's authenticating signature before it is ever run. If verification fails, the app is marked as failed and skipped.
- **Silent switches:** Installers are launched with documented quiet flags so the batch runs unattended.
- **No telemetry, no accounts, no cloud sync:** Eztaller is purely local — it never sends data anywhere except to download the installers you explicitly chose.

---

## ⚙️ How it works

1. **Launch** → Elevation check → Load `apps.json`
2. **User picks apps** → Checkbox selection UI
3. **Install batch** → For each selected app:
   - Try `winget install` from the official Microsoft repository (if `wingetId` is provided), or
   - Download the installer from the publisher's official URL
   - Verify the digital signature
   - Run the installer with silent switches
   - Log success or failure, then move to the next app
4. **Summary** → Lists every success and every failure with reasons
5. **Reboot** → 60-second cancel-able countdown, then `InitiateSystemShutdownEx`

---

## 🧩 Customizing the app list

Eztaller reads `apps.json` from its own folder. Each entry looks like this:

```json
{
  "id": "unique-id",
  "name": "Display Name",
  "publisher": "Publisher Name",
  "wingetId": "Publisher.AppId",
  "officialUrl": "https://publisher.com/download/setup.exe",
  "installerFileName": "setup.exe",
  "installerType": "exe",
  "silentArgs": "/S",
  "rebootSuppress": true,
  "needsUserInput": false,
  "verifyMethod": "signature"
}
```

- `wingetId`: optional; if set and `winget` is available, the official Windows Package Manager is used.
- `officialUrl`: direct link to the publisher's installer. Leave blank for winget-only apps (e.g., gaming launchers).
- `silentArgs`: quiet-install command-line switches for that installer.
- `needsUserInput`: set to `true` for installers that require clicks/logins (they are logged, not hung).

Add, remove, or edit entries in `apps.json` and re-launch.

---

## 🏗 Building from source

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download) on Windows.

```bash
# Clone or copy the project folder
cd Eztaller

# Publish a single self-contained Windows executable
dotnet publish -c Release -r win-x64 --self-contained /p:PublishSingleFile=true
```

Output appears in:
```
bin/Release/net8.0-windows/win-x64/publish/Eztaller.exe
```

Ship `Eztaller.exe` together with `apps.json`.

---

## ⚠️ Limitations & scope

Eztaller intentionally stays small:

- **No accounts, no login, no cloud sync**
- **No auto-updates** — download a new release manually
- **No silent install for every app** — some launchers (Epic, Battle.net, etc.) require interactive login and are flagged accordingly
- **Reboot is mandatory-ish** — you can cancel the countdown, but some installers (e.g., Visual Studio) may require a restart to finish
- **Single machine only** — no remote deployment

---

## 📄 License

MIT License. Feel free to fork, modify, and redistribute.

---

## 🤝 Contributing

Pull requests are welcome. Suggested contributions:

- Additional official app entries in `apps.json`
- Improved silent-argument strings for newer installer versions
- UI/theme polish
- Additional verification options (SHA256 checksum fallback)

Please keep the scope to the list → install → reboot flow and avoid adding cloud, telemetry, or account features.

---

**Eztaller** — Install what you need, reboot when you're done. One click, official sources only.