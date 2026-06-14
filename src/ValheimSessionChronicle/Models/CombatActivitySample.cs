using System;

namespace ValheimSessionChronicle.Models
{
    public sealed class CombatActivitySample
    {
        public DateTime TimestampUtc { get; set; }
        public string Actor { get; set; } = string.Empty;
        public string EnemyName { get; set; } = string.Empty;
        public string Biome { get; set; } = string.Empty;
        public bool IsKill { get; set; }
        public bool IsEliteEnemy { get; set; }
        public bool IsBoss { get; set; }
        public bool IsIncomingDamage { get; set; }
        public float IncomingDamage { get; set; }
        public string DamageType { get; set; } = string.Empty;
        public float HealthAfterHit { get; set; }
        public float MaxHealth { get; set; }
        public float HealthPercent { get; set; }
        public float DamagePercentOfMaxHealth { get; set; }
        public bool IsNearDeath { get; set; }
        public bool CausedDeath { get; set; }
    }
}
