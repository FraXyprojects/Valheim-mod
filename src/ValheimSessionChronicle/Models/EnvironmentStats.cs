using System;
using System.Collections.Generic;

namespace ValheimSessionChronicle.Models
{
    public sealed class EnvironmentStats
    {
        public int WeatherChanges { get; set; }
        public int DayTransitions { get; set; }
        public int NightTransitions { get; set; }
        public int PortalUses { get; set; }
        public int VisibleEnemyDeaths { get; set; }
        public List<string> WeatherSeen { get; set; } = new List<string>();
        public List<string> BiomesVisited { get; set; } = new List<string>();
        public List<string> OutpostBiomes { get; set; } = new List<string>();
        public List<string> BossesKilled { get; set; } = new List<string>();
        public Dictionary<string, DateTime> FirstBiomeVisitUtc { get; set; } = new Dictionary<string, DateTime>();
    }
}
