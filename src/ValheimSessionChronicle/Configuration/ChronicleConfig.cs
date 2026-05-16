using BepInEx.Configuration;

namespace ValheimSessionChronicle.Configuration
{
    public sealed class ChronicleConfig
    {
        public ConfigEntry<bool> EnableDiscordWebhook { get; }
        public ConfigEntry<string> DiscordWebhookURL { get; }
        public ConfigEntry<bool> SaveJSON { get; }
        public ConfigEntry<bool> SaveTXT { get; }
        public ConfigEntry<bool> EnableVerboseLogging { get; }
        public ConfigEntry<bool> TrackEnvironment { get; }
        public ConfigEntry<bool> TrackCombat { get; }
        public ConfigEntry<bool> TrackBuilding { get; }
        public ConfigEntry<bool> TrackCrafting { get; }

        public ChronicleConfig(ConfigFile config)
        {
            SaveTXT = config.Bind(
                "Output",
                nameof(SaveTXT),
                true,
                "Save a Czech human-readable TXT report after disconnect.");

            SaveJSON = config.Bind(
                "Output",
                nameof(SaveJSON),
                true,
                "Save raw JSON session data after disconnect.");

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
                "Track local combat moments, visible enemy deaths, and visible boss deaths.");

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

            EnableDiscordWebhook = config.Bind(
                "Discord",
                nameof(EnableDiscordWebhook),
                false,
                "Send the generated TXT summary to a Discord webhook after disconnect.");

            DiscordWebhookURL = config.Bind(
                "Discord",
                nameof(DiscordWebhookURL),
                string.Empty,
                "Discord webhook URL. Leave empty unless EnableDiscordWebhook is enabled.");
        }
    }
}
