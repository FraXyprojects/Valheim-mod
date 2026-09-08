using System;
using System.Collections.Generic;
using ValheimSessionChronicle.Configuration;

namespace ValheimSessionChronicle.Localization
{
    public static class LocalizationManager
    {
        private static Language _currentLanguage = Language.Czech;

        private static readonly Dictionary<string, Dictionary<Language, string>> _strings = new Dictionary<string, Dictionary<Language, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["SessionStarted"] = new Dictionary<Language, string> { { Language.Czech, "Session začala na serveru {0}." }, { Language.English, "Session started on server {0}." } },
            ["PlayerJoined"] = new Dictionary<Language, string> { { Language.Czech, "{0} se připojil k serveru." }, { Language.English, "{0} joined the server." } },
            ["SessionEnded"] = new Dictionary<Language, string> { { Language.Czech, "Hráč byl odpojen. Důvod: {0}" }, { Language.English, "Player disconnected. Reason: {0}" } },
            ["TombstoneCreated"] = new Dictionary<Language, string> { { Language.Czech, "Byl vytvořen náhrobek hráče {0}." }, { Language.English, "Tombstone created for player {0}." } },
            ["FoodEaten"] = new Dictionary<Language, string> { { Language.Czech, "{0} poprvé pojedl: {1}." }, { Language.English, "{0} ate for the first time: {1}." } },
                        ["AnimalTamed"] = new Dictionary<Language, string> { { Language.Czech, "{0} úspěšně ochočil zvíře: {1}." }, { Language.English, "{0} successfully tamed animal: {1}." } },

            // Moods
            ["Mood_Desperate"] = new Dictionary<Language, string> { { Language.Czech, "Zoufalá" }, { Language.English, "Desperate" } },
            ["Mood_Brutal"] = new Dictionary<Language, string> { { Language.Czech, "Brutální" }, { Language.English, "Brutal" } },
            ["Mood_Triumphant"] = new Dictionary<Language, string> { { Language.Czech, "Triumfální" }, { Language.English, "Triumphant" } },
            ["Mood_Tense"] = new Dictionary<Language, string> { { Language.Czech, "Napjatá" }, { Language.English, "Tense" } },
            ["Mood_Building"] = new Dictionary<Language, string> { { Language.Czech, "Stavitelská" }, { Language.English, "Building" } },
            ["Mood_Exploration"] = new Dictionary<Language, string> { { Language.Czech, "Průzkumná" }, { Language.English, "Exploratory" } },
            ["Mood_Calm"] = new Dictionary<Language, string> { { Language.Czech, "Klidná" }, { Language.English, "Calm" } },

            // Phases
            ["Phase_Arrival"] = new Dictionary<Language, string> { { Language.Czech, "Příjezd a příprava" }, { Language.English, "Arrival & Preparation" } },
            ["Phase_Exploration"] = new Dictionary<Language, string> { { Language.Czech, "Průzkum" }, { Language.English, "Exploration" } },
            ["Phase_Building"] = new Dictionary<Language, string> { { Language.Czech, "Budování zázemí" }, { Language.English, "Base Building" } },
            ["Phase_IntenseCombat"] = new Dictionary<Language, string> { { Language.Czech, "Intenzivní boj" }, { Language.English, "Intense Combat" } },
            ["Phase_Combat"] = new Dictionary<Language, string> { { Language.Czech, "Boj" }, { Language.English, "Combat" } },
            ["Phase_BossFight"] = new Dictionary<Language, string> { { Language.Czech, "Střet s bossem" }, { Language.English, "Boss Fight" } },
            ["Phase_Conclusion"] = new Dictionary<Language, string> { { Language.Czech, "Návrat a zhodnocení" }, { Language.English, "Return & Conclusion" } },

        };

        public static void Initialize(ChronicleConfig config)
        {
            if (string.Equals(config.Language.Value, "English", StringComparison.OrdinalIgnoreCase))
            {
                _currentLanguage = Language.English;
            }
            else
            {
                _currentLanguage = Language.Czech;
            }
        }

        public static string GetString(string key, params object[] args)
        {
            if (_strings.TryGetValue(key, out var translations))
            {
                if (translations.TryGetValue(_currentLanguage, out string text))
                {
                    if (args != null && args.Length > 0)
                    {
                        try
                        {
                            return string.Format(text, args);
                        }
                        catch
                        {
                            return text;
                        }
                    }
                    return text;
                }
            }

            // Fallback to key itself if not found
            if (args != null && args.Length > 0)
            {
                return $"{key}: {string.Join(", ", args)}";
            }
            return key;
        }
    }
}
