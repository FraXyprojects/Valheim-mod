using System;
using System.Collections.Generic;
using ValheimSessionChronicle.Models;

namespace ValheimSessionChronicle.Core
{
    public sealed class EventManager
    {
        private readonly Dictionary<string, DateTime> _lastEventByKey = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private SessionData _session;

        public void Begin(SessionData session)
        {
            _session = session;
            _lastEventByKey.Clear();
        }

        public void End()
        {
            _session = null;
            _lastEventByKey.Clear();
        }

        public SessionEvent Add(
            string type,
            string category,
            string description,
            string actor = null,
            string target = null,
            string biome = null,
            string position = null,
            EventImportance importance = EventImportance.Low,
            string duplicateKey = null,
            double duplicateCooldownSeconds = 0,
            Dictionary<string, string> metadata = null,
            string eventId = null,
            string[] eventArgs = null)
        {
            if (_session == null)
            {
                return null;
            }

            DateTime now = DateTime.UtcNow;
            double sessionSeconds = Math.Max(0, (now - _session.StartTimeUtc).TotalSeconds);

            if (!EventSanitizer.IsValid(type, category, description, actor, target, biome, sessionSeconds))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(duplicateKey) && duplicateCooldownSeconds > 0)
            {
                if (_lastEventByKey.TryGetValue(duplicateKey, out DateTime previous) &&
                    (now - previous).TotalSeconds < duplicateCooldownSeconds)
                {
                    return null;
                }

                _lastEventByKey[duplicateKey] = now;
            }

            string finalDescription = description ?? string.Empty;
            if (string.IsNullOrEmpty(finalDescription) && !string.IsNullOrEmpty(eventId))
            {
                finalDescription = ValheimSessionChronicle.Localization.LocalizationManager.GetString(eventId, eventArgs ?? new string[0]);
            }

            SessionEvent entry = new SessionEvent
            {
                TimestampUtc = now,
                SessionSeconds = sessionSeconds,
                Type = type,
                Category = category,
                Actor = actor ?? string.Empty,
                Target = target ?? string.Empty,
                Biome = biome ?? string.Empty,
                Position = position ?? string.Empty,
                Description = finalDescription,
                EventId = eventId ?? string.Empty,
                EventArgs = eventArgs ?? new string[0],
                Importance = importance,
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            _session.Events.Add(entry);

            ChronicleLogger.Verbose($"Event: {entry.Type} | {entry.Description}");
            return entry;
        }
    }
}
