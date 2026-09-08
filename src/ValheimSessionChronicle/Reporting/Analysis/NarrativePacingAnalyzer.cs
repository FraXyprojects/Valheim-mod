using System;
using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Core;
using ValheimSessionChronicle.Models;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class NarrativePacingAnalyzer
    {
        private const int StepSeconds = 30;
        private const int WindowSeconds = 60;

        public NarrativePacingResult Analyze(SessionData session)
        {
            var result = new NarrativePacingResult();
            var events = session.Events.Where(e => !string.IsNullOrEmpty(e.Type)).OrderBy(e => e.TimestampUtc).ToList();
            if (events.Count == 0) return result;

            var rawPhases = CalculateRawWindows(events, session.StartTimeUtc, session.Duration);
            var mergedPhases = MergeWindows(rawPhases);

            PopulateParticipants(mergedPhases, events);

            result.Phases = mergedPhases;
            return result;
        }

        private List<PacingPhase> CalculateRawWindows(List<SessionEvent> events, DateTime startTime, TimeSpan duration)
        {
            var windows = new List<PacingPhase>();
            int numSteps = (int)Math.Ceiling(duration.TotalSeconds / StepSeconds);

            for (int i = 0; i < numSteps; i++)
            {
                var currentStart = startTime.AddSeconds(i * StepSeconds);
                var currentEnd = currentStart.AddSeconds(WindowSeconds);

                var windowEvents = events.Where(e => e.TimestampUtc >= currentStart && e.TimestampUtc < currentEnd).ToList();

                var phase = new PacingPhase
                {
                    StartTime = currentStart,
                    EndTime = currentEnd,
                    EventCount = windowEvents.Count,
                    Events = windowEvents,
                    DominantBiome = DetermineDominantBiome(windowEvents),
                    Intensity = CalculateIntensity(windowEvents),
                };

                phase.PhaseType = DeterminePhaseType(phase, windowEvents);
                windows.Add(phase);
            }

            return windows;
        }

        private List<PacingPhase> MergeWindows(List<PacingPhase> rawWindows)
        {
            if (rawWindows.Count == 0) return new List<PacingPhase>();

            var merged = new List<PacingPhase>();
            var currentPhase = ClonePhase(rawWindows[0]);

            for (int i = 1; i < rawWindows.Count; i++)
            {
                var nextWindow = rawWindows[i];

                // If phase type is similar or it's a very short gap, merge them
                if (currentPhase.PhaseType == nextWindow.PhaseType)
                {
                    currentPhase.EndTime = nextWindow.EndTime;
                    currentPhase.Events.AddRange(nextWindow.Events.Where(e => !currentPhase.Events.Contains(e)));
                    currentPhase.EventCount = currentPhase.Events.Count;
                    currentPhase.Intensity = Math.Max(currentPhase.Intensity, nextWindow.Intensity);

                    if (string.IsNullOrEmpty(currentPhase.DominantBiome) && !string.IsNullOrEmpty(nextWindow.DominantBiome))
                    {
                        currentPhase.DominantBiome = nextWindow.DominantBiome;
                    }
                }
                else
                {
                    // Smooth out short valleys / noise
                    if (currentPhase.Duration.TotalMinutes < 1.5 && merged.Count > 0 && merged.Last().PhaseType == nextWindow.PhaseType)
                    {
                         merged.Last().EndTime = nextWindow.EndTime;
                         merged.Last().Events.AddRange(currentPhase.Events);
                         merged.Last().Events.AddRange(nextWindow.Events.Where(e => !merged.Last().Events.Contains(e)));
                         merged.Last().EventCount = merged.Last().Events.Count;
                         merged.Last().Intensity = Math.Max(merged.Last().Intensity, Math.Max(currentPhase.Intensity, nextWindow.Intensity));
                         currentPhase = ClonePhase(merged.Last());
                         merged.RemoveAt(merged.Count - 1);
                    }
                    else
                    {
                        merged.Add(currentPhase);
                        currentPhase = ClonePhase(nextWindow);
                    }
                }
            }

            merged.Add(currentPhase);

            // Post-process peaks and valleys
            if (merged.Count > 0)
            {
                double averageIntensity = merged.Average(p => p.Intensity);
                foreach(var phase in merged)
                {
                    if (phase.PhaseType == PacingPhaseType.CombatPeak) phase.IsPeak = true;
                    if (phase.Intensity > averageIntensity * 2 && phase.Intensity > 15) phase.IsPeak = true;
                    if (phase.PhaseType == PacingPhaseType.Calm) phase.IsValley = true;
                }
            }

            return merged;
        }

        private PacingPhase ClonePhase(PacingPhase original)
        {
            return new PacingPhase
            {
                StartTime = original.StartTime,
                EndTime = original.EndTime,
                PhaseType = original.PhaseType,
                Intensity = original.Intensity,
                EventCount = original.EventCount,
                IsPeak = original.IsPeak,
                IsValley = original.IsValley,
                DominantBiome = original.DominantBiome,
                Events = new List<SessionEvent>(original.Events)
            };
        }

        private double CalculateIntensity(List<SessionEvent> events)
        {
            double score = 0;
            foreach (var e in events)
            {
                double weight = 1.0;

                if (e.Importance == EventImportance.Medium) weight = 2.0;
                else if (e.Importance == EventImportance.High) weight = 5.0;
                else if (e.Importance == EventImportance.Critical) weight = 10.0;

                if (e.Type == EventTypes.EnemyKilled) weight += 1.0;
                if (e.Type == EventTypes.PlayerDeath) weight += 20.0;
                if (e.Type == EventTypes.BossKilled) weight += 50.0;
                if (e.Category == EventCategories.Combat) weight += 0.5;
                if (e.Category == EventCategories.Building) weight += 0.2;

                score += weight;
            }

            // Bonus for temporal density (more events in short window)
            if (events.Count > 10) score *= 1.2;
            if (events.Count > 20) score *= 1.5;

            return score;
        }

        private string DetermineDominantBiome(List<SessionEvent> events)
        {
            var biomes = events.Where(e => !string.IsNullOrEmpty(e.Biome)).GroupBy(e => e.Biome).OrderByDescending(g => g.Count()).FirstOrDefault();
            return biomes?.Key ?? string.Empty;
        }

        private PacingPhaseType DeterminePhaseType(PacingPhase phase, List<SessionEvent> events)
        {
            if (events.Count == 0) return PacingPhaseType.Calm;

            int combatCount = events.Count(e => e.Category == EventCategories.Combat);
            int buildCount = events.Count(e => e.Category == EventCategories.Building);
            int exploreCount = events.Count(e => e.Type == EventTypes.BiomeEntered || e.Type == EventTypes.Discovery);
            int travelCount = events.Count(e => e.Type == EventTypes.PortalUsed || e.Type == EventTypes.ShipUsed);

            if (events.Any(e => e.Type == EventTypes.BossKilled)) return PacingPhaseType.BossCombat;

            if (phase.Intensity > 30 || combatCount >= 8 || events.Any(e => e.Category == EventCategories.Combat && e.Type == EventTypes.PlayerDeath))
                return PacingPhaseType.CombatPeak;

            if (combatCount >= 3) return PacingPhaseType.Combat;
            if (buildCount >= 5) return PacingPhaseType.Building;
            if (exploreCount >= 2) return PacingPhaseType.Exploration;
            if (travelCount >= 1) return PacingPhaseType.Travel;

            return PacingPhaseType.Activity;
        }

        private void PopulateParticipants(List<PacingPhase> phases, List<SessionEvent> allEvents)
        {
             foreach (var phase in phases)
             {
                 var activeActorsInPhase = phase.Events
                    .Where(e => !string.IsNullOrEmpty(e.Actor) && !e.Actor.Equals("Environment", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(e => e.Actor)
                    .ToDictionary(g => g.Key, g => g.ToList());

                 // Look slightly wider for medium evidence
                 var extendedStart = phase.StartTime.AddMinutes(-2);
                 var extendedEnd = phase.EndTime.AddMinutes(2);

                 var activeActorsInExtendedWindow = allEvents
                    .Where(e => e.TimestampUtc >= extendedStart && e.TimestampUtc <= extendedEnd && !string.IsNullOrEmpty(e.Actor) && !e.Actor.Equals("Environment", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(e => e.Actor)
                    .ToDictionary(g => g.Key, g => g.ToList());

                 foreach (var actorGroup in activeActorsInPhase)
                 {
                     string actor = actorGroup.Key;
                     var actorEvents = actorGroup.Value;

                     // Strong evidence: Requires both a position (so we know exactly where they are)
                     // AND multiple events to prove they are actively doing things in the exact timeframe.
                     bool hasExplicitPosition = actorEvents.Any(e => !string.IsNullOrEmpty(e.Position));

                     // We DO NOT use matching biome alone as strong evidence anymore. That is medium at best.

                     if (actorEvents.Count >= 2 && hasExplicitPosition)
                     {
                          if (!phase.StrongEvidenceParticipants.Contains(actor))
                              phase.StrongEvidenceParticipants.Add(actor);
                     }
                     else
                     {
                          if (!phase.MediumEvidenceParticipants.Contains(actor))
                              phase.MediumEvidenceParticipants.Add(actor);
                     }
                 }

                 foreach (var actorGroup in activeActorsInExtendedWindow)
                 {
                     string actor = actorGroup.Key;
                     if (!phase.StrongEvidenceParticipants.Contains(actor) && !phase.MediumEvidenceParticipants.Contains(actor))
                     {
                          if (!phase.WeakEvidenceParticipants.Contains(actor))
                              phase.WeakEvidenceParticipants.Add(actor);
                     }
                 }
             }
        }
    }

    public class NarrativePacingResult
    {
        public List<PacingPhase> Phases { get; set; } = new List<PacingPhase>();
    }
}
