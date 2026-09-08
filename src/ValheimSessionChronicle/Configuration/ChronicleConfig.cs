using BepInEx.Configuration;

namespace ValheimSessionChronicle.Configuration
{
    public sealed class ChronicleConfig
    {        public ConfigEntry<bool> EnableDiscordWebhook { get; }
        public ConfigEntry<string> DiscordWebhookURL { get; }
        public ConfigEntry<bool> SaveTXT { get; }
        public ConfigEntry<bool> SaveMarkdown { get; }
        public ConfigEntry<bool> SaveJSON { get; }
        public ConfigEntry<bool> SaveDiscord { get; }
        public ConfigEntry<bool> EnableDebugJsonExport { get; }
        public ConfigEntry<bool> IncludeCompactTimeline { get; }
        public ConfigEntry<bool> EnableVerboseLogging { get; }
        public ConfigEntry<bool> TrackEnvironment { get; }
        public ConfigEntry<bool> TrackCombat { get; }
        public ConfigEntry<bool> TrackBuilding { get; }
        public ConfigEntry<bool> TrackCrafting { get; }
        public ConfigEntry<int> ReconnectToleranceSeconds { get; }
        public ConfigEntry<int> DisconnectDebounceSeconds { get; }

        // Language
        public ConfigEntry<string> Language { get; }

        // UI
        public ConfigEntry<bool> ShowStatusIndicator { get; }
        public ConfigEntry<string> StatusIndicatorPosition { get; }
        public ConfigEntry<int> StatusIndicatorOffsetX { get; }
        public ConfigEntry<int> StatusIndicatorOffsetY { get; }

        // Narrative & History
        public ConfigEntry<string> NarrativeVerbosity { get; }
        public ConfigEntry<bool> EnableChapters { get; }
        public ConfigEntry<bool> EnableMoodAnalysis { get; }
        public ConfigEntry<bool> EnableArchetypeAnalysis { get; }
        public ConfigEntry<bool> EnablePacingAnalysis { get; }
        public ConfigEntry<bool> EnableEnvironmentalStorytelling { get; }
        public ConfigEntry<bool> EnableCombatAnalysis { get; }
        public ConfigEntry<bool> EnableHPAnalysis { get; }
        public ConfigEntry<bool> EnableCampAnalysis { get; }
        public ConfigEntry<bool> EnableChronicleHistory { get; }

