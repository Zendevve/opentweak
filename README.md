# OpenTweak

<a href="https://prgportfolio.com" target="_blank">
    <img src="https://img.shields.io/badge/PRG-Gold Project-FFD700?style=for-the-badge&logo=data:image/svg%2bxml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBzdGFuZGFsb25lPSJubyI/Pgo8IURPQ1RZUEUgc3ZnIFBVQkxJQyAiLS8vVzNDLy9EVEQgU1ZHIDIwMDEwOTA0Ly9FTiIKICJodHRwOi8vd3d3LnczLm9yZy9UUi8yMDAxL1JFQy1TVkctMjAwMTA5MDQvRFREL3N2ZzEwLmR0ZCI+CjxzdmcgdmVyc2lvbj0iMS4wIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciCiB3aWR0aD0iMjYuMDAwMDAwcHQiIGhlaWdodD0iMzQuMDAwMDAwcHQiIHZpZXdCb3g9IjAgMCAyNi4wMDAwMDAgMzQuMDAwMDAwIgogcHJlc2VydmVBc3BlY3RSYXRpbz0ieE1pZFlNaWQgbWVldCI+Cgo8ZyB0cmFuc2Zvcm09InRyYW5zbGF0ZSgwLjAwMDAwMCwzNC4wMDAwMDApIHNjYWxlKDAuMTAwMDAwLC0wLjEwMDAwMCkiCmZpbGw9IiNGRkQ3MDAiIHN0cm9rZT0ibm9uZSI+CjxwYXRoIGQ9Ik0xMiAzMjggYy04IC04IC0xMiAtNTEgLTEyIC0xMzUgMCAtMTA5IDIgLTEyNSAxOSAtMTQwIDQyIC0zOCA0OAotNDIgNTkgLTMxIDcgNyAxNyA2IDMxIC0xIDEzIC03IDIxIC04IDIxIC0yIDAgNiAyOCAxMSA2MyAxMyBsNjIgMyAwIDE1MCAwCjE1MCAtMTE1IDMgYy04MSAyIC0xMTkgLTEgLTEyOCAtMTB6IG0xMDIgLTc0IGMtNiAtMzMgLTUgLTM2IDE3IC0zMiAxOCAyIDIzCjggMjEgMjUgLTMgMjQgMTUgNDAgMzAgMjUgMTQgLTE0IC0xNyAtNTkgLTQ4IC02NiAtMjAgLTUgLTIzIC0xMSAtMTggLTMyIDYKLTIxIDMgLTI1IC0xMSAtMjIgLTE2IDIgLTE4IDEzIC0xOCA2NiAxIDc3IDAgNzIgMTggNzIgMTMgMCAxNSAtNyA5IC0zNnoKbTExNiAtMTY5IGMwIC0yMyAtMyAtMjUgLTQ5IC0yNSAtNDAgMCAtNTAgMyAtNTQgMjAgLTMgMTQgLTE0IDIwIC0zMiAyMCAtMTgKMCAtMjkgLTYgLTMyIC0yMCAtNyAtMjUgLTIzIC0yNiAtMjMgLTIgMCAyOSA4IDMyIDEwMiAzMiA4NyAwIDg4IDAgODggLTI1eiIvPgo8L2c+Cjwvc3ZnPgo=" alt="Gold" />
</a>

<p align="center">
  <img src="OpenTweak/Assets/icon.png" alt="OpenTweak Logo" width="128" height="128">
</p>

<p align="center">
  <strong>🎮 The Transparent, Open-Source Alternative to Twiki</strong>
</p>

<p align="center">
  Automatically optimize your PC games with deterministic tweaks from PCGamingWiki — no black boxes, no guesswork, full transparency.
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#installation">Installation</a> •
  <a href="#usage">Usage</a> •
  <a href="#tech-stack">Tech Stack</a> •
  <a href="#architecture">Architecture</a> •
  <a href="#contributing">Contributing</a> •
  <a href="#license">License</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 8">
  <img src="https://img.shields.io/badge/Platform-Windows-0078D4?logo=windows&logoColor=white" alt="Windows">
  <img src="https://img.shields.io/badge/License-PolyForm%20Shield-blue" alt="License">
</p>

---

## 💰 Distribution Model

OpenTweak is **source-available and free to build**, with **pre-built binaries available for purchase**.

| | Build from Source | Buy Pre-built |
|----|-------------------|---------------|
| **Price** | Free | $25 USD |
| **Source Code** | ✅ Full access | ✅ Full access |
| **Features** | ✅ All features | ✅ All features |
| **Convenience** | Build it yourself | Download and run |
| **Support** | Community | Priority |

