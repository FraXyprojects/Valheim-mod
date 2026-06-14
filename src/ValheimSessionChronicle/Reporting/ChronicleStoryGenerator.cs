using System;
using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Core;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Reporting.Analysis;
using ValheimSessionChronicle.Utility;
using ValheimSessionChronicle.WorldMemory;

namespace ValheimSessionChronicle.Reporting
{
    public sealed class ChronicleStoryGenerator
    {
        private readonly NarrativePhaseBuilder _phaseBuilder = new NarrativePhaseBuilder();

        // Local procedural story composition. No external service is used; the text is built from
        // grouped phases, intensity analysis, discoveries, camp classification, and survival stats.
        public string Generate(
            SessionData session,
            IReadOnlyList<SessionEvent> meaningfulEvents,
            CombatIntensityResult combat,
            SurvivalSummary survival,
            ExpeditionProfileResult profile,
            IReadOnlyList<CampCluster> camps,
            WorldMemoryData worldMemory,
            WorldMemoryUpdateResult memoryUpdate,
            ProgressionContext progression,
            DiscoveryAnalysis discovery)
        {
            List<string> paragraphs = new List<string>();
            string mainPlayer = GetMainPlayer(session);

            List<string> phases = _phaseBuilder.BuildPhases(session, combat, survival, camps, worldMemory, memoryUpdate, progression, discovery);
            if (phases.Count > 0)
            {
                paragraphs.Add(string.Join(" ", phases));
            }

            AddWorldMemoryParagraph(worldMemory, memoryUpdate, paragraphs);
            AddProgressionParagraph(progression, paragraphs);
            AddResourceOperationParagraph(discovery, paragraphs);
            AddDiscoveryParagraph(discovery, meaningfulEvents, paragraphs);
            AddPortalNetworkParagraph(session, worldMemory, paragraphs);
            AddProfileParagraph(profile, paragraphs);
            AddCombatDetails(session, mainPlayer, combat, paragraphs);
            AddSurvivalDetails(mainPlayer, survival, paragraphs);
            AddEndingParagraph(session, paragraphs);

            if (paragraphs.Count == 0)
            {
                paragraphs.Add($"{mainPlayer} odehrál klidnou session dlouhou {FormatDuration(session.Duration)} bez výrazných zlomů zachycených klientem.");
            }

            return string.Join(Environment.NewLine + Environment.NewLine, paragraphs);
        }

        private static void AddWorldMemoryParagraph(WorldMemoryData worldMemory, WorldMemoryUpdateResult memoryUpdate, ICollection<string> paragraphs)
        {
            if (memoryUpdate.CampChanges.Count == 0)
            {
                if (worldMemory.SessionCount > 1 && worldMemory.Camps.Count > 0)
                {
                    paragraphs.Add($"Kronika navazuje na dříve pozorovaný svět, ve kterém už je známo {worldMemory.Camps.Count} táborů nebo základen.");
                }

                return;
            }

            PersistentCampChange strongest = memoryUpdate.CampChanges
                .OrderByDescending(change => change.NewTier)
                .ThenByDescending(change => change.AddedStructures)
                .First();

            string biome = ChronicleFilters.IsValidBiome(strongest.Biome) ? $" v biomu {strongest.Biome}" : string.Empty;
            if (strongest.IsNewCamp)
            {
                paragraphs.Add($"Na mapě světa se objevil nový opěrný bod: {strongest.NewTierName}{biome}.");
                return;
            }

            if (strongest.IsTierUpgrade)
            {
                paragraphs.Add($"Dříve známý {strongest.PreviousTierName}{biome} se během session proměnil v {strongest.NewTierName}.");
                return;
            }

            List<string> additions = new List<string>();
            if (strongest.AddedForge)
            {
                additions.Add("kovárnu");
            }

            if (strongest.AddedDefenses)
            {
                additions.Add("obranné prvky");
            }

            if (strongest.AddedStorage)
            {
                additions.Add("skladování");
            }

            if (strongest.AddedPortal)
            {
                additions.Add("portál");
            }

            if (strongest.AddedAdvancedStation)
            {
                additions.Add("pokročilé řemeslné vybavení");
            }

            if (additions.Count > 0)
            {
                paragraphs.Add($"Skupina pokračovala v rozšiřování známého místa{biome} o {JoinCzech(additions)}.");
            }
            else if (strongest.AddedStructures > 0)
            {
                paragraphs.Add($"Známé zázemí{biome} bylo dál rozšířeno a upevněno.");
            }
        }

