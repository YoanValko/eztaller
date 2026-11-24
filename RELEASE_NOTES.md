# Eztaller v1.0.0 — Release Test Notes

Copy and paste this content into the GitHub Release "Describe this release" field.

---

## What's New

**Eztaller v1.0.0** — One-click Windows app installer with automatic reboot.

- Select from **44 applications** across browsers, utilities, communication, gaming launchers, and developer tools
- All apps downloaded from **official publisher sources** only (never third-party mirrors)
- Every installer **digitally signature-verified** before execution (`WinVerifyTrust`)
- **Unattended silent installation** — one button, no command lines
- **Never aborts on failure** — one app failing doesn't stop the rest; all failures summarized
- **Auto-reboot** with cancel-able 60-second countdown
- **Administrator elevation** required and explained on launch

---

## Included Applications (44)

| Category | Apps |
|----------|------|
| **Browsers & Media** | Google Chrome, Mozilla Firefox, VLC media player |
| **Utilities** | 7-Zip, Notepad++, LibreOffice, OBS Studio, WinSCP, FileZilla, PuTTY, Audacity, Process Hacker 2, WinRAR, CCleaner |
| **Communication** | Discord, Spotify, Zoom, Steam, Slack, Microsoft Teams, WhatsApp Desktop, Telegram Desktop, Signal Desktop |
| **Adobe** | Adobe Acrobat Reader |
| **Gaming Launchers** | Epic Games Launcher, GOG Galaxy, Battle.net (Blizzard App), EA App, Ubisoft Connect, Roblox, Minecraft Launcher |
| **Developer Tools** | Visual Studio Code, Visual Studio Community 2022, Visual Studio Build Tools 2022, Eclipse Temurin JDK 21 (Java), Python 3.12, Node.js LTS, Git for Windows, Docker Desktop, Microsoft .NET SDK 8, Microsoft .NET Desktop Runtime 8, Microsoft PowerToys, qBittorrent, GIMP |

---

## How to Test

### Prerequisites
- Fresh Windows 10 or Windows 11 (64-bit)
- Administrator rights
- Internet connection

### Step 1: Download
Download both files from this release:
- `Eztaller.exe` (~147 MB self-contained, or ~204 KB if framework-dependent)
- `apps.json` (must be in the same folder as the exe)

### Step 2: Launch as Administrator
1. Right-click `Eztaller.exe`
2. Select **"Run as administrator"**
3. Confirm the UAC prompt
4. You should see the elevation explanation dialog if not running elevated

### Step 3: Verify UI
Expected behavior:
- [ ] Window title says "Eztaller"
- [ ] Header shows "Eztaller • One-click app installer & auto-reboot"
- [ ] App list shows 44 applications with checkboxes
- [ ] Each app shows name and publisher
- [ ] "Select all" and "Select none" checkboxes work
- [ ] Counter shows "X of 44 selected"
- [ ] "Install selected" button is enabled when at least one app is checked

### Step 4: Install Apps
1. Select 2–3 apps (e.g., 7-Zip, VLC, Google Chrome)
2. Click **"Install selected"**
3. Button changes to "Installing..." and becomes disabled
4. Progress bar appears with current app name
5. Log panel shows plain-language status for each app:
   - Downloading from official URL
   - Verifying digital signature
   - Installing...
6. Each app row shows a status icon (✓ / ✗ / ⚠) and status text in color

### Step 5: Verify Summary
After all installs finish:
- [ ] Status summary shows count (e.g., "3 installed, 0 failed")
- [ ] If any failures, they are listed with reasons under "Failures:"
- [ ] One app failing does NOT stop the remaining apps from installing

### Step 6: Verify Reboot
- [ ] Reboot popup appears with "Rebooting soon"
- [ ] Countdown timer starts at 60 seconds
- [ ] Popup shows "The PC will restart automatically to complete the installations"
- [ ] Clicking **"Cancel Reboot"** stops the countdown and dismisses the popup
- [ ] If not cancelled, PC reboots after 60 seconds

---

## Known Limitations

| Limitation | Details |
|------------|---------|
| **Admin rights required** | Must right-click → Run as administrator |
| **Gaming launchers need login** | Epic, Battle.net, EA App, Ubisoft Connect, Roblox, Minecraft require interactive login; Eztaller logs them instead of hanging |
| **Visual Studio needs restart** | VS Community/Build Tools may require a reboot to finish even after Eztaller's reboot |
| **winget may be missing** | On original Windows 10 (not updated), winget may not be present; Eztaller falls back to direct official downloads |
| **SmartScreen** | Unsigned exe may trigger Windows SmartScreen; click "More info → Run anyway" |
| **No auto-updates** | Download new releases manually from GitHub |

---

## Files in this Release

| File | Description |
|------|-------------|
| `Eztaller.exe` | The application executable (self-contained .NET 8) |
| `apps.json` | Application catalog with 44 official-source entries |

Keep both files in the same folder.

---

## Support

- Report bugs: [GitHub Issues](https://github.com/YoanValko/eztaller/issues)
- Suggest features: [GitHub Issues](https://github.com/YoanValko/eztaller/issues)
- Source code: [GitHub Repository](https://github.com/YoanValko/eztaller)

---

**Eztaller** — Install what you need, reboot when you're done. One click, official sources only.