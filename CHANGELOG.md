# Changelog

## 1.0.0
- **Major Architecture Overhaul**: Replaced hard-coded Czech strings with a generic Localization Engine. Added full English support.
- **Smart Analytics**: Added rolling windows for `CombatIntensityAnalyzer`, improved `ExpeditionProfileAnalyzer` weighted scoring.
- **In-Game UI**: Added a configurable, unobtrusive in-game status indicator using Unity uGUI.
- **Session Lifecycle**: Repaired temporary disconnect handling to guarantee "One Session = One Chronicle".
- **Output Ecosystem**: Added Markdown, Discord-optimized Markdown, Canonical JSON, and continuity tracking (`ChronicleHistory.md`).
- **Release Automation**: Integrated a Thunderstore packaging script directly into MSBuild.
