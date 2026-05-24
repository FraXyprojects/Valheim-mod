using BepInEx.Configuration;

namespace ValheimSessionChronicle.Configuration
{
    public sealed class ChronicleConfig
    {
        public ConfigEntry<bool> EnableDiscordWebhook { get; }
        public ConfigEntry<string> DiscordWebhookURL { get; }
        public ConfigEntry<bool> SaveTXT { get; }
        public ConfigEntry<bool> EnableDebugJsonExport { get; }
        public ConfigEntry<bool> IncludeCompactTimeline { get; }
        public ConfigEntry<bool> EnableVerboseLogging { get; }
        public ConfigEntry<bool> TrackEnvironment { get; }
        public ConfigEntry<bool> TrackCombat { get; }
        public ConfigEntry<bool> TrackBuilding { get; }
        public ConfigEntry<bool> TrackCrafting { get; }
        public ConfigEntry<int> ReconnectToleranceSeconds { get; }
        public ConfigEntry<int> DisconnectDebounceSeconds { get; }

        public ChronicleConfig(ConfigFile config)
        {
            SaveTXT = config.Bind(
                "Output",
                nameof(SaveTXT),
                true,
                "Save one Czech human-readable TXT chronicle after disconnect.");

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
