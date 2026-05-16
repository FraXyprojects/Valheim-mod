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
        public List<string> Players { get; set; } = new List<string>();
        public List<SessionEvent> Events { get; set; } = new List<SessionEvent>();
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