        private static void AddProgressionParagraph(ProgressionContext progression, ICollection<string> paragraphs)
        {
            if (progression == null || !progression.HasStrongEvidence)
            {
                return;
            }

            paragraphs.Add($"Z pozorovaných zásob, stanic a milníků působil svět nejvíc jako fáze {progression.DominantLabel}, takže jednotlivé nálezy dostaly váhu podle skutečného postupu světa.");
        }

        private static void AddResourceOperationParagraph(DiscoveryAnalysis discovery, ICollection<string> paragraphs)
        {
            if (discovery == null || discovery.ResourceOperations.Count == 0)
            {
                return;
            }

            List<ResourceOperation> operations = discovery.ResourceOperations.Take(3).ToList();
            paragraphs.Add(operations.Count == 1
                ? $"Výrazným motivem session byla operace: {operations[0].Summary}."
                : $"Vedle samotného průzkumu hrály roli i větší zásobovací operace: {JoinCzech(operations.Select(operation => operation.Summary).ToList())}.");
        }

        private static void AddDiscoveryParagraph(DiscoveryAnalysis discovery, IReadOnlyList<SessionEvent> meaningfulEvents, ICollection<string> paragraphs)
        {
            if (discovery != null && discovery.Discoveries.Count > 0)
            {
                List<string> analyzedDiscoveries = discovery.Discoveries
                    .Where(record => record.Tier >= DiscoveryValueTier.High)
                    .Select(record => record.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToList();

                if (analyzedDiscoveries.Count > 0)
                {
                    paragraphs.Add(analyzedDiscoveries.Count == 1
                        ? $"Nejvýraznějším progresním nálezem byl {analyzedDiscoveries[0]}."
                        : $"Nejvýraznější progresní nálezy představovaly {JoinCzech(analyzedDiscoveries)}.");
                    return;
                }
            }

            List<string> discoveries = meaningfulEvents
                .Where(entry => (entry.Type == EventTypes.Discovery || entry.Type == EventTypes.Crafting) &&
                                !string.IsNullOrWhiteSpace(entry.Target) &&
                                !ChronicleFilters.IsCommonResource(entry.Target))
                .Select(entry => entry.Target)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();

            if (discoveries.Count == 0)
            {
                return;
            }

            paragraphs.Add(discoveries.Count == 1
                ? $"Nejdůležitějším nálezem session byl {discoveries[0]}."
                : $"Mezi důležité nálezy a milníky patřily {JoinCzech(discoveries)}.");
        }

        private static void AddPortalNetworkParagraph(SessionData session, WorldMemoryData worldMemory, ICollection<string> paragraphs)
        {
            int portalUses = session.PlayerStats.Values.Sum(stats => stats.PortalUses);
            int knownPortals = worldMemory?.Portals.Count ?? 0;
            if (portalUses < 8 && knownPortals < 3)
            {
                return;
            }

            if (portalUses >= 14)
            {
                paragraphs.Add("Výprava se výrazně opírala o zavedenou portálovou síť, která propojovala aktivní zázemí a zkracovala návraty mezi jednotlivými cíli.");
            }
            else
            {
                paragraphs.Add("Časté portálové přesuny naznačily, že skupina pracovala s už rozvinutější sítí opěrných bodů.");
            }
        }

        private static void AddProfileParagraph(ExpeditionProfileResult profile, ICollection<string> paragraphs)
        {
            List<ExpeditionProfileScore> dominant = profile.DominantScores.ToList();
            if (dominant.Count == 0)
            {
                return;
            }

            paragraphs.Add("Charakter výpravy nejvíc určovaly tyto prvky: " +
                           string.Join(", ", dominant.Select(score => $"{score.Label.ToLowerInvariant()} ({score.Percentage} %)")) + ".");
        }

        private static void AddCombatDetails(SessionData session, string mainPlayer, CombatIntensityResult combat, ICollection<string> paragraphs)
        {
            Dictionary<string, int> enemyKills = MergeCounts(session.PlayerStats.Values.Select(stats => stats.EnemyKills));
            if (enemyKills.Count == 0)
            {
                return;
            }

            int totalKills = session.PlayerStats.Values.Sum(stats => stats.EnemiesKilled);
            int dangerousEncounters = session.PlayerStats.Values.Sum(stats => stats.DangerousEncounters);
            string combatPlace = ChronicleFilters.IsValidBiome(combat.DominantCombatBiome)
                ? $"v biomu {combat.DominantCombatBiome}"
                : "v nebezpečném terénu";

            if (combat.Tier == CombatIntensityTier.Extreme)
            {
                paragraphs.Add($"Bojová část měla brutální tempo: {combatPlace} se z výpravy stala série téměř nepřetržitých střetů, s {totalKills} potvrzenými zabitími a {dangerousEncounters} nebezpečnými momenty.");
                return;
            }

            if (combat.Tier == CombatIntensityTier.High)
            {
                paragraphs.Add($"Poklidný postup se zlomil v tvrdý boj o přežití; {combatPlace} skupina odolala výraznému tlaku nepřátel a zakončila session s {totalKills} potvrzenými zabitími.");
                return;
            }

            List<string> topKills = enemyKills
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .Take(IsAtLeast(combat.Tier, CombatIntensityTier.High) ? 2 : 3)
                .Select(pair => $"{pair.Value}x {pair.Key}")
                .ToList();

            if (IsAtLeast(combat.Tier, CombatIntensityTier.High))
            {
                paragraphs.Add($"{mainPlayer} a okolní skupina čelili dlouhému tlaku nepřátel; nejčastěji padali {JoinCzech(topKills)}.");
            }
            else
            {
                paragraphs.Add($"{mainPlayer} během cesty porazil hlavně {JoinCzech(topKills)}.");
            }
        }

        private static void AddSurvivalDetails(string mainPlayer, SurvivalSummary survival, ICollection<string> paragraphs)
        {
            if (!survival.HasHealthData)
            {
                return;
            }

            if (survival.HeroicEscapes > 0)
            {
                paragraphs.Add($"{mainPlayer} unikl téměř jisté smrti s pouhými {survival.LowestHealthPercent:P0} zdraví a přesto se dokázal vrátit do boje.");
                return;
            }

            if (survival.LastStandMoments > 0)
            {
                paragraphs.Add($"Nejostřejší střet vyústil v poslední odpor: i při kritickém zranění padli další nepřátelé a výprava se nerozpadla.");
                return;
            }

            if (survival.NearDeathEscapes > 0)
            {
                paragraphs.Add($"{mainPlayer} přežil kritický moment, kdy zdraví kleslo na {survival.LowestHealthPercent:P0}.");
                return;
            }

            switch (survival.StressTier)
            {
                case CombatIntensityTier.Extreme:
                    paragraphs.Add("Přežití samo se stalo největší výzvou session; tlak zásahů a nízkého zdraví držel výpravu dlouho na hraně.");
                    break;
                case CombatIntensityTier.High:
                    paragraphs.Add("Skupina ustála dlouhé bojové vypětí a několik tvrdých zásahů bez toho, aby ztratila tempo.");
                    break;
                case CombatIntensityTier.Medium:
                    paragraphs.Add("Nebezpečí se vracelo v několika vlnách, ale výprava ho zvládla bez skutečně kritického zlomu.");
                    break;
            }
        }

        private static void AddEndingParagraph(SessionData session, ICollection<string> paragraphs)
        {
            int deaths = session.PlayerStats.Values.Sum(stats => stats.Deaths);
            string duration = FormatDuration(session.Duration);

            if (deaths == 0)
            {
                paragraphs.Add($"Výprava skončila bez smrti po {duration} dobrodružství.");
            }
            else if (deaths == 1)
            {
                paragraphs.Add($"Výprava trvala {duration} a vyžádala si jedno úmrtí.");
            }
            else
            {
                paragraphs.Add($"Výprava trvala {duration} a vyžádala si {deaths} úmrtí.");
            }
        }

        private static string GetMainPlayer(SessionData session)
        {
            if (!string.IsNullOrWhiteSpace(session.LocalPlayerName))
            {
                return session.LocalPlayerName;
            }

            return session.Players.FirstOrDefault() ?? "Hráč";
        }

        private static Dictionary<string, int> MergeCounts(IEnumerable<Dictionary<string, int>> dictionaries)
        {
            Dictionary<string, int> merged = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, int> dictionary in dictionaries)
            {
                foreach (KeyValuePair<string, int> pair in dictionary)
                {
                    merged.TryGetValue(pair.Key, out int current);
                    merged[pair.Key] = current + pair.Value;
                }
            }

            return merged;
        }

        private static string JoinCzech(IReadOnlyList<string> values)
        {
            if (values.Count == 0)
            {
                return string.Empty;
            }

            if (values.Count == 1)
            {
                return values[0];
            }

            return string.Join(", ", values.Take(values.Count - 1)) + " a " + values.Last();
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            }

            return $"{Math.Max(0, duration.Minutes)}m {Math.Max(0, duration.Seconds)}s";
        }

        private static bool IsAtLeast(CombatIntensityTier actual, CombatIntensityTier expected)
        {
            return (int)actual >= (int)expected;
        }
    }
}
