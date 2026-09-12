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

            string shortId = string.IsNullOrEmpty(session.SessionId) ? Guid.NewGuid().ToString("N").Substring(0, 4) : session.SessionId.Substring(0, 4);

            return MakeSafeFileName($"{date}_{profileToken}_{biome}_{shortId}");
        }

        private static string BuildProfileToken(ExpeditionProfileResult profile)
        {
            var dominant = profile.DominantScores.FirstOrDefault();
            string profileStr = dominant != null ? dominant.FileToken : "Chronicle";
            return profileStr;
        }

        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            // Security: Path.GetInvalidFileNameChars is OS-dependent.
            // Explicitly strip slashes and dots to prevent path traversal on all OS.
            value = value.Replace('/', '_').Replace('\\', '_').Replace('.', '_');

            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Replace(' ', '_');
        }
    }
}
