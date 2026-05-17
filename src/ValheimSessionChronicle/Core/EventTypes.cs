namespace ValheimSessionChronicle.Core
{
    public static class EventCategories
    {
        public const string Session = "session";
        public const string Player = "player";
        public const string Environment = "environment";
        public const string Combat = "combat";
        public const string Building = "building";
        public const string Crafting = "crafting";
        public const string Progression = "progression";
        public const string Travel = "travel";
        public const string Loot = "loot";
    }

    public static class EventTypes
    {
        public const string SessionStarted = "session_started";
        public const string SessionEnded = "session_ended";
        public const string PlayerJoined = "player_joined";
        public const string PlayerDeath = "player_death";
        public const string PlayerRespawn = "player_respawn";
        public const string BiomeEntered = "biome_entered";
        public const string WeatherChanged = "weather_changed";
        public const string DayNightChanged = "day_night_changed";
        public const string BossKilled = "boss_killed";
        public const string EnemyKilled = "enemy_killed";
        public const string CombatMilestone = "combat_milestone";
        public const string CombatMoment = "combat_moment";
        public const string PortalUsed = "portal_used";
        public const string Sleeping = "sleeping";
        public const string ShipUsed = "ship_used";
        public const string Crafting = "crafting";
        public const string PiecePlaced = "piece_placed";
        public const string BuildingMilestone = "building_milestone";
        public const string WorkstationPlaced = "workstation_placed";
        public const string ItemPickedUp = "item_picked_up";
        public const string Discovery = "discovery";
        public const string TombstoneCreated = "tombstone_created";
    }
}
