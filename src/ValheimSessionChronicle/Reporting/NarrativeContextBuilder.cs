using System;
using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Reporting.Analysis;
using ValheimSessionChronicle.Utility;
using ValheimSessionChronicle.WorldMemory;
using ValheimSessionChronicle.Localization;

namespace ValheimSessionChronicle.Reporting
{
    public sealed class NarrativeContextBuilder
    {
        private readonly NarrativePhaseBuilder _phaseBuilder = new NarrativePhaseBuilder();

        public NarrativeContext Build(
            SessionData session,
            IReadOnlyList<SessionEvent> meaningfulEvents,
            CombatIntensityResult combat,
            SurvivalSummary survival,
            ExpeditionProfileResult profile,
            IReadOnlyList<CampCluster> camps,
            WorldMemoryData worldMemory,
            WorldMemoryUpdateResult memoryUpdate,
            ProgressionContext progression,
            DiscoveryAnalysis discovery,
            NarrativePacingResult pacing)
        {
            var context = new NarrativeContext();

            context.Phases = _phaseBuilder.BuildPhases(session, combat, survival, camps, worldMemory, memoryUpdate, progression, discovery);

            context.PacingTimeline = pacing.Windows.Select(w => LocalizationManager.GetString(w.State)).ToList();

            string mainPlayer = GetMainPlayer(session);

            BuildProgressionParagraph(progression, context);
            BuildResourceOperationParagraph(discovery, context);
            BuildDiscoveryParagraph(discovery, meaningfulEvents, context);
            BuildPortalNetworkParagraph(session, worldMemory, context);
            BuildProfileParagraph(profile, context);
            BuildCombatDetails(session, mainPlayer, combat, context);
            BuildSurvivalDetails(mainPlayer, survival, context);
            BuildEndingParagraph(session, context);

            return context;
        }

        private static string GetMainPlayer(SessionData session)
        {
            if (!string.IsNullOrWhiteSpace(session.LocalPlayerName))
            {
                return session.LocalPlayerName;
            }

            return session.Players.FirstOrDefault() ?? "Hráč";
        }

        private static void BuildProgressionParagraph(ProgressionContext progression, NarrativeContext context)
        {
            if (progression == null || !progression.HasStrongEvidence || string.IsNullOrWhiteSpace(progression.ProgressionShiftEvidence))
            {
                return;
            }
            context.ProgressionParagraph = progression.ProgressionShiftEvidence;
        }

        private static void BuildResourceOperationParagraph(DiscoveryAnalysis discovery, NarrativeContext context)
        {
            if (discovery == null || discovery.ResourceOperations.Count == 0)
            {
                return;
            }

            List<ResourceOperation> operations = discovery.ResourceOperations.Take(3).ToList();
            var summaries = string.Join(", ", operations.Select(o => o.Summary)); // Consider localizing later
            context.ResourceParagraph = operations.Count == 1
                ? $"Výrazným motivem session byla operace: {summaries}."
                : $"Vedle samotného průzkumu hrály roli i větší zásobovací operace: {summaries}.";
        }

        private static void BuildDiscoveryParagraph(DiscoveryAnalysis discovery, IReadOnlyList<SessionEvent> meaningfulEvents, NarrativeContext context)
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
                    context.DiscoveryParagraph = analyzedDiscoveries.Count == 1
                        ? $"Nejvýraznějším progresním nálezem byl {analyzedDiscoveries[0]}."
                        : $"Nejvýraznější progresní nálezy představovaly {string.Join(", ", analyzedDiscoveries)}.";
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

            if (discoveries.Count > 0)
            {
                context.DiscoveryParagraph = discoveries.Count == 1
                    ? $"Nejdůležitějším nálezem session byl {discoveries[0]}."
                    : $"Mezi důležité nálezy a milníky patřily {string.Join(", ", discoveries)}.";
            }
        }

        private static void BuildPortalNetworkParagraph(SessionData session, WorldMemoryData worldMemory, NarrativeContext context)
        {
            int portalUses = session.PlayerStats.Values.Sum(stats => stats.PortalUses);
            int knownPortals = worldMemory?.Portals.Count ?? 0;
            if (portalUses >= 14)
            {
                context.PortalNetworkParagraph = "Výprava se výrazně opírala o zavedenou portálovou síť, která propojovala aktivní zázemí a zkracovala návraty mezi jednotlivými cíli.";
            }
            else if (portalUses >= 8 || knownPortals >= 3)
            {
                context.PortalNetworkParagraph = "Časté portálové přesuny naznačily, že skupina pracovala s už rozvinutější sítí opěrných bodů.";
            }
        }

