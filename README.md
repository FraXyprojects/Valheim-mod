# ValheimSessionChronicle

Client-side Valheim mod for BepInEx that passively records a multiplayer session and writes a readable chronicle after disconnect. It analyzes the combat intensity, survival pressure, expedition character, and generates a procedural narrative summary of the session.

## Features

- **Client-Side Only**: Does not require server installation.
- **Multilingual Support**: Supports Czech and English outputs configurable via settings.
- **In-Game Status**: Unobtrusive on-screen UI indicator (can be customized or disabled).
- **Session Continuity**: Handles short temporary disconnects (e.g. portal loading or ZNet rebuilds) safely so they stay as one session.
- **Analytical Modules**: Smart tracking of Combat Intensity, Expedition Character, Camp Types, and Session Mood.
- **Discord Ready**: Generates a compact Markdown snippet specifically optimized for sharing on Discord.
- **Continuity Memory**: Saves history across sessions to establish context (e.g. "returned to the Swamp").
- **Offline & Deterministic**: The entire logic runs locally, without cloud APIs or AI requests.

## Installation

1. Install BepInEx for Valheim (e.g., through Thunderstore/r2modman).
2. Download the release `ValheimSessionChronicle-v1.0.0.zip`.
3. Extract and place `ValheimSessionChronicle.dll` into:
   `BepInEx/plugins/ValheimSessionChronicle/`
4. Start Valheim and join a server.
5. Play normally; reports are generated after you fully disconnect in:
   `BepInEx/plugins/ValheimSessionChronicle/Reports/<WorldName>/`

## Configuration

After the first launch, edit `BepInEx/config/fraxy.valheim.sessionchronicle.cfg`:

- `Language`: Čeština / English.
- `ShowStatusIndicator`: Enable/disable in-game UI.
- `SaveMarkdown` / `SaveJSON` / `SaveDiscord`: Toggle generation of specific output formats.
- `NarrativeVerbosity`: Controls the density of the story summary.

## Outputs

Each session generates coherent outputs with a smart filename (e.g. `2026-05-17_Combat_Swamp_a91f.md`).
- **.md**: Detailed Markdown report (Story, Chapters, Key Moments, Statistics).
- **.json**: Canonical, raw machine-readable session dump.
- **Discord/**: Short summarized snippet ready to be pasted into a Discord channel.
- **ChronicleHistory.md**: Running log of all past sessions.
