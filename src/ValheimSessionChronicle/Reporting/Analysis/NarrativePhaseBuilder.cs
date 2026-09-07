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
            AddSurvivalPhase(session, survival, phases);
            AddBossPhase(session, survival, phases);
            return phases;
        }

        private static void AddProgressionPhase(ProgressionContext progression, ICollection<string> phases)
        {
            if (progression == null || !progression.HasStrongEvidence || progression.DominantStage < ProgressionStage.Swamp)
            {
                return;
            }

            phases.Add(NarrativeDatabase.GetProgressionPhase(progression.DominantLabel));
        }

        private static void AddExplorationPhase(SessionData session, ICollection<string> phases)
        {
            List<string> biomes = session.Environment.BiomesVisited
                .Where(ChronicleFilters.IsValidBiome)
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (biomes.Count >= 2)
            {
                phases.Add(NarrativeDatabase.GetMultiBiomeExploration(biomes.First(), biomes.Last()));
            }
            else if (biomes.Count == 1)
            {
                phases.Add(NarrativeDatabase.GetSingleBiomeExploration(biomes[0]));
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
            phases.Add(NarrativeDatabase.GetCampGeneric(strongest.Name, biome, strongest.StructureCount));
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
                phases.Add(NarrativeDatabase.GetCampNew(change.NewTierName, biome));
            }
            else if (change.IsTierUpgrade)
            {
                phases.Add(NarrativeDatabase.GetCampUpgrade(change.PreviousTierName, biome, change.NewTierName));
            }
            else if (change.AddedAdvancedStation || change.AddedForge)
            {
                phases.Add(NarrativeDatabase.GetCampAddedAdvancedStation(biome));
            }
            else if (change.AddedDefenses)
            {
                phases.Add(NarrativeDatabase.GetCampAddedDefenses(biome));
            }
            else
            {
                phases.Add(NarrativeDatabase.GetCampExpanded(biome));
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

            phases.Add(NarrativeDatabase.GetResourceOperation(operation.OperationType));
        }

        private static void AddCombatPhase(CombatIntensityResult combat, ICollection<string> phases)
        {
            switch (combat.Tier)
            {
                case CombatIntensityTier.Extreme:
                    phases.Add(NarrativeDatabase.GetCombatExtreme());
                    break;
                case CombatIntensityTier.High:
                    phases.Add(NarrativeDatabase.GetCombatHigh());
                    break;
                case CombatIntensityTier.Medium:
                    phases.Add(NarrativeDatabase.GetCombatMedium());
                    break;
                default:
                    phases.Add(NarrativeDatabase.GetCombatLow());
                    break;
            }
        }

        private static void AddSurvivalPhase(SessionData session, SurvivalSummary survival, ICollection<string> phases)
        {
            string playerName = string.IsNullOrWhiteSpace(session.LocalPlayerName) ? "Hráč" : session.LocalPlayerName;

            if (survival.HasHealthData && survival.HeroicEscapes > 0)
            {
                phases.Add(NarrativeDatabase.GetSurvivalHeroicEscape(playerName));
                return;
            }

            if (survival.HasHealthData && survival.LastStandMoments > 0)
            {
                phases.Add(NarrativeDatabase.GetSurvivalLastStand(playerName));
                return;
            }

            if (survival.HasHealthData && survival.NearDeathEscapes > 0)
            {
                phases.Add(NarrativeDatabase.GetSurvivalNearDeath(playerName, survival.LowestHealthPercent.ToString("P0")));
                return;
            }

            int deaths = session.PlayerStats.Values.Sum(stats => stats.Deaths);
            if (deaths == 0 && (int)survival.StressTier >= (int)CombatIntensityTier.High)
            {
                phases.Add(NarrativeDatabase.GetSurvivalNoDeathsHighStress());
            }
            else if ((int)survival.StressTier >= (int)CombatIntensityTier.Medium)
            {
                phases.Add(NarrativeDatabase.GetSurvivalMediumStress());
            }
            else if (deaths > 0)
            {
                phases.Add(NarrativeDatabase.GetSurvivalWithDeaths());
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
                phases.Add(NarrativeDatabase.GetBossNoDeath());
            }
            else
            {
                phases.Add(NarrativeDatabase.GetBossWithDeath());
            }
        }
    }
}
