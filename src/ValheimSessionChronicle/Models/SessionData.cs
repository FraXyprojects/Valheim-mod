using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ValheimSessionChronicle.Models
{
    public sealed class SessionData
    {
        public string SchemaVersion { get; set; } = "1.0";
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
        public string ModVersion { get; set; } = string.Empty;
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
        public string ServerName { get; set; } = "Dedicated Server";
        public string WorldName { get; set; } = string.Empty;
        public string LocalPlayerName { get; set; } = string.Empty;
        public bool IsMultiplayerClient { get; set; }
        public string DisconnectReason { get; set; } = string.Empty;
        public int ReconnectCount { get; set; }
        public DateTime LastWorldActivityUtc { get; set; }
        public DateTime LastConfirmedNetworkUtc { get; set; }
        public List<string> Players { get; set; } = new List<string>();
        public List<SessionEvent> Events { get; set; } = new List<SessionEvent>();
        public List<CombatActivitySample> CombatSamples { get; set; } = new List<CombatActivitySample>();
        public List<BuildActivitySample> BuildSamples { get; set; } = new List<BuildActivitySample>();
        public List<PortalActivitySample> PortalSamples { get; set; } = new List<PortalActivitySample>();
        public List<ProgressionStructureObservation> StructureObservations { get; set; } = new List<ProgressionStructureObservation>();
        public Dictionary<string, int> ObservedInventoryItems { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> ObservedContainerItems { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, PlayerStats> PlayerStats { get; set; } = new Dictionary<string, PlayerStats>(StringComparer.OrdinalIgnoreCase);
        public EnvironmentStats Environment { get; set; } = new EnvironmentStats();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        [JsonIgnore]
        public TimeSpan Duration
        {
            get
            {
                DateTime end = EndTimeUtc == default(DateTime) ? DateTime.UtcNow : EndTimeUtc;
                return end >= StartTimeUtc ? end - StartTimeUtc : TimeSpan.Zero;
            }
        }
    }
}
