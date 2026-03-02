# Empire Engine v1.0

Custom C#/.NET 8 engine + deterministic 4X game built on Silk.NET/OpenGL (no Unity/Godot).

## Architecture
- `Engine/` engine/runtime systems.
- `Game/CoreSim` pure deterministic rules/simulation (no Engine refs).
- `Game/Net` host-authoritative sequential-turn TCP networking.
- `Game/Presentation` renderer/input/UI bridge.
- `Game/Content` data-driven civs/units/techs/buildings/improvements/resources.

## Build
```bash
dotnet restore Empire.sln
dotnet build Empire.sln -c Release
dotnet test Empire.sln -c Release
```

## Run
### Singleplayer
```bash
dotnet run --project Game/Game.csproj
```
### Host multiplayer
```bash
dotnet run --project Game/Game.csproj -- host
```
### Join multiplayer
```bash
dotnet run --project Game/Game.csproj -- join 127.0.0.1:7777
```
### Replay file
```bash
dotnet run --project Game/Game.csproj -- replay replay.json
```
### Load save
```bash
dotnet run --project Game/Game.csproj -- load savegame.json
```

## Build a launchable Windows .exe package (every update)
Use the release script after each code update to always produce a fresh runnable `.exe` + zip package.

### PowerShell (Windows)
```powershell
./scripts/build-release.ps1 -Configuration Release -Runtime win-x64 -Version 1.0.0
```

### Bash
```bash
./scripts/build-release.sh
```

Artifacts are written to:
- `artifacts/publish/win-x64/Game.exe`
- `artifacts/Empire-<version>-win-x64.zip`

> Optional self-contained publish:
> - PowerShell: add `-SelfContained`
> - Bash: `SELF_CONTAINED=true ./scripts/build-release.sh`

## Controls
- WASD + RMB drag + wheel: camera.
- LMB select unit, RMB issue move.
- Buttons: end turn, found city, set research, queue unit, build improvement.
- `F5` state dump to `logs/` (**devtools mode only**).
- `F6` quick save (**devtools mode only**).
- `F7` request snapshot resend (**devtools + network client only**).


## Hidden Developer Diagnostics
- Developer diagnostics are **off by default** and invisible to normal players.
- Enable with CLI flag `--devtools` or env var `EMPIRE_DEVTOOLS=1`.
- When enabled, the game writes high-volume debug telemetry to `logs/debug_*.log` including:
  - command application/results and simulation exceptions
  - client/server network payload traces
  - event and input trace entries
- The in-game Event Log/debug hash overlays are only shown in devtools mode.

## Multiplayer Stability
- Versioned protocol handshake.
- Hash check per command; desync triggers snapshot resend.
- Join/reconnect gets full snapshot.
- Spectator handshake supported (`spectator=true`).

## Save/Load + Replay
- Save file includes snapshot + seed + save version.
- Corrupted save handling returns graceful error.
- Replay mode re-simulates from initial snapshot + command log deterministically.

## Strategic systems
- Culture borders + tile ownership.
- Builder improvements (Farm/Mine/LumberMill).
- Strategic resources (Wheat/Iron/Horse) with activation by improvement.
- Combat depth: zone of control, flanking, fortify, healing.
- Fog of war with mountain LOS blocking and forest visibility penalty.
- Diplomacy: war/peace, open borders, GPT trade deals.
- Victories: Domination, Science, Culture, Score(turn limit).

## Packaging
Build release output from `Game/bin/Release/net8.0` and zip with:
- `Game.exe`
- `Content/`
- `logs/` (empty folder included)

## Dependencies
- Silk.NET.Windowing (MIT)
- Silk.NET.Input (MIT)
- Silk.NET.OpenGL (MIT)
- xUnit (Apache-2.0)
- Microsoft.NET.Test.Sdk (MIT)
- coverlet.collector (MIT)

## Known limitations
- UI is intentionally minimal (custom immediate mode).
- Rendering path is not fully instanced yet.
- Host migration is not implemented (optional roadmap item).