**Purchase:** [BuyMeACoffee - OpenTweak](https://buymeacoffee.com/opentweak)

> **Why charge for binaries?** As a student, I can't afford code signing certificates. The fee helps keep the project sustainable while keeping source code fully open. You're paying for convenience, not access.

---

## ✨ Why OpenTweak?

Most game optimization tools are **black boxes** — you don't know what they're doing to your system. OpenTweak is different:

| | OpenTweak | Other Tools |
|---|-----------|-------------|
| **Transparency** | ✅ Full source code available | ❌ Closed source |
| **Auditability** | ✅ Read exactly what each tweak does | ❌ Hidden operations |
| **Reversibility** | ✅ Automatic backups before any change | ❌ Often irreversible |
| **Reliability** | ✅ Deterministic recipes from PCGamingWiki | ❌ AI guesswork |
| **Privacy** | ✅ No telemetry, works offline | ❌ Cloud-dependent |

> *"Don't trust, verify."* — OpenTweak puts you in control of your gaming experience.

---

## 🚀 Features

### 🎯 Multi-Launcher Game Discovery
Automatically discovers installed games from all major PC gaming platforms:

- **Steam** — Scans library folders and app manifests
- **Epic Games Store** — Parses `.item` manifest files
- **GOG Galaxy** — Reads registry entries
- **Xbox Game Pass** — Scans WindowsApps folder
- **Manual Addition** — Add any game by path

### 📚 Deterministic Tweaks from PCGamingWiki
Fetches structured optimization data directly from [PCGamingWiki](https://www.pcgamingwiki.com) using the **Cargo API** — no brittle HTML scraping:

- Video settings (resolution, frame rate, VSync)
- Audio fixes (crackling, latency)
- Input optimizations (raw input, acceleration)
- Performance tweaks (shadow quality, draw distance)
- Bug fixes and workarounds

### 💾 Automatic Backup & Restore
**The "Holy Grail" feature:** Every configuration file is backed up before modification:

- One-click snapshot creation
- Full restore to previous state
- Multiple restore points per game
- Never worry about breaking your game

### 🎨 Windows 11 Modern UI
Built with [WPF-UI](https://github.com/lepoco/wpfui) for a native Windows 11 experience:

- Mica/Acrylic backdrop effects
- List/Grid view toggle
- Animated slide-over detail panel
- Fluent Design System
- Dark mode support

---

## 📸 Screenshots

<p align="center">
  <img src="docs/screenshots/main-window.png" alt="Main Window" width="600">
  <br>
  <em>Main library view with grid layout and search</em>
</p>

<p align="center">
  <img src="docs/screenshots/game-detail.png" alt="Game Detail" width="600">
  <br>
  <em>Game detail panel showing available tweaks</em>
</p>

---

## 📦 Installation

### Option 1: Purchase Pre-built Binary (Recommended)

1. **Purchase** from [BuyMeACoffee](https://buymeacoffee.com/opentweak) — $25 USD
2. **Download** the executable
3. **Run** `OpenTweak.exe`
4. **Enjoy** optimized gaming!

### Option 2: Build from Source

```powershell
# Clone the repository
git clone https://github.com/nathanielopentweak/opentweak.git
cd opentweak

# Build and run
dotnet build OpenTweak.sln --configuration Release
./OpenTweak/bin/Release/net8.0-windows/OpenTweak.exe
```

**Requirements:**
- Windows 10 or later
- .NET 8.0 SDK ([Download](https://dotnet.microsoft.com/download))

See [docs/DISTRIBUTION.md](docs/DISTRIBUTION.md) for detailed build instructions.

---

## 🎮 Usage

1. **Launch OpenTweak** — Your games are automatically discovered
2. **Select a game** — Click any game in your library
3. **Browse tweaks** — See available optimizations from PCGamingWiki
4. **Apply tweaks** — Click to apply; backups are created automatically
5. **Restore if needed** — Use the backup panel to undo changes

---

## 🏗️ Tech Stack

| Component | Technology |
|-----------|------------|
| **Framework** | .NET 8 + WPF |
| **UI Library** | [WPF-UI](https://github.com/lepoco/wpfui) (Windows 11 Fluent Design) |
| **Database** | [LiteDB](https://www.litedb.org/) (Embedded NoSQL) |
| **Config Parsing** | [Salaros.ConfigParser](https://github.com/salaros/config-parser) (Safe INI/CFG) |
| **MVVM Framework** | [CommunityToolkit.Mvvm](https://learn.microsoft.com/windows/communitytoolkit/mvvm/introduction) |
| **Data Source** | PCGamingWiki Cargo API |

---

## 🏛️ Architecture

OpenTweak follows a **layered architecture** with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                        UI Layer (Views)                      │
│              MainWindow, GameDetailView, GridView            │
├─────────────────────────────────────────────────────────────┤
│                    ViewModel Layer                           │
│              MainViewModel, GameDetailViewModel              │
├─────────────────────────────────────────────────────────────┤
│                     Service Layer                            │
│    GameScanner, PCGWService, TweakEngine, BackupService      │
├─────────────────────────────────────────────────────────────┤
│                     Data Layer                               │
│              LiteDB (Game, TweakRecipe, Snapshot)            │
└─────────────────────────────────────────────────────────────┘
```

See [docs/Architecture/Overview.md](docs/Architecture/Overview.md) for detailed architecture documentation.

---

## 🤝 Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) for details.

### Development Setup

```powershell
# Clone
git clone https://github.com/nathanielopentweak/opentweak.git
cd opentweak

# Restore
dotnet restore

# Build
dotnet build

# Test
dotnet test
```

---

## 📄 License

This project is licensed under the **PolyForm Shield License 1.0.0** with Commercial Distribution Addendum.

### Quick Summary

- ✅ **View** source code
- ✅ **Build** for personal use (free)
- ✅ **Modify** for personal use
- ❌ **Distribute** pre-built binaries (without permission)
- ❌ **Operate** automated build services for others
- ❌ **Create** competing products

### Binary Distribution

Pre-built binaries are available exclusively through:
- **BuyMeACoffee:** https://buymeacoffee.com/opentweak ($25 USD)

See [LICENSE.md](LICENSE.md) for full terms and [docs/DISTRIBUTION.md](docs/DISTRIBUTION.md) for distribution details.

---

## 🙏 Acknowledgments

- [PCGamingWiki](https://www.pcgamingwiki.com) — For maintaining the comprehensive game optimization database
- [WPF-UI](https://github.com/lepoco/wpfui) — For the beautiful Windows 11 UI components
- [PolyForm Project](https://polyformproject.org/) — For the Shield license

---

<p align="center">
  Made with ❤️ by a student developer
</p>
