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

            // Pacing
            ["Pacing_Calm"] = new Dictionary<Language, string> { { Language.Czech, "Klidná" }, { Language.English, "Calm" } },
            ["Pacing_Activity"] = new Dictionary<Language, string> { { Language.Czech, "Aktivita" }, { Language.English, "Activity" } },
            ["Pacing_Exploration"] = new Dictionary<Language, string> { { Language.Czech, "Průzkum" }, { Language.English, "Exploration" } },
            ["Pacing_Building"] = new Dictionary<Language, string> { { Language.Czech, "Budování" }, { Language.English, "Building" } },
            ["Pacing_Combat"] = new Dictionary<Language, string> { { Language.Czech, "Boj" }, { Language.English, "Combat" } },
            ["Pacing_CombatPeak"] = new Dictionary<Language, string> { { Language.Czech, "Intenzivní boj" }, { Language.English, "Intense Combat" } },
            ["Pacing_BossCombat"] = new Dictionary<Language, string> { { Language.Czech, "Souboj s bossem" }, { Language.English, "Boss Combat" } },

            // Narrative Database
            ["SingleBiomeExploration"] = new Dictionary<Language, string> { { Language.Czech, "Hlavním dějištěm této výpravy se stal biom {0}." }, { Language.English, "The main stage for this expedition was the {0} biome." } },
            ["MultiBiomeExploration"] = new Dictionary<Language, string> { { Language.Czech, "Výprava se tentokrát vydala na dlouhou cestu; od {0} až do {1}, takže měla jasný průzkumný oblouk." }, { Language.English, "The expedition went on a long journey, from {0} to {1}, creating a clear exploration arc." } },
            ["CampNew"] = new Dictionary<Language, string> { { Language.Czech, "Na mapě přibyl nový opěrný bod: {0}{1}." }, { Language.English, "A new foothold was established on the map: {0}{1}." } },
            ["CampUpgrade"] = new Dictionary<Language, string> { { Language.Czech, "Dříve známý {0}{1} se posunul na úroveň {2}." }, { Language.English, "The previously known {0}{1} was upgraded to {2}." } },
            ["CampAddedAdvancedStation"] = new Dictionary<Language, string> { { Language.Czech, "Staré zázemí{0} dostalo další řemeslnou infrastrukturu a začalo působit jako důležitější základna." }, { Language.English, "The old camp{0} received further crafting infrastructure and started acting as a more important base." } },
            ["CampAddedDefenses"] = new Dictionary<Language, string> { { Language.Czech, "Výprava investovala čas do opevnění a obrany své staré pozice{0}." }, { Language.English, "The expedition invested time in fortifying and defending its old position{0}." } },
            ["CampExpanded"] = new Dictionary<Language, string> { { Language.Czech, "Stará základna{0} se dočkala strukturálního rozšíření, i když nedosáhla nové úrovně." }, { Language.English, "The old base{0} received structural expansion, even if it didn't reach a new tier." } },
            ["CampGeneric"] = new Dictionary<Language, string> { { Language.Czech, "Důležitým opěrným bodem pro další postup se stal {0}{1} (zahrnující {2} struktur)." }, { Language.English, "An important foothold for further progress was {0}{1} (comprising {2} structures)." } },
            ["CombatExtreme"] = new Dictionary<Language, string> { { Language.Czech, "Postup kupředu vyžadoval obrovskou daň; skupina prošla extrémním střetem, který prověřil všechny její limity." }, { Language.English, "Pushing forward demanded a huge toll; the group went through an extreme clash that tested all its limits." } },
            ["CombatHigh"] = new Dictionary<Language, string> { { Language.Czech, "Výprava se musela probojovat skrz tuhý odpor a zvládla několik těžkých šarvátek za sebou." }, { Language.English, "The expedition had to fight its way through stiff resistance, surviving several heavy skirmishes in a row." } },
            ["CombatMedium"] = new Dictionary<Language, string> { { Language.Czech, "Boj nebyl výjimečně těžký, ale skupina se několikrát musela bránit organizovanému útoku." }, { Language.English, "The combat was not exceptionally difficult, but the group had to defend against organized attacks several times." } },
            ["CombatLow"] = new Dictionary<Language, string> { { Language.Czech, "Z hlediska boje byla výprava spíše klidná; nepřátelé nepředstavovali větší hrozbu." }, { Language.English, "In terms of combat, the expedition was rather peaceful; enemies posed no major threat." } },
            ["SurvivalHeroicEscape"] = new Dictionary<Language, string> { { Language.Czech, "Během výpravy předvedl {0} heroický útěk, když unikl jisté smrti na poslední chvíli." }, { Language.English, "During the expedition, {0} pulled off a heroic escape, evading certain death at the last moment." } },
            ["SurvivalLastStand"] = new Dictionary<Language, string> { { Language.Czech, "Situace eskalovala až k dramatickému poslednímu vzdoru, při kterém {0} bojoval o holý život." }, { Language.English, "The situation escalated into a dramatic last stand, where {0} fought for their bare life." } },
            ["SurvivalNearDeath"] = new Dictionary<Language, string> { { Language.Czech, "{0} jen těsně unikl smrti (zdraví kleslo na {1})." }, { Language.English, "{0} narrowly escaped death (health dropped to {1})." } },
            ["SurvivalNoDeathsHighStress"] = new Dictionary<Language, string> { { Language.Czech, "Navzdory obrovskému tlaku přežila skupina všechny střety bez ztrát na životech." }, { Language.English, "Despite immense pressure, the group survived all encounters with no casualties." } },
            ["SurvivalMediumStress"] = new Dictionary<Language, string> { { Language.Czech, "I přes občasné krizové situace si výprava udržela kontrolu nad situací a nikdo nezemřel." }, { Language.English, "Despite occasional crisis situations, the expedition maintained control and no one died." } },
            ["SurvivalWithDeaths"] = new Dictionary<Language, string> { { Language.Czech, "Náročnost výpravy si vybrala svou daň a vyžádala si oběti na životech." }, { Language.English, "The difficulty of the expedition took its toll and resulted in casualties." } },
            ["BossNoDeath"] = new Dictionary<Language, string> { { Language.Czech, "Skupina dokázala porazit silného bosse bez jediné smrti. Skvělý výkon!" }, { Language.English, "The group managed to defeat a powerful boss without a single death. Excellent performance!" } },
            ["BossWithDeath"] = new Dictionary<Language, string> { { Language.Czech, "Boj s bossem byl brutální a vyžádal si životy, ale nakonec byl úspěšný." }, { Language.English, "The boss fight was brutal and claimed lives, but was ultimately successful." } },
            ["ProgressionPhase"] = new Dictionary<Language, string> { { Language.Czech, "Výprava znamenala důležitý krok v celkovém postupu érou: {0}." }, { Language.English, "The expedition marked an important step in the overall progression of the era: {0}." } },
            ["ResourceOperationPhase"] = new Dictionary<Language, string> { { Language.Czech, "Značná část úsilí byla věnována shromažďování surovin: {0}." }, { Language.English, "A significant part of the effort was dedicated to gathering resources: {0}." } },
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
