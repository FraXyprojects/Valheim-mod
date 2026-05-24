using System;
using System.Linq;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Reporting.Analysis;
using ValheimSessionChronicle.Utility;

namespace ValheimSessionChronicle.Storage
{
    public sealed class ChronicleFileNameBuilder
    {
        private readonly CombatIntensityAnalyzer _combatAnalyzer = new CombatIntensityAnalyzer();
        private readonly SurvivalAnalyzer _survivalAnalyzer = new SurvivalAnalyzer();
        private readonly ExpeditionProfileAnalyzer _profileAnalyzer = new ExpeditionProfileAnalyzer();

        public string BuildBaseName(SessionData session)
        {
            CombatIntensityResult combat = _combatAnalyzer.Analyze(session);
            SurvivalSummary survival = _survivalAnalyzer.Analyze(session);
            ExpeditionProfileResult profile = _profileAnalyzer.Analyze(session, combat, survival);

            string date = session.StartTimeUtc.ToLocalTime().ToString("yyyy-MM-dd");
            string profileToken = BuildProfileToken(profile);
            string biome = ChronicleFilters.IsValidBiome(combat.DominantCombatBiome)
                ? combat.DominantCombatBiome
                : session.Environment.BiomesVisited.LastOrDefault(ChronicleFilters.IsValidBiome) ?? "Unknown";

            return MakeSafeFileName($"{date}_{profileToken}Vyprava_{biome}_{session.SessionId.Substring(0, 8)}");
        }

        private static string BuildProfileToken(ExpeditionProfileResult profile)
        {
            string combined = string.Concat(profile.DominantScores.Select(score => score.FileToken));
            return string.IsNullOrWhiteSpace(combined) ? "Kronika" : combined;
        }

        private static string MakeSafeFileName(string value)
        {
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Replace(' ', '_');
        }
    }
}
