using System;
using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Core;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Reporting.Analysis;
using ValheimSessionChronicle.Utility;

namespace ValheimSessionChronicle.WorldMemory
{
    public sealed class WorldMemoryManager
    {
        private const float CampMergeRadius = 80f;

        private readonly CampClassificationSystem _campClassification = new CampClassificationSystem();

        public WorldMemoryUpdateResult UpdateMemory(WorldMemoryData memory, SessionData session)
        {
            EnsureCollections(memory);
            WorldMemoryUpdateResult result = SnapshotKnownState(memory);

            memory.SessionCount++;
            memory.LastSessionId = session.SessionId;
            memory.UpdatedUtc = DateTime.UtcNow;

            UpdateBiomes(memory, session);
            UpdateImportantItems(memory, session);
            UpdateBosses(memory, session);
            UpdateCamps(memory, session, result);
            UpdatePortals(memory, session, result);
            UpdateImportantStructures(memory, session, result);

            Trim(memory);
            return result;
        }

        private static void EnsureCollections(WorldMemoryData memory)
        {
            memory.Camps = memory.Camps ?? new List<PersistentCampCluster>();
            memory.Portals = memory.Portals ?? new List<PersistentPortalRecord>();
            memory.ImportantStructures = memory.ImportantStructures ?? new List<PersistentStructureRecord>();
            memory.DiscoveredBiomes = memory.DiscoveredBiomes ?? new List<string>();
            memory.ImportantItems = memory.ImportantItems ?? new List<string>();
            memory.BossesDefeated = memory.BossesDefeated ?? new List<string>();
            memory.ProgressionObservations = memory.ProgressionObservations ?? new List<WorldProgressionObservation>();

            foreach (PersistentCampCluster camp in memory.Camps)
            {
                camp.StationTypes = camp.StationTypes ?? new List<string>();
                camp.PortalNames = camp.PortalNames ?? new List<string>();
                camp.TierHistory = camp.TierHistory ?? new List<PersistentCampTierObservation>();
            }
        }

        private WorldMemoryUpdateResult SnapshotKnownState(WorldMemoryData memory)
        {
            return new WorldMemoryUpdateResult
            {
                PreviouslyKnownBiomes = new HashSet<string>(memory.DiscoveredBiomes, StringComparer.OrdinalIgnoreCase),
                PreviouslyKnownImportantItems = new HashSet<string>(memory.ImportantItems, StringComparer.OrdinalIgnoreCase),
                PreviouslyKnownBosses = new HashSet<string>(memory.BossesDefeated, StringComparer.OrdinalIgnoreCase),
                PreviouslyKnownImportantStructures = new HashSet<string>(
                    memory.ImportantStructures.Select(entry => entry.StructureName),
                    StringComparer.OrdinalIgnoreCase)
            };
        }

        private static void UpdateBiomes(WorldMemoryData memory, SessionData session)
        {
            foreach (string biome in session.Environment.BiomesVisited.Where(ChronicleFilters.IsValidBiome))
            {
                AddUnique(memory.DiscoveredBiomes, biome);
            }
        }

        private static void UpdateImportantItems(WorldMemoryData memory, SessionData session)
        {
            IEnumerable<string> observedItems = session.PlayerStats.Values.SelectMany(stats => stats.ItemPickups.Keys)
                .Concat(session.PlayerStats.Values.SelectMany(stats => stats.CraftedItems.Keys))
                .Concat(session.ObservedInventoryItems.Keys)
                .Concat(session.ObservedContainerItems.Keys);

            foreach (string item in observedItems.Where(ValheimNames.IsImportantItem))
            {
                if (AddUnique(memory.ImportantItems, item))
                {
                    memory.ProgressionObservations.Add(new WorldProgressionObservation
                    {
                        TimestampUtc = DateTime.UtcNow,
                        SessionId = session.SessionId,
                        Type = "item",
                        Name = item
                    });
                }
            }
        }

        private static void UpdateBosses(WorldMemoryData memory, SessionData session)
        {
            foreach (string boss in session.Environment.BossesKilled)
            {
                if (AddUnique(memory.BossesDefeated, boss))
                {
                    memory.ProgressionObservations.Add(new WorldProgressionObservation
                    {
                        TimestampUtc = DateTime.UtcNow,
                        SessionId = session.SessionId,
                        Type = "boss",
                        Name = boss
                    });
                }
            }
        }

        private void UpdateCamps(WorldMemoryData memory, SessionData session, WorldMemoryUpdateResult result)
        {
            List<CampCluster> sessionCamps = _campClassification.Classify(session);
            HashSet<BuildActivitySample> classifiedSamples = new HashSet<BuildActivitySample>(sessionCamps.SelectMany(camp => camp.Samples));

            foreach (CampCluster sessionCamp in sessionCamps)
            {
                PersistentCampCluster persistent = FindCamp(memory.Camps, sessionCamp);
                bool isNew = persistent == null;
                if (persistent == null)
                {
                    persistent = CreateCamp(sessionCamp, session);
                    memory.Camps.Add(persistent);
                }

                PersistentCampChange change = ApplyCampObservation(persistent, sessionCamp, session, isNew);
                if (change.IsNewCamp || change.IsTierUpgrade || change.AddedStructures >= 5 ||
                    change.AddedForge || change.AddedPortal || change.AddedDefenses || change.AddedStorage || change.AddedAdvancedStation)
                {
                    result.CampChanges.Add(change);
                }
            }

            UpdateKnownCampsFromLooseBuildSamples(memory, session, result, classifiedSamples);
        }

        private static void UpdateKnownCampsFromLooseBuildSamples(
            WorldMemoryData memory,
            SessionData session,
            WorldMemoryUpdateResult result,
            HashSet<BuildActivitySample> classifiedSamples)
        {
            foreach (BuildActivitySample sample in session.BuildSamples.Where(sample => !classifiedSamples.Contains(sample)))
            {
                PersistentCampCluster persistent = memory.Camps
                    .OrderBy(camp => DistanceSquared(camp.CenterX, camp.CenterZ, sample.X, sample.Z))
                    .FirstOrDefault(camp => DistanceSquared(camp.CenterX, camp.CenterZ, sample.X, sample.Z) <= CampMergeRadius * CampMergeRadius);

                if (persistent == null)
                {
                    continue;
                }

                CampCluster syntheticCluster = new CampCluster
                {
                    Biome = sample.Biome,
                    CenterX = sample.X,
                    CenterZ = sample.Z,
                    StructureCount = 1,
                    Samples = new List<BuildActivitySample> { sample }
                };

                PersistentCampChange change = ApplyCampObservation(persistent, syntheticCluster, session, false);
                if (change.IsTierUpgrade || change.AddedForge || change.AddedPortal ||
                    change.AddedDefenses || change.AddedStorage || change.AddedAdvancedStation)
                {
                    result.CampChanges.Add(change);
                }
            }
        }

        private static PersistentCampCluster FindCamp(IEnumerable<PersistentCampCluster> camps, CampCluster sessionCamp)
        {
            return camps
                .Where(camp => string.IsNullOrWhiteSpace(camp.Biome) ||
                               string.IsNullOrWhiteSpace(sessionCamp.Biome) ||
                               string.Equals(camp.Biome, sessionCamp.Biome, StringComparison.OrdinalIgnoreCase))
                .OrderBy(camp => DistanceSquared(camp.CenterX, camp.CenterZ, sessionCamp.CenterX, sessionCamp.CenterZ))
                .FirstOrDefault(camp => DistanceSquared(camp.CenterX, camp.CenterZ, sessionCamp.CenterX, sessionCamp.CenterZ) <= CampMergeRadius * CampMergeRadius);
        }

        private static PersistentCampCluster CreateCamp(CampCluster sessionCamp, SessionData session)
        {
            return new PersistentCampCluster
            {
                CenterX = sessionCamp.CenterX,
                CenterZ = sessionCamp.CenterZ,
                Biome = sessionCamp.Biome,
                FirstSeenUtc = DateTime.UtcNow,
                LastSeenUtc = DateTime.UtcNow,
                LastExpandedSessionId = session.SessionId
            };
        }

        private static PersistentCampChange ApplyCampObservation(
            PersistentCampCluster persistent,
            CampCluster sessionCamp,
            SessionData session,
            bool isNew)
        {
            int previousTier = persistent.CurrentTier;
            string previousTierName = persistent.CurrentTierName;
            int previousStructures = persistent.StructureCount;
            bool hadForge = persistent.HasForge;
            bool hadPortal = persistent.HasPortal;
            bool hadAdvanced = persistent.HasAdvancedStation;
            int previousStorage = persistent.StorageCount;
            int previousDefenses = persistent.DefenseCount;

            int addedStructures = sessionCamp.StructureCount;
            persistent.StructureCount += addedStructures;
            persistent.LastExpandedSessionId = session.SessionId;
            persistent.LastSeenUtc = DateTime.UtcNow;
            persistent.CenterX = WeightedAverage(persistent.CenterX, previousStructures, sessionCamp.CenterX, Math.Max(1, sessionCamp.StructureCount));
            persistent.CenterZ = WeightedAverage(persistent.CenterZ, previousStructures, sessionCamp.CenterZ, Math.Max(1, sessionCamp.StructureCount));
            if (ChronicleFilters.IsValidBiome(sessionCamp.Biome))
            {
                persistent.Biome = sessionCamp.Biome;
            }

            foreach (BuildActivitySample sample in sessionCamp.Samples)
            {
                ApplyBuildFlags(persistent, sample);
            }

            ReclassifyPersistentCamp(persistent, session);

            return new PersistentCampChange
            {
                ClusterId = persistent.ClusterId,
                Biome = persistent.Biome,
                IsNewCamp = isNew,
                PreviousTier = previousTier,
                PreviousTierName = previousTierName,
                NewTier = persistent.CurrentTier,
                NewTierName = persistent.CurrentTierName,
                AddedStructures = persistent.StructureCount - previousStructures,
                AddedForge = !hadForge && persistent.HasForge,
                AddedPortal = !hadPortal && persistent.HasPortal,
                AddedAdvancedStation = !hadAdvanced && persistent.HasAdvancedStation,
                AddedStorage = persistent.StorageCount > previousStorage,
                AddedDefenses = persistent.DefenseCount > previousDefenses
            };
        }

        private static void ApplyBuildFlags(PersistentCampCluster camp, BuildActivitySample sample)
        {
            camp.HasFire |= sample.IsFire;
            camp.HasWorkbench |= sample.IsWorkbench;
            camp.HasForge |= sample.IsForge;
            camp.HasPortal |= sample.IsPortal;
            camp.HasAdvancedStation |= sample.IsAdvancedStation;
            if (sample.IsBed)
            {
                camp.BedCount++;
            }

            if (sample.IsStorage)
            {
                camp.StorageCount++;
            }

            if (sample.IsWallOrDefense)
            {
                camp.DefenseCount++;
            }

            if (sample.IsWorkstation || sample.IsForge || sample.IsAdvancedStation)
            {
                AddUnique(camp.StationTypes, sample.PieceName);
            }
        }

        private static void ReclassifyPersistentCamp(PersistentCampCluster camp, SessionData session)
        {
            int tier;
            string name;
            if (camp.HasPortal && camp.HasForge && (camp.DefenseCount > 0 || camp.HasAdvancedStation || camp.StructureCount >= 80))
            {
                tier = 5;
                name = "Pevnost";
            }
            else if (camp.StructureCount >= 40 || camp.StorageCount >= 3 || camp.DefenseCount >= 8 || camp.StationTypes.Count >= 3)
            {
                tier = 4;
                name = "Základna";
            }
            else if (camp.BedCount >= 2 && camp.HasFire && camp.HasWorkbench)
            {
                tier = 3;
                name = "Tábořiště";
            }
            else if (camp.HasFire && camp.HasWorkbench)
            {
                tier = 2;
                name = "Provizorní tábor";
            }
            else if (camp.HasFire)
            {
                tier = 1;
                name = "Táborák";
            }
            else
            {
                tier = camp.CurrentTier;
                name = camp.CurrentTierName;
            }

            if (tier != camp.CurrentTier)
            {
                camp.TierHistory.Add(new PersistentCampTierObservation
                {
                    TimestampUtc = DateTime.UtcNow,
                    SessionId = session.SessionId,
                    Tier = tier,
                    TierName = name,
                    StructureCount = camp.StructureCount
                });
            }

            camp.CurrentTier = tier;
            camp.CurrentTierName = name;
        }

        private static void UpdatePortals(WorldMemoryData memory, SessionData session, WorldMemoryUpdateResult result)
        {
            foreach (PortalActivitySample sample in session.PortalSamples)
            {
                string name = string.IsNullOrWhiteSpace(sample.PortalName) ? "Neznámý portál" : sample.PortalName;
                PersistentPortalRecord portal = memory.Portals.FirstOrDefault(existing =>
                    string.Equals(existing.PortalName, name, StringComparison.OrdinalIgnoreCase) ||
                    DistanceSquared(existing.ApproximateX, existing.ApproximateZ, sample.X, sample.Z) <= 45f * 45f);

                bool isNew = portal == null;
                if (portal == null)
                {
                    portal = new PersistentPortalRecord
                    {
                        PortalName = name,
                        FirstSeenUtc = sample.TimestampUtc
                    };
                    memory.Portals.Add(portal);
                }

                portal.PortalName = string.IsNullOrWhiteSpace(portal.PortalName) || portal.PortalName == "Neznámý portál" ? name : portal.PortalName;
                portal.LastSeenUtc = sample.TimestampUtc;
                portal.UsageCount++;
                portal.LinkedBiome = ChronicleFilters.IsValidBiome(sample.Biome) ? sample.Biome : portal.LinkedBiome;
                portal.ApproximateX = sample.X;
                portal.ApproximateY = sample.Y;
                portal.ApproximateZ = sample.Z;

                if (isNew)
                {
                    result.NewPortals.Add(portal);
                }

                AssociatePortalWithNearestCamp(memory, portal);
            }

            if (session.PortalSamples.Count == 0)
            {
                foreach (SessionEvent entry in session.Events.Where(entry => entry.Type == EventTypes.PortalUsed))
                {
                    string name = string.IsNullOrWhiteSpace(entry.Target) ? "Neznámý portál" : entry.Target;
                    PersistentPortalRecord portal = memory.Portals.FirstOrDefault(existing =>
                        string.Equals(existing.PortalName, name, StringComparison.OrdinalIgnoreCase));

                    bool isNew = portal == null;
                    if (portal == null)
                    {
                        portal = new PersistentPortalRecord
                        {
                            PortalName = name,
                            FirstSeenUtc = entry.TimestampUtc
                        };
                        memory.Portals.Add(portal);
                    }

                    portal.LastSeenUtc = entry.TimestampUtc;
                    portal.UsageCount++;
                    portal.LinkedBiome = ChronicleFilters.IsValidBiome(entry.Biome) ? entry.Biome : portal.LinkedBiome;
                    ApplyPosition(portal, entry.Position);

                    if (isNew)
                    {
                        result.NewPortals.Add(portal);
                    }

                    AssociatePortalWithNearestCamp(memory, portal);
                }
            }

            foreach (BuildActivitySample sample in session.BuildSamples.Where(sample => sample.IsPortal))
            {
                PersistentPortalRecord portal = memory.Portals.FirstOrDefault(existing =>
                    DistanceSquared(existing.ApproximateX, existing.ApproximateZ, sample.X, sample.Z) <= 45f * 45f);
                if (portal == null)
                {
                    portal = new PersistentPortalRecord
                    {
                        PortalName = "Neznámý portál",
                        FirstSeenUtc = sample.TimestampUtc,
                        ApproximateX = sample.X,
                        ApproximateY = sample.Y,
                        ApproximateZ = sample.Z,
                        LinkedBiome = sample.Biome
                    };
                    memory.Portals.Add(portal);
                    result.NewPortals.Add(portal);
                }

                portal.LastSeenUtc = sample.TimestampUtc;
                AssociatePortalWithNearestCamp(memory, portal);
            }
        }

        private static void AssociatePortalWithNearestCamp(WorldMemoryData memory, PersistentPortalRecord portal)
        {
            PersistentCampCluster camp = memory.Camps
                .OrderBy(existing => DistanceSquared(existing.CenterX, existing.CenterZ, portal.ApproximateX, portal.ApproximateZ))
                .FirstOrDefault(existing => DistanceSquared(existing.CenterX, existing.CenterZ, portal.ApproximateX, portal.ApproximateZ) <= 90f * 90f);

            if (camp == null)
            {
                return;
            }

            camp.HasPortal = true;
            AddUnique(camp.PortalNames, portal.PortalName);
        }

        private static void UpdateImportantStructures(WorldMemoryData memory, SessionData session, WorldMemoryUpdateResult result)
        {
            foreach (BuildActivitySample sample in session.BuildSamples.Where(IsImportantStructure))
            {
                PersistentStructureRecord record = memory.ImportantStructures.FirstOrDefault(existing =>
                    string.Equals(existing.StructureName, sample.PieceName, StringComparison.OrdinalIgnoreCase) &&
                    DistanceSquared(existing.ApproximateX, existing.ApproximateZ, sample.X, sample.Z) <= 60f * 60f);

                bool isNew = record == null;
                if (record == null)
                {
                    record = new PersistentStructureRecord
                    {
                        StructureName = sample.PieceName,
                        StructureType = GetStructureType(sample),
                        Biome = sample.Biome,
                        FirstSeenUtc = sample.TimestampUtc,
                        FirstSeenSessionId = session.SessionId,
                        ApproximateX = sample.X,
                        ApproximateY = sample.Y,
                        ApproximateZ = sample.Z
                    };
                    memory.ImportantStructures.Add(record);
                }

                record.LastSeenUtc = sample.TimestampUtc;
                record.ObservationCount++;
                if (isNew)
                {
                    result.NewImportantStructures.Add(record);
                    memory.ProgressionObservations.Add(new WorldProgressionObservation
                    {
                        TimestampUtc = sample.TimestampUtc,
                        SessionId = session.SessionId,
                        Type = "structure",
                        Name = sample.PieceName,
                        Biome = sample.Biome
                    });
                }
            }

            foreach (ProgressionStructureObservation observation in session.StructureObservations.Where(IsImportantStructure))
            {
                PersistentStructureRecord record = memory.ImportantStructures.FirstOrDefault(existing =>
                    string.Equals(existing.StructureName, observation.StructureName, StringComparison.OrdinalIgnoreCase) &&
                    DistanceSquared(existing.ApproximateX, existing.ApproximateZ, observation.X, observation.Z) <= 60f * 60f);

                bool isNew = record == null;
                if (record == null)
                {
                    record = new PersistentStructureRecord
                    {
                        StructureName = observation.StructureName,
                        StructureType = observation.StructureType,
                        Biome = observation.Biome,
                        FirstSeenUtc = observation.TimestampUtc,
                        FirstSeenSessionId = session.SessionId,
                        ApproximateX = observation.X,
                        ApproximateY = observation.Y,
                        ApproximateZ = observation.Z
                    };
                    memory.ImportantStructures.Add(record);
                }

                record.LastSeenUtc = observation.TimestampUtc;
                record.ObservationCount++;
                if (isNew)
                {
                    result.NewImportantStructures.Add(record);
                    memory.ProgressionObservations.Add(new WorldProgressionObservation
                    {
                        TimestampUtc = observation.TimestampUtc,
                        SessionId = session.SessionId,
                        Type = "structure",
                        Name = observation.StructureName,
                        Biome = observation.Biome
                    });
                }
            }
        }

        private static bool IsImportantStructure(BuildActivitySample sample)
        {
            return sample.IsForge || sample.IsAdvancedStation ||
                   ChronicleFilters.NormalizeKey(sample.PieceName).Contains("stonecutter") ||
                   ChronicleFilters.NormalizeKey(sample.PieceName).Contains("artisan");
        }

        private static bool IsImportantStructure(ProgressionStructureObservation observation)
        {
            string normalized = ChronicleFilters.NormalizeKey(observation.StructureName);
            return normalized.Contains("forge") || normalized.Contains("kovarna") ||
                   normalized.Contains("stonecutter") || normalized.Contains("artisan") ||
                   normalized.Contains("blackforge") || normalized.Contains("galdr") ||
                   normalized.Contains("eitrrefinery") || normalized.Contains("blastfurnace") ||
                   normalized.Contains("windmill") || normalized.Contains("spinningwheel");
        }

        private static string GetStructureType(BuildActivitySample sample)
        {
            if (sample.IsForge)
            {
                return "Forge";
            }

            if (sample.IsAdvancedStation)
            {
                return "AdvancedStation";
            }

            return "ProgressionStructure";
        }

        private static void ApplyPosition(PersistentPortalRecord portal, string position)
        {
            if (string.IsNullOrWhiteSpace(position))
            {
                return;
            }

            string[] parts = position.Split(',');
            if (parts.Length != 3)
            {
                return;
            }

            if (float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z))
            {
                portal.ApproximateX = x;
                portal.ApproximateY = y;
                portal.ApproximateZ = z;
            }
        }

        private static void Trim(WorldMemoryData memory)
        {
            memory.ProgressionObservations = memory.ProgressionObservations
                .OrderByDescending(entry => entry.TimestampUtc)
                .Take(200)
                .OrderBy(entry => entry.TimestampUtc)
                .ToList();
        }

        private static float WeightedAverage(float current, int currentWeight, float incoming, int incomingWeight)
        {
            int total = Math.Max(1, currentWeight + incomingWeight);
            return (current * currentWeight + incoming * incomingWeight) / total;
        }

        private static float DistanceSquared(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx;
            float dz = az - bz;
            return dx * dx + dz * dz;
        }

        private static bool AddUnique(ICollection<string> values, string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(value);
                return true;
            }

            return false;
        }
    }
}
