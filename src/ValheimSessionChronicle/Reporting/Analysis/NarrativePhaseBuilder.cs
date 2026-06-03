using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Utility;
using ValheimSessionChronicle.WorldMemory;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class NarrativePhaseBuilder
    {
        public List<string> BuildPhases(
            SessionData session,
            CombatIntensityResult combat,
            SurvivalSummary survival,
            IReadOnlyList<CampCluster> camps,
            WorldMemoryData worldMemory = null,
            WorldMemoryUpdateResult memoryUpdate = null,
            ProgressionContext progression = null,
            DiscoveryAnalysis discovery = null)
        {
            List<string> phases = new List<string>();
            AddProgressionPhase(progression, phases);
            AddExplorationPhase(session, phases);
            if (!AddPersistentCampPhase(memoryUpdate, phases))
            {
                AddCampPhase(camps, phases);
            }

            AddResourceOperationPhase(discovery, phases);
            AddCombatPhase(combat, phases);
            AddSurvivalPhase(session, survival, combat, phases);
            AddBossPhase(session, survival, phases);
            return phases;
        }

        private static void AddProgressionPhase(ProgressionContext progression, ICollection<string> phases)
        {
            if (progression == null || !progression.HasStrongEvidence || progression.DominantStage < ProgressionStage.Swamp)
            {
                return;
            }

            phases.Add($"Podle zachycených zásob a stanic už svět nese znaky fáze {progression.DominantLabel}.");
        }

        private static void AddExplorationPhase(SessionData session, ICollection<string> phases)
        {
            List<string> biomes = session.Environment.BiomesVisited
                .Where(ChronicleFilters.IsValidBiome)
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (biomes.Count >= 2)
            {
                phases.Add($"Výprava se postupně přesunula z {biomes.First()} až do {biomes.Last()}, takže měla jasný průzkumný oblouk.");
            }
            else if (biomes.Count == 1)
            {
                phases.Add($"Hlavním dějištěm výpravy byl biome {biomes[0]}.");
            }
        }

        private static void AddCampPhase(IReadOnlyList<CampCluster> camps, ICollection<string> phases)
        {
            CampCluster strongest = camps.FirstOrDefault();
            if (strongest == null)
            {
                return;
            }

            string biome = ChronicleFilters.IsValidBiome(strongest.Biome) ? $" v biomu {strongest.Biome}" : string.Empty;
            phases.Add($"Stavební část výpravy vyústila v objekt typu {strongest.Name}{biome}, se zhruba {strongest.StructureCount} postavenými díly.");
        }

        private static bool AddPersistentCampPhase(WorldMemoryUpdateResult memoryUpdate, ICollection<string> phases)
        {
            PersistentCampChange change = memoryUpdate?.CampChanges
                .OrderByDescending(entry => entry.IsTierUpgrade)
                .ThenByDescending(entry => entry.NewTier)
                .ThenByDescending(entry => entry.AddedStructures)
                .FirstOrDefault();

            if (change == null)
            {
                return false;
            }

            string biome = ChronicleFilters.IsValidBiome(change.Biome) ? $" v biomu {change.Biome}" : string.Empty;
            if (change.IsNewCamp)
            {
                phases.Add($"Na mapě přibyl nový opěrný bod: {change.NewTierName}{biome}.");
            }
            else if (change.IsTierUpgrade)
            {
                phases.Add($"Dříve známý {change.PreviousTierName}{biome} se posunul na úroveň {change.NewTierName}.");
            }
            else
            {
                phases.Add($"Známé zázemí{biome} bylo během session dál rozšířeno a upevněno.");
            }

            return true;
        }

        private static void AddResourceOperationPhase(DiscoveryAnalysis discovery, ICollection<string> phases)
        {
            ResourceOperation operation = discovery?.ResourceOperations.FirstOrDefault();
            if (operation == null)
            {
                return;
            }

            phases.Add($"Zásobovací linku session nejvíc určovala oblast '{operation.OperationType}'.");
        }

        private static void AddCombatPhase(CombatIntensityResult combat, ICollection<string> phases)
        {
            switch (combat.Tier)
            {
                case CombatIntensityTier.Extreme:
                    string place = ChronicleFilters.IsValidBiome(combat.DominantCombatBiome) ? combat.DominantCombatBiome : "nepřátelském území";
                    phases.Add($"{place} se proměnil v dlouhou sérii brutálních střetů, ve kterých skupina bojovala téměř nepřetržitě.");
                    break;
                case CombatIntensityTier.High:
                    phases.Add("Poklidná výprava se postupně změnila v intenzivní boj o přežití.");
                    break;
                case CombatIntensityTier.Medium:
                    phases.Add("Skupina během postupu několikrát narazila na odpor místních nepřátel.");
                    break;
                default:
                    phases.Add("Výprava se nesla hlavně ve znamení průzkumu a přesunů mezi biomy.");
                    break;
            }
        }

        private static void AddSurvivalPhase(SessionData session, SurvivalSummary survival, CombatIntensityResult combat, ICollection<string> phases)
        {
            if (survival.HasHealthData && survival.NearDeathMoments > 0)
            {
                phases.Add("Nejméně jednou se situace zlomila na hraně přežití a FraXson unikl smrti jen s minimem sil.");
                return;
            }

            int deaths = session.PlayerStats.Values.Sum(stats => stats.Deaths);
            if (deaths == 0 && (int)combat.Tier >= (int)CombatIntensityTier.High)
            {
                phases.Add("Navzdory silnému tlaku výprava přežila bez ztráty života.");
            }
            else if (deaths > 0)
            {
                phases.Add("Výprava měla i fázi obnovy po smrti, po které bylo potřeba znovu získat tempo.");
            }
        }

        private static void AddBossPhase(SessionData session, SurvivalSummary survival, ICollection<string> phases)
        {
            if (session.Environment.BossesKilled.Count == 0)
            {
                return;
            }

            if (survival.Deaths == 0)
            {
                phases.Add("Boss byl poražen bez jediné zaznamenané smrti.");
            }
            else
            {
                phases.Add("Boss fight se stal jedním z hlavních zlomů celé session.");
            }
        }
    }
}