        public ChronicleConfig(ConfigFile config)
        {            SaveTXT = config.Bind(
                "Output",
                nameof(SaveTXT),
                true,
                "Save one human-readable TXT chronicle after disconnect.");

            SaveMarkdown = config.Bind(
                "Output",
                nameof(SaveMarkdown),
                true,
                "Save one human-readable Markdown chronicle after disconnect.");

            SaveJSON = config.Bind(
                "Output",
                nameof(SaveJSON),
                true,
                "Save one canonical JSON chronicle after disconnect.");

            SaveDiscord = config.Bind(
                "Output",
                nameof(SaveDiscord),
                true,
                "Save one Discord-optimized Markdown snippet after disconnect.");

            Language = config.Bind(
                "General",
                nameof(Language),
                "Čeština",
                "Language of the generated reports (Čeština / English).");

            ShowStatusIndicator = config.Bind(
                "UI",
                nameof(ShowStatusIndicator),
                true,
                "Show an in-game UI indicator for the Chronicle state.");

            StatusIndicatorPosition = config.Bind(
                "UI",
                nameof(StatusIndicatorPosition),
                "TopRight",
                "Position of the UI indicator (TopLeft, TopCenter, TopRight, CenterLeft, CenterRight, BottomLeft, BottomCenter, BottomRight).");

            StatusIndicatorOffsetX = config.Bind(
                "UI",
                nameof(StatusIndicatorOffsetX),
                -10,
                "Horizontal offset of the UI indicator.");

            StatusIndicatorOffsetY = config.Bind(
                "UI",
                nameof(StatusIndicatorOffsetY),
                -10,
                "Vertical offset of the UI indicator.");

            NarrativeVerbosity = config.Bind(
                "Narrative",
                nameof(NarrativeVerbosity),
                "Normal",
                "Verbosity of the generated narrative (Compact / Normal / Rich).");

            EnableChapters = config.Bind(
                "Narrative",
                nameof(EnableChapters),
                true,
                "Generate narrative chapters for long sessions.");

            EnableMoodAnalysis = config.Bind(
                "Analysis",
                nameof(EnableMoodAnalysis),
                true,
                "Analyze and report the session mood.");

            EnableArchetypeAnalysis = config.Bind(
                "Analysis",
                nameof(EnableArchetypeAnalysis),
                true,
                "Analyze and report the dominant expedition archetype.");

            EnablePacingAnalysis = config.Bind(
                "Analysis",
                nameof(EnablePacingAnalysis),
                true,
                "Analyze and report the narrative pacing (flow and intensity over time).");

            EnableEnvironmentalStorytelling = config.Bind(
                "Narrative",
                nameof(EnableEnvironmentalStorytelling),
                true,
                "Include environmental factors (weather, biomes) in the story.");

            EnableCombatAnalysis = config.Bind(
                "Analysis",
                nameof(EnableCombatAnalysis),
                true,
                "Enable advanced combat intensity and encounter analysis.");

            EnableHPAnalysis = config.Bind(
                "Analysis",
                nameof(EnableHPAnalysis),
                true,
                "Enable client-side survival and near-death analysis.");

            EnableCampAnalysis = config.Bind(
                "Analysis",
                nameof(EnableCampAnalysis),
                true,
                "Enable advanced camp tier classification and structure clustering.");

            EnableChronicleHistory = config.Bind(
                "Output",
                nameof(EnableChronicleHistory),
                true,
                "Maintain a ChronicleHistory.md file accumulating all sessions.");

            EnableDebugJsonExport = config.Bind(
                "Debug",
                nameof(EnableDebugJsonExport),
                false,
                "Save raw debug JSON session data after disconnect. Disabled by default to keep one clean TXT chronicle per session.");

            IncludeCompactTimeline = config.Bind(
                "Output",
                nameof(IncludeCompactTimeline),
                true,
                "Include a compact timeline of medium and high importance moments in the final TXT chronicle.");

            EnableVerboseLogging = config.Bind(
                "Diagnostics",
                nameof(EnableVerboseLogging),
                false,
                "Enable extra BepInEx log messages for troubleshooting hooks.");

            TrackEnvironment = config.Bind(
                "Tracking",
                nameof(TrackEnvironment),
                true,
                "Track biome, weather, and day/night transitions.");

            TrackCombat = config.Bind(
                "Tracking",
                nameof(TrackCombat),
                true,
                "Track aggregated combat statistics, dangerous encounters, visible enemy deaths, and visible boss deaths.");

            TrackBuilding = config.Bind(
                "Tracking",
                nameof(TrackBuilding),
                true,
                "Track local building and workstation placement hooks.");

            TrackCrafting = config.Bind(
                "Tracking",
                nameof(TrackCrafting),
                true,
                "Track local crafting hooks.");

            ReconnectToleranceSeconds = config.Bind(
                "Session",
                nameof(ReconnectToleranceSeconds),
                90,
                "How long a temporary ZNet/player loss may last before the session is finalized. Recommended range: 60-120 seconds.");

            DisconnectDebounceSeconds = config.Bind(
                "Session",
                nameof(DisconnectDebounceSeconds),
                10,
                "Short debounce window before treating a missing player/ZNet as temporary connection loss.");

            EnableDiscordWebhook = config.Bind(
                "Discord",
                nameof(EnableDiscordWebhook),
                false,
                "Send the TXT session summary to a Discord webhook after disconnect.");

            DiscordWebhookURL = config.Bind(
                "Discord",
                nameof(DiscordWebhookURL),
                string.Empty,
                "Discord webhook URL. Leave empty unless EnableDiscordWebhook is enabled.");
        }
    }
}
