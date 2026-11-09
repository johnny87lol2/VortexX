<div align="center">

# VortexX

**A modern, lightweight and customizable Roblox launcher for Windows.**

Keep Roblox up to date in a folder you choose, launch it with one click, tweak the engine, swap the skybox, and see exactly which server you are connected to.



</div>

---

> **Disclaimer:** VortexX is an independent, community-made project. It is not affiliated with, endorsed by, or sponsored by Roblox Corporation. "Roblox" is a trademark of Roblox Corporation. Use at your own risk.

## Features

- **Modern interface**: rounded dark cards, a top pill navigation bar, a greeting hero banner on the home page, and smooth animations that can be turned off in Settings.
- **Install manager**: downloads the latest Roblox and copies it into a folder of your choice. It checks the latest version and tells you when an update is available.
- **One-click launch**: the Launch button on the home page starts Roblox right away. It can optionally flush the DNS cache and switch to the high-performance power plan first.
- **Mods**: put files in the Mods folder and they are applied over your Roblox install on launch. Original files are backed up and restored when you remove a mod.
- **Engine Settings (FastFlags)**: quick settings for FPS cap, MSAA, texture quality, post-processing, grass, shadows and DPI scaling, plus a JSON editor for custom flags.
- **Skybox**: drag and drop your sky `.tex` files onto the Skybox page. Files whose names match Roblox's sky files replace them automatically. One click restores the defaults.
- **Server info**: automatically detects the Roblox server you are currently in (game name, place ID, job ID, IP) and shows its location, ISP and timezone. It can also look up any IP or hostname manually, and measures ping to Roblox services and your game server.
- **Roblox status**: shows platform status. It reads the official status page and falls back to a live connectivity check, so it avoids false "outage" alarms.
- **AI Chat**: chat with Anthropic, OpenAI, Gemini or any OpenAI-compatible provider. The AI can propose changes to VortexX settings (theme, language, and more), and nothing is changed until you press Apply. API keys are encrypted with Windows DPAPI.
- **Check for updates**: compares your local `v.txt` with the one on GitHub and updates VortexX automatically when the versions differ.
- **5 UI styles**: Cyber, Midnight, Matrix, Crimson and Arctic. They switch instantly.
- **Languages**: English (default) and Traditional Chinese.
- **Persistent settings**: stored in `%APPDATA%\Voretex_RB\settings.json`, so updating the launcher never resets them.
- **System monitor**: live CPU and RAM usage in the footer and on the home page.

## Download and install

1. Go to the [Releases page](https://github.com/johnny87lol2/VortexX/releases) and download the latest `VortexX.zip`.
2. Extract it and run `VortexX.exe`.
3. Open **Install**, choose a folder, and press **Install Roblox**.
4. Press **Launch** on the home page (or open the **Launch** tab).

## Build from source

Requirements: Windows 10/11, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), Visual Studio 2022 (optional).

```bash
git clone https://github.com/johnny87lol2/VortexX.git
cd VortexX
dotnet build -c Release
```

Or open `VortexX.sln` in Visual Studio 2022 and press **F5**.

## Project structure

```
VortexX/
├─ Models/      Settings and API data models
├─ Services/    Roblox logic, AI, mods, updates, language, themes, system stats
└─ Views/       WPF main window (XAML + code-behind, one file per page group)
```

## Privacy

VortexX has no telemetry and no account. It only contacts:

- Roblox endpoints (version check, installer download, game name lookup, status)
- [ip-api.com](https://ip-api.com) (IP geolocation, only when you look up an IP or detect a server)
- GitHub (the update check and update download, only when you press **Check for updates**)
- The AI provider you configure (only when you use AI Chat)

## Contributing

Issues and pull requests are welcome. For larger changes, please join the Discord https://discord.gg/sC4pcvq3zj go to chat and discuss what you would like to change.
