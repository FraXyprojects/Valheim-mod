using System.Collections.Generic;

namespace ValheimSessionChronicle.Models
{
    public sealed class PlayerStats
    {
        public string PlayerName { get; set; } = string.Empty;
        public int Deaths { get; set; }
        public int Respawns { get; set; }
        public int PortalUses { get; set; }
        public int ShipUses { get; set; }
        public int SleepingEvents { get; set; }
        public int ItemsPickedUp { get; set; }
        public int Crafts { get; set; }
        public int PiecesPlacedTotal { get; set; }
        public int WorkstationsPlaced { get; set; }
        public int CombatMoments { get; set; }
        public int DangerousEncounters { get; set; }
        public int EnemiesKilled { get; set; }
        public int BossesKilled { get; set; }
        public int TombstonesCreated { get; set; }
        public List<string> BiomesVisited { get; set; } = new List<string>();
        public Dictionary<string, int> ItemPickups { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> CraftedItems { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> PiecesPlaced { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> EnemyKills { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, Dictionary<string, int>> EnemyKillsByBiome { get; set; } = new Dictionary<string, Dictionary<string, int>>();
        public Dictionary<string, int> DangerousEncountersByBiome { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> BossKills { get; set; } = new Dictionary<string, int>();
    }
}
