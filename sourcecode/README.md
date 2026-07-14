# DEP3

A lightweight Windows MP3 music player built with C# and WinForms.

Made by [Gim4](https://github.com/soh4p)

## Features

- Open a folder of MP3 files and play with cover art, title, and artist
- Play / pause, previous, next, seek bar, volume control
- Shuffle (off or random) and optional auto-play to the next track
- Custom playlists (create, add songs, remove songs, delete playlist)
- Search and filter by artist
- Equalizer presets, playback device selection
- Launch on startup and minimise-to-tray on close
- Total playtime tracking

## Download

Get **installer.exe** from the [Releases](https://github.com/soh4p/DEP3/releases) page and run it to install.

A portable **DEP3.exe** is also included if you prefer not to install.

**Requires [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)** (Windows x64). The installer will prompt you to download it if missing.

## Build from source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) on Windows.

```powershell
# Build the app
dotnet publish DEP3.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release/staging

# Build installer (after staging exists)
powershell -ExecutionPolicy Bypass -File build.ps1
```

Source files are in the `sourcecode/` folder.

## License

MIT
