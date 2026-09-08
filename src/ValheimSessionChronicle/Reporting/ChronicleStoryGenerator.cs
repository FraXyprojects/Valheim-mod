using System;
using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Reporting.Analysis;
using ValheimSessionChronicle.Utility;
using ValheimSessionChronicle.WorldMemory;

namespace ValheimSessionChronicle.Reporting
{
    public sealed class ChronicleStoryGenerator
    {
        private readonly NarrativeContextBuilder _contextBuilder = new NarrativeContextBuilder();

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
            DiscoveryAnalysis discovery,
            NarrativePacingResult pacing)
        {
            List<string> paragraphs = new List<string>();

            NarrativeContext context = _contextBuilder.Build(session, meaningfulEvents, combat, survival, profile, camps, worldMemory, memoryUpdate, progression, discovery, pacing);

            if (context.Phases.Count > 0)
            {
                paragraphs.Add(string.Join(" ", context.Phases));
            }

            AddWorldMemoryParagraph(worldMemory, memoryUpdate, paragraphs);

            if (!string.IsNullOrWhiteSpace(context.ProgressionParagraph)) paragraphs.Add(context.ProgressionParagraph);
            if (!string.IsNullOrWhiteSpace(context.ResourceParagraph)) paragraphs.Add(context.ResourceParagraph);
            if (!string.IsNullOrWhiteSpace(context.DiscoveryParagraph)) paragraphs.Add(context.DiscoveryParagraph);
            if (!string.IsNullOrWhiteSpace(context.PortalNetworkParagraph)) paragraphs.Add(context.PortalNetworkParagraph);
            if (!string.IsNullOrWhiteSpace(context.ProfileParagraph)) paragraphs.Add(context.ProfileParagraph);
            if (!string.IsNullOrWhiteSpace(context.CombatParagraph)) paragraphs.Add(context.CombatParagraph);
            if (!string.IsNullOrWhiteSpace(context.SurvivalParagraph)) paragraphs.Add(context.SurvivalParagraph);
            if (!string.IsNullOrWhiteSpace(context.EndingParagraph)) paragraphs.Add(context.EndingParagraph);

            if (paragraphs.Count == 0)
            {
                string mainPlayer = session.LocalPlayerName ?? session.Players.FirstOrDefault() ?? "Hráč";
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

            List<string> createdCamps = memoryUpdate.CampChanges.Where(c => c.Type == WorldMemoryChangeType.Created).Select(c => $"{c.NewTierName.ToLowerInvariant()} {c.CampName}").ToList();
            List<string> upgradedCamps = memoryUpdate.CampChanges.Where(c => c.Type == WorldMemoryChangeType.Upgraded).Select(c => $"{c.CampName} na úroveň {c.NewTierName.ToLowerInvariant()}").ToList();

            if (createdCamps.Count > 0)
            {
                paragraphs.Add(createdCamps.Count == 1
                    ? $"Do paměti světa byl trvale zapsán nový opěrný bod: {createdCamps[0]}."
                    : $"Na mapu světa přibyly nové opěrné body: {string.Join(", ", createdCamps)}.");
            }

            if (upgradedCamps.Count > 0)
            {
                paragraphs.Add(upgradedCamps.Count == 1
                    ? $"Trvalé zázemí bylo rozšířeno, když byl vylepšen {upgradedCamps[0]}."
                    : $"Oblast se stala bezpečnější díky vylepšení několika základen: {string.Join(", ", upgradedCamps)}.");
            }
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            }

            return $"{Math.Max(0, duration.Minutes)}m {Math.Max(0, duration.Seconds)}s";
        }
    }
}
