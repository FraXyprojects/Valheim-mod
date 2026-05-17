# ValheimSessionChronicle

Client-side Valheim mod for BepInEx that records a private multiplayer session and writes a readable Czech chronicle after disconnect.

The mod is passive. It does not change gameplay, does not require installation on the server, and only uses information visible to the local client.

## Features

- Session start and disconnect detection
- Czech `.txt` session chronicle with a procedural story summary
- Optional raw `.debug.json` export for troubleshooting
- In-memory event batching with no periodic disk writes during play
- Defensive Harmony patches for Valheim activity hooks
- Optional Discord webhook POST with the TXT session summary
- Config switches for TXT output, debug JSON, compact timeline, verbose logging, environment, combat, building, and crafting tracking

Tracked when available client-side:

- player join/session start/disconnect
- visible player deaths and local respawns
- local biome transitions and first biome discovery
- visible boss deaths
- local portal use
- sleeping and ship steering interaction
- local crafting
- local piece/workstation placement
- meaningful local item pickup milestones
- aggregated combat statistics, dangerous encounters, and likely local enemy kills
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
- `SaveTXT`
- `EnableDebugJsonExport`
- `IncludeCompactTimeline`
- `EnableVerboseLogging`
- `TrackEnvironment`
- `TrackCombat`
- `TrackBuilding`
- `TrackCrafting`

Discord support is intentionally simple in phase 1: it sends a plain text code block to the configured webhook and truncates to fit Discord message limits.

## Example Report

```text
==================================================
VALHEIM SESSION CHRONICLE
==================================================

--------------------------------------------------
SESSION
--------------------------------------------------

Server: Dedicated Server
Datum: 17.05.2026
Délka session: 3h 24m

--------------------------------------------------
HRÁČI
--------------------------------------------------

- FraXson
- PATYSSON
- HIMMELHERGOTSON

--------------------------------------------------
PŘÍBĚH SESSION
--------------------------------------------------

FraXson se během výpravy vydal z biomu Meadows až do Black Forest.
V biomu Meadows vznikl malý tábor pro další výpravu.
Mezi důležité nálezy a milníky patřily Ancient Seed a Surtling Core.
FraXson v boji porazil hlavně 12x Greydwarf a 4x Skeleton.
Výprava skončila bez smrti po 3h 24m dobrodružství.

--------------------------------------------------
HLAVNÍ MOMENTY
--------------------------------------------------

- [19:42] První návštěva biomu Black Forest.
- [19:55] FraXson během výpravy porazil už 5x Greydwarf v biomu Black Forest.
- [20:12] Boss Bonemass byl poražen.

--------------------------------------------------
STATISTIKY
--------------------------------------------------

Úmrtí:
- FraXson: 3

Cestování:
- Portály použity: 7x
```

## Known Client-Side Limitations

This mod is not server authoritative. It cannot reliably know everything that happened on the server.

- Events outside local network relevance, visibility, or replicated state may be missing.
- Enemy kills are only confidently attributed to the local player if the local client recently damaged that enemy before death.
- Boss kills are detected when the boss death is visible/replicated to the client.
- Player activity for distant players is incomplete unless the client receives or observes the relevant object/event.
- Common survival resources such as stone, wood, resin, mushrooms, neck tails, boar meat, leather scraps, and raspberries are intentionally excluded from the final chronicle.
- Valheim method names can change between updates. Missing Harmony targets are logged and skipped instead of crashing the game.

## Thunderstore Package

Package root files:

- `manifest.json`
- `README.md`
- `icon.png`

The build copies the compiled DLL into:

`package/plugins/ValheimSessionChronicle/`

For release packaging, place the compiled DLL under the same `plugins/ValheimSessionChronicle/` path in the zip together with `manifest.json`, `README.md`, `CHANGELOG.md`, and `icon.png`.
