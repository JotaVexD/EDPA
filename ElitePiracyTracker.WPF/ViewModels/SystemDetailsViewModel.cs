using ElitePiracyTracker.Models;

namespace ElitePiracyTracker.WPF.ViewModels
{
    public class SystemDetailsViewModel
    {
        public string SystemName { get; set; }
        public double FinalScore { get; set; }
        public double EconomyScore { get; set; }
        public double NoRingsScore { get; set; }
        public double GovernmentScore { get; set; }
        public double SecurityScore { get; set; }
        public double FactionStateScore { get; set; }
        public double MarketDemandScore { get; set; }
        public string Recommendation { get; set; }

        public SystemDetailsViewModel(PiracyScoreResult system)
        {
            SystemName = system.SystemName;
            FinalScore = system.FinalScore;
            EconomyScore = system.EconomyScore;
            NoRingsScore = system.NoRingsScore;
            GovernmentScore = system.GovernmentScore;
            SecurityScore = system.SecurityScore;
            FactionStateScore = system.FactionStateScore;
            MarketDemandScore = system.MarketDemandScore;

            // Generate recommendation based on score
            if (FinalScore >= 90)
                Recommendation = "⭐ EXCELLENT PIRACY SPOT - Highly recommended!";
            else if (FinalScore >= 80)
                Recommendation = "✓ Good piracy spot - Worth checking out";
            else if (FinalScore >= 70)
                Recommendation = "~ Moderate piracy spot - Some potential";
            else
                Recommendation = "✗ Poor piracy spot - Not recommended";
        }
    }
}