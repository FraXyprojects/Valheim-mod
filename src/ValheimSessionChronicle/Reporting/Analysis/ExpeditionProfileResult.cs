using System.Collections.Generic;
using System.Linq;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class ExpeditionProfileResult
    {
        public List<ExpeditionProfileScore> Scores { get; set; } = new List<ExpeditionProfileScore>();

        public IEnumerable<ExpeditionProfileScore> DominantScores =>
            Scores.Where(score => score.Percentage >= 15).OrderByDescending(score => score.Percentage).Take(2);
    }
}