        private static void BuildProfileParagraph(ExpeditionProfileResult profile, NarrativeContext context)
        {
            List<ExpeditionProfileScore> dominant = profile.DominantScores.ToList();
            if (dominant.Count > 0)
            {
                context.ProfileParagraph = "Charakter výpravy nejvíc určovaly tyto prvky: " +
                           string.Join(", ", dominant.Select(score => $"{score.Label.ToLowerInvariant()} ({score.Percentage} %)")) + ".";
            }
        }

        private static void BuildCombatDetails(SessionData session, string mainPlayer, CombatIntensityResult combat, NarrativeContext context)
        {
            Dictionary<string, int> enemyKills = MergeCounts(session.PlayerStats.Values.Select(stats => stats.EnemyKills));
            if (enemyKills.Count == 0) return;

            int totalKills = session.PlayerStats.Values.Sum(stats => stats.EnemiesKilled);
            int dangerousEncounters = session.PlayerStats.Values.Sum(stats => stats.DangerousEncounters);
            string combatPlace = ChronicleFilters.IsValidBiome(combat.DominantCombatBiome)
                ? $"v biomu {combat.DominantCombatBiome}"
                : "v nebezpečném terénu";

            if (combat.Tier == CombatIntensityTier.Extreme)
            {
                context.CombatParagraph = $"Bojová část měla brutální tempo: {combatPlace} se z výpravy stala série téměř nepřetržitých střetů, s {totalKills} potvrzenými zabitími a {dangerousEncounters} nebezpečnými momenty.";
                return;
            }

            if (combat.Tier == CombatIntensityTier.High)
            {
                context.CombatParagraph = $"Poklidný postup se zlomil v tvrdý boj o přežití; {combatPlace} skupina odolala výraznému tlaku nepřátel a zakončila session s {totalKills} potvrzenými zabitími.";
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
                context.CombatParagraph = $"{mainPlayer} a okolní skupina čelili dlouhému tlaku nepřátel; nejčastěji padali {string.Join(", ", topKills)}.";
            }
            else
            {
                context.CombatParagraph = $"{mainPlayer} během cesty porazil hlavně {string.Join(", ", topKills)}.";
            }
        }

        private static void BuildSurvivalDetails(string mainPlayer, SurvivalSummary survival, NarrativeContext context)
        {
            if (!survival.HasHealthData) return;

            if (survival.HeroicEscapes > 0)
            {
                context.SurvivalParagraph = $"{mainPlayer} unikl téměř jisté smrti s pouhými {survival.LowestHealthPercent:P0} zdraví a přesto se dokázal vrátit do boje.";
                return;
            }

            if (survival.LastStandMoments > 0)
            {
                context.SurvivalParagraph = $"Nejostřejší střet vyústil v poslední odpor: i při kritickém zranění padli další nepřátelé a výprava se nerozpadla.";
                return;
            }

            if (survival.NearDeathEscapes > 0)
            {
                context.SurvivalParagraph = $"{mainPlayer} přežil kritický moment, kdy zdraví kleslo na {survival.LowestHealthPercent:P0}.";
                return;
            }

            switch (survival.StressTier)
            {
                case CombatIntensityTier.Extreme:
                    context.SurvivalParagraph = "Přežití samo se stalo největší výzvou session; tlak zásahů a nízkého zdraví držel výpravu dlouho na hraně.";
                    break;
                case CombatIntensityTier.High:
                    context.SurvivalParagraph = "Skupina ustála dlouhé bojové vypětí a několik tvrdých zásahů bez toho, aby ztratila tempo.";
                    break;
                case CombatIntensityTier.Medium:
                    context.SurvivalParagraph = "Nebezpečí se vracelo v několika vlnách, ale výprava ho zvládla bez skutečně kritického zlomu.";
                    break;
            }
        }

        private static void BuildEndingParagraph(SessionData session, NarrativeContext context)
        {
            int deaths = session.PlayerStats.Values.Sum(stats => stats.Deaths);
            string duration = FormatDuration(session.Duration);

            if (deaths == 0)
            {
                context.EndingParagraph = $"Výprava skončila bez smrti po {duration} dobrodružství.";
            }
            else if (deaths == 1)
            {
                context.EndingParagraph = $"Výprava trvala {duration} a vyžádala si jedno úmrtí.";
            }
            else
            {
                context.EndingParagraph = $"Výprava trvala {duration} a vyžádala si {deaths} úmrtí.";
            }
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
