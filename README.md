# OpenTweak

<p align="center">
  <img src="OpenTweak/Assets/icon.png" alt="OpenTweak Logo" width="128" height="128">
</p>

<p align="center">
  <strong>Transparent, Open-Source PC Game Optimization</strong>
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#installation">Installation</a> •
  <a href="#building">Building</a> •
  <a href="#how-it-works">How It Works</a> •
  <a href="#license">License</a>
</p>

---

## Why OpenTweak?

Unlike "black-box" AI tools that guess what to do, **OpenTweak uses deterministic recipes** parsed from [PCGamingWiki](https://www.pcgamingwiki.com). Every tweak is:

- ✅ **Auditable** — Read exactly what each tweak does
- ✅ **Reversible** — Automatic backups before any change
- ✅ **Reliable** — Same recipe = same result, every time
- ✅ **Transparent** — Full source code available

## Features

### 🎮 Multi-Launcher Detection
Automatically discovers games from:
- Steam
- Epic Games Store
- GOG Galaxy
- Xbox Game Pass
- Manual paths

### 📚 PCGW Integration
Fetches structured data via the MediaWiki Cargo API — no brittle HTML scraping.

### 💾 Snapshot & Revert
**The "Holy Grail" feature**: Every config file is backed up before modification. One click restores everything.

### 🔧 Safe Configuration Editing
Uses [Salaros.ConfigParser](https://github.com/salaros/config-parser) to preserve comments and formatting in `.ini`/`.cfg` files.

### 🎨 Modern Windows 11 UI
Built with [WPF-UI](https://github.com/lepoco/wpfui) for native Mica/Acrylic effects.

## Installation

### Signed Binary (Recommended)
Download the latest signed `.exe` from [Releases](https://github.com/your-repo/releases).

> 💡 The signed binary won't trigger Windows SmartScreen warnings.

### Build from Source
See [Building](#building) below.

## Building

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11

### Build Commands
```powershell
# Clone the repo
git clone https://github.com/your-repo/OpenTweak.git
cd OpenTweak

# Restore dependencies
dotnet restore

# Build
dotnet build --configuration Release

# Run
dotnet run --project OpenTweak
```

### Publish Single-File Executable
```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

## How It Works

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  Game Scanner   │────▶│   PCGW Service   │────▶│  Tweak Engine   │
│                 │     │   (Cargo API)    │     │                 │
│ Steam/Epic/GOG  │     │                  │     │ BackupService   │
│ Xbox/Manual     │     │ Video_settings   │     │ ConfigParser    │
└─────────────────┘     │ Audio_settings   │     └─────────────────┘
                        │ Input_settings   │              │
                        └──────────────────┘              ▼
                                                  ┌─────────────────┐
                                                  │   LiteDB        │
                                                  │   (Local DB)    │
                                                  └─────────────────┘
```

1. **Scan**: Detect installed games from all launchers
2. **Fetch**: Query PCGW Cargo API for structured tweak data
3. **Preview**: Show diff of proposed changes
4. **Backup**: Snapshot all target files
5. **Apply**: Use safe parsers to modify configs
6. **Restore**: One-click rollback from any snapshot

## Project Structure

```
OpenTweak/
├── Models/
│   ├── Game.cs          # Game metadata
│   ├── TweakRecipe.cs   # Deterministic tweak definition
│   └── Snapshot.cs      # Backup snapshot
├── Services/
│   ├── GameScanner.cs   # Multi-launcher detection
│   ├── PCGWService.cs   # Cargo API client
│   ├── TweakEngine.cs   # Safe config modification
│   ├── BackupService.cs # Snapshot management
│   └── DatabaseService.cs # LiteDB wrapper
├── ViewModels/
│   ├── MainViewModel.cs
│   └── GameDetailViewModel.cs
└── Views/
    ├── MainWindow.xaml
    └── GameDetailView.xaml
```

## License

This project uses the **[PolyForm Shield License 1.0.0](LICENSE.md)**.

- ✅ Read and audit the code
- ✅ Build for personal use
- ✅ Modify for your own use
- ❌ Sell or redistribute commercially
- ❌ Create competing products

---

<p align="center">
Made with ❤️ for the PC gaming community
</p>
