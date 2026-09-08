# ValheimSessionChronicle

Client-side Valheim mod for BepInEx that records a private multiplayer session and writes a readable Czech chronicle after disconnect.

The mod is passive. It does not change gameplay, does not require installation on the server, and only uses information visible to the local client.

## Features

- Session start and disconnect detection
- Czech `.txt` session chronicle with a procedural story summary
- Optional raw `.debug.json` export for troubleshooting
- Robust session lifecycle with reconnect tolerance for temporary ZNet/player rebuilds
- Combat intensity, survival pressure, expedition profile, and camp classification analysis
- Progression context analysis from observed inventory, nearby containers, crafting stations, and WorldMemory
- Discovery value and resource-operation analysis so repeated late-game resources do not look like first discoveries
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
- lightweight progression observations from local inventory, nearby loaded containers, and nearby crafting stations

## Installation

1. Install BepInEx for Valheim, for example through Thunderstore/r2modman.
2. Build `ValheimSessionChronicle.sln` in Release mode.
3. Copy `ValheimSessionChronicle.dll` to:

   `BepInEx/plugins/ValheimSessionChronicle/ValheimSessionChronicle.dll`

4. Start Valheim and join a server.
5. Reports are written after disconnect to a world-specific folder:

   `BepInEx/plugins/ValheimSessionChronicle/Reports/<WorldName>/`

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
- `ReconnectToleranceSeconds`
- `DisconnectDebounceSeconds`
- `EnableVerboseLogging`
- `TrackEnvironment`
- `TrackCombat`
- `TrackBuilding`
- `TrackCrafting`

Discord support is intentionally simple in phase 1: it sends a plain text code block to the configured webhook and truncates to fit Discord message limits.

## World Memory

ValheimSessionChronicle keeps lightweight best-effort memory per world. This is not an authoritative world database; it stores only observed persistent information that helps future session chronicles understand continuity.

Folder layout:

```text
BepInEx/plugins/ValheimSessionChronicle/Reports/
└── Doupeson/
    ├── DoupesonWorldMemory.json
    ├── 2026-05-21_BojovaVyprava_Swamp.txt
    ├── 2026-05-22_StavitelskaVyprava_Meadows.txt
    └── Raw/
        └── 2026-05-21_BojovaVyprava_Swamp_session.debug.json
```

World memory tracks lightweight observations:

- persistent camp/base clusters and tier evolution
- important structures such as forge, stonecutter, artisan table, black forge, eitr refinery, galdr table, and blast furnace
- observed portal names and approximate locations
- discovered biomes, important items, and boss progression
- progression evidence from observed player inventory, nearby stockpiles, and crafting infrastructure

The memory intentionally does not store every kill, every structure, or raw combat history.

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
CHARAKTER VÝPRAVY
--------------------------------------------------

- Bojová: 57%
- Průzkumná: 22%
- Stavitelská: 14%
- Námořní: 7%

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
- HP tracking is client-side best effort. If Valheim does not expose reliable health/damage data for a situation, the chronicle falls back to combat pressure and death statistics.
- Inventory, nearby container, and nearby crafting station scans are lightweight best effort. They only see loaded objects available to the client and do not represent the full world.
- Temporary object rebuilds, ZNet resets, and short reconnects are treated as one continuous play session within `ReconnectToleranceSeconds`.
- World memory contains only previously observed client-side state. It improves continuity, but it does not prove that unseen structures still exist.
- Valheim method names can change between updates. Missing Harmony targets are logged and skipped instead of crashing the game.

## Thunderstore Package

Package root files:

- `manifest.json`
- `README.md`
- `icon.png`

The final report filename uses the dominant expedition profile and biome, for example:

`2026-05-17_BojovaPruzkumnaVyprava_Swamp.txt`

The build copies the compiled DLL into:

`package/plugins/ValheimSessionChronicle/`

For release packaging, place the compiled DLL under the same `plugins/ValheimSessionChronicle/` path in the zip together with `manifest.json`, `README.md`, `CHANGELOG.md`, and `icon.png`.
