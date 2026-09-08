# ValheimSessionChronicle - Technical Documentation

## Architecture

The mod consists of several key subsystems:
1. **Session Lifecycle (`SessionLifecycleManager`)**: Manages states like `InWorld`, `TemporaryConnectionLoss`, and `Disconnecting`. Enforces a strict debounce to avoid fragmenting one real play session into multiple reports.
2. **Event Tracking (`SessionWatcher`, `SessionManager`)**: Subscribes to local events (via polling or Harmony patches) to observe kills, deaths, crafting, and biome entry. Emits semantic events (with `EventId` and args) instead of hardcoded strings.
3. **Analytics (`Reporting/Analysis`)**:
   - `CombatIntensityAnalyzer`: Uses rolling windows (30s, 60s, 90s) to gauge the pressure of encounters based on damage, kills, and near-death moments.
   - `SessionPhaseAnalyzer`: Groups events temporally to determine logical phases (e.g. "Exploration" vs "Intense Combat").
   - `ExpeditionProfileAnalyzer`: Uses weighted scoring to characterize the session (e.g., "Combat", "Sailing", "Building").
4. **Story Generator (`ChronicleStoryGenerator`)**: Deterministically composes narrative sentences from the analyzed events, using localization strings.
5. **Localization (`LocalizationManager`)**: Maps semantic `EventId` to either Czech or English templates.
6. **Output Storage (`SessionStorage`)**: Saves outputs to Markdown, Discord-Markdown, JSON, and updates the `ChronicleHistory.md`.

## Thunderstore Packaging

Packaging is automated via MSBuild. Building the solution in `Release` mode (`dotnet build -c Release`) automatically executes the `CreateThunderstoreZip` target which combines the DLL, `manifest.json`, `README.md`, and `icon.png` into a Thunderstore-compliant `.zip` archive in the `dist/` folder.
