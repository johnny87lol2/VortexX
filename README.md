<div align="center">

# VortexX

**A lightweight, customizable Roblox launcher for Windows.**

Keep Roblox up to date in a folder you choose, launch it with one click, and see exactly which server you are connected to.



</div>

---

> **Disclaimer:** VortexX is an independent, community-made project. It is not affiliated with, endorsed by, or sponsored by Roblox Corporation. "Roblox" is a trademark of Roblox Corporation. Use at your own risk.

## Features

- **Install manager**: downloads the latest Roblox and copies it into a folder of your choice. It checks the latest version and tells you when an update is available.
- **One-click launch**: optionally flushes the DNS cache and switches to the high-performance power plan before starting Roblox.
- **Server info**: automatically detects the Roblox server you are currently in (game name, place ID, job ID, IP) and shows its location, ISP and timezone. It can also look up any IP or hostname manually, and measures ping to Roblox services and your game server.
- **Roblox status**: shows platform status. It reads the official status page and falls back to a live connectivity check, so it avoids false "outage" alarms.
- **5 UI styles**: Cyber, Midnight, Matrix, Crimson and Arctic. They switch instantly.
- **Languages**: English (default) and Traditional Chinese.
- **Persistent settings**: stored in `%APPDATA%\Voretex_RB\settings.json`, so updating the launcher never resets them.
- **System monitor**: live CPU and RAM usage in the sidebar.

## Download and install

1. Go to the [Releases page](https://github.com/johnny87lol2/VortexX/releases) and download the latest `VortexX.zip`.
2. Extract it and run `VortexX.exe`.
3. Open **Install**, choose a folder, and press **Install Roblox**.
4. Open **Launch** and press **Launch Roblox**.

## Build from source

Requirements: Windows 10/11, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), Visual Studio 2022 (optional).

```bash
git clone https://github.com/johnny87lol2/VortexX.git
cd Vor
dotnet build -c Release
```

Or open `VortexX.sln` in Visual Studio 2022 and press **F5**.

## Project structure

```
VortexX/
├─ Models/      Settings and API data models
├─ Services/    Roblox logic, language, themes, system stats
└─ Views/       WPF main window (XAML + code-behind)
```

## Privacy

VortexX has no telemetry and no account. It only contacts Roblox endpoints (version check, installer download, game name lookup, status) and [ip-api.com](https://ip-api.com) (IP geolocation, only when you look up an IP or detect a server).

## Contributing

Issues and pull requests are welcome. For larger changes, please join the Discord https://discord.gg/sC4pcvq3zj go to chat and discuss what you would like to change.
