# ValheimSessionChronicle

Client-side Valheim mod for BepInEx that records a private multiplayer session and writes a readable Czech chronicle after disconnect.

The mod is passive. It does not change gameplay, does not require installation on the server, and only uses information visible to the local client.

## Features

- Session start and disconnect detection
- Czech `.txt` session report
- Raw `.json` session export
- In-memory event batching with no periodic disk writes during play
- Defensive Harmony patches for Valheim activity hooks
- Optional Discord webhook POST with the generated TXT summary
- Config switches for JSON, TXT, verbose logging, environment, combat, building, and crafting tracking

Tracked when available client-side:

- player join/session start/disconnect
- visible player deaths and local respawns
- local biome transitions and first biome discovery
- visible boss deaths
- local portal use
- sleeping and ship steering interaction
- local crafting
- local piece/workstation placement
- local item pickup milestones
- local combat moments and likely local enemy kills
- local tombstone creation
- weather and day/night transitions

## Installation

1. Install BepInEx for Valheim, for example through Thunderstore/r2modman.
2. Build `ValheimSessionChronicle.sln` in Release mode.
3. Copy `ValheimSessionChronicle.dll` to:

   `BepInEx/plugins/ValheimSessionChronicle/ValheimSessionChronicle.dll`

4. Start Valheim and join a server.
5. Reports are written after disconnect to:

   `BepInEx/plugins/ValheimSessionChronicle/Reports/`

## Build Setup

The project targets `net472`, which is the common target for BepInEx 5 Valheim mods.

By default the `.csproj` expects Valheim at:

`C:\Program Files (x86)\Steam\steamapps\common\Valheim`

If your installation is elsewhere, override `ValheimInstallPath` in Visual Studio/MSBuild:

```powershell
msbuild .\ValheimSessionChronicle.sln /p:Configuration=Release /p:ValheimInstallPath="D:\SteamLibrary\steamapps\common\Valheim"
```

The project references these local game/runtime files:

- `BepInEx/core/BepInEx.dll`
- `BepInEx/core/0Harmony.dll`
- `BepInEx/core/Newtonsoft.Json.dll`
- `valheim_Data/Managed/UnityEngine.CoreModule.dll`
- `valheim_Data/Managed/assembly_valheim.dll`
- `valheim_Data/Managed/assembly_guiutils.dll`

## Configuration

BepInEx creates the config file after the first launch:

`BepInEx/config/fraxy.valheim.sessionchronicle.cfg`

Options:

- `EnableDiscordWebhook`
- `DiscordWebhookURL`
- `SaveJSON`
- `SaveTXT`
- `EnableVerboseLogging`
- `TrackEnvironment`
- `TrackCombat`
- `TrackBuilding`
- `TrackCrafting`

Discord support is intentionally simple in phase 1: it sends a plain text code block to the configured webhook and truncates to fit Discord message limits.

## Example Report

```text
==================================================
VALHEIM SESSION REPORT
==================================================

Server: Dedicated Server
Datum: 17.05.2026
Délka session: 3h 24m

--------------------------------------------------
HRACI
--------------------------------------------------

- FraXson
- PATYSSON
- HIMMELHERGOTSON

--------------------------------------------------
HLAVNI UDALOSTI
--------------------------------------------------

[19:42]
FraXson vstoupil do biomu Swamp.

[20:12]
Boss Bonemass byl porazen.

--------------------------------------------------
STATISTIKY
--------------------------------------------------

Umrti:
- FraXson: 3

Portaly:
- Pouzito 7x
```

## Known Client-Side Limitations

This mod is not server authoritative. It cannot reliably know everything that happened on the server.

- Events outside local network relevance, visibility, or replicated state may be missing.
- Enemy kills are only confidently attributed to the local player if the local client recently damaged that enemy before death.
- Boss kills are detected when the boss death is visible/replicated to the client.
- Player activity for distant players is incomplete unless the client receives or observes the relevant object/event.
- Valheim method names can change between updates. Missing Harmony targets are logged and skipped instead of crashing the game.

## Thunderstore Package

Required package root files are included:

- `manifest.json`
- `README.md`
- `icon.png`

The build copies the compiled DLL into:

`package/plugins/ValheimSessionChronicle/`

For release packaging, place the compiled DLL under the same `plugins/ValheimSessionChronicle/` path in the zip together with `manifest.json`, `README.md`, `CHANGELOG.md`, and `icon.png`.
