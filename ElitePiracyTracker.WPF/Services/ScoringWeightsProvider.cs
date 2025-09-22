using ElitePiracyTracker.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElitePiracyTracker.WPF.Services
{
    public static class ScoringWeightsProvider
    {
        private static PiracyScoringConfig _weights;

        public static void Initialize(IConfiguration configuration)
        {
            _weights = new PiracyScoringConfig();
            configuration.GetSection("ScoringWeights").Bind(_weights);
        }

        // Helper method to get weight by name
        public static double GetWeight(string weightName)
        {
            if (_weights == null)
                return 1.0; // Default fallback

            return weightName switch
            {
                "Economy" => _weights.EconomyScoreWeight,
                "NoRings" => _weights.NoRingsScoreWeight,
                "Government" => _weights.GovernmentScoreWeight,
                "Security" => _weights.SecurityScoreWeight,
                "FactionState" => _weights.FactionStateScoreWeight,
                "MarketDemand" => _weights.MarketDemandScoreWeight,
                "Population" => _weights.PopulationScoreWeight,
                _ => 1.0
            };
        }
    }
}