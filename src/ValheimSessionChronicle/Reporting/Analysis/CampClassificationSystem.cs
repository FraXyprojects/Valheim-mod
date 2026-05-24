using System;
using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Utility;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class CampClassificationSystem
    {
        private const float ClusterRadius = 45f;

        public List<CampCluster> Classify(SessionData session)
        {
            List<CampCluster> clusters = new List<CampCluster>();
            foreach (BuildActivitySample sample in session.BuildSamples.OrderBy(sample => sample.TimestampUtc))
            {
                CampCluster cluster = FindCluster(clusters, sample);
                if (cluster == null)
                {
                    cluster = new CampCluster { CenterX = sample.X, CenterZ = sample.Z, Biome = sample.Biome };
                    clusters.Add(cluster);
                }

                cluster.Samples.Add(sample);
                RecalculateCenter(cluster);
            }

            foreach (CampCluster cluster in clusters)
            {
                ClassifyCluster(cluster);
            }

            return clusters
                .Where(cluster => cluster.Tier != CampTier.None)
                .OrderByDescending(cluster => cluster.Tier)
                .ThenByDescending(cluster => cluster.StructureCount)
                .ToList();
        }

        private static CampCluster FindCluster(IEnumerable<CampCluster> clusters, BuildActivitySample sample)
        {
            return clusters.FirstOrDefault(cluster =>
            {
                float dx = cluster.CenterX - sample.X;
                float dz = cluster.CenterZ - sample.Z;
                return dx * dx + dz * dz <= ClusterRadius * ClusterRadius;
            });
        }

        private static void RecalculateCenter(CampCluster cluster)
        {
            cluster.CenterX = cluster.Samples.Average(sample => sample.X);
            cluster.CenterZ = cluster.Samples.Average(sample => sample.Z);
            cluster.Biome = cluster.Samples
                .Where(sample => ChronicleFilters.IsValidBiome(sample.Biome))
                .GroupBy(sample => sample.Biome, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .Select(group => group.Key)
                .FirstOrDefault() ?? cluster.Biome;
        }

        private static void ClassifyCluster(CampCluster cluster)
        {
            int structures = cluster.Samples.Count;
            int fires = cluster.Samples.Count(sample => sample.IsFire);
            int workbenches = cluster.Samples.Count(sample => sample.IsWorkbench);
            int beds = cluster.Samples.Count(sample => sample.IsBed);
            int storage = cluster.Samples.Count(sample => sample.IsStorage);
            int defenses = cluster.Samples.Count(sample => sample.IsWallOrDefense);
            int portals = cluster.Samples.Count(sample => sample.IsPortal);
            int forges = cluster.Samples.Count(sample => sample.IsForge);
            int advancedStations = cluster.Samples.Count(sample => sample.IsAdvancedStation);
            int stations = cluster.Samples.Count(sample => sample.IsWorkstation || sample.IsForge || sample.IsAdvancedStation);

            cluster.StructureCount = structures;
            cluster.WorkstationCount = stations;

            if (portals > 0 && forges > 0 && (defenses > 0 || advancedStations > 0 || structures >= 80))
            {
                SetTier(cluster, CampTier.Pevnost, "Pevnost");
            }
            else if (structures >= 40 || storage >= 3 || defenses >= 8 || stations >= 3)
            {
                SetTier(cluster, CampTier.Zakladna, "Základna");
            }
            else if (beds >= 2 && fires > 0 && workbenches > 0)
            {
                SetTier(cluster, CampTier.Taboriste, "Tábořiště");
            }
            else if (fires > 0 && workbenches > 0)
            {
                SetTier(cluster, CampTier.ProvizorniTabor, "Provizorní tábor");
            }
            else if (fires > 0)
            {
                SetTier(cluster, CampTier.Taborak, "Táborák");
            }
        }

        private static void SetTier(CampCluster cluster, CampTier tier, string name)
        {
            cluster.Tier = tier;
            cluster.Name = name;
        }
    }
}
