using ElitePiracyTracker.Models;
using System.Windows.Media;

namespace ElitePiracyTracker.WPF.ViewModels
{
    public class SystemDetailsViewModel
    {
        private readonly PiracyScoreResult _system;

        public SystemDetailsViewModel(PiracyScoreResult system)
        {
            _system = system;
        }

        public string SystemName => _system.SystemName;
        public double FinalScore => _system.FinalScore;
        public double EconomyScore => _system.EconomyScore;
        public double NoRingsScore => _system.NoRingsScore;
        public double GovernmentScore => _system.GovernmentScore;
        public double SecurityScore => _system.SecurityScore;
        public double FactionStateScore => _system.FactionStateScore;
        public double MarketDemandScore => _system.MarketDemandScore;
        public string Recommendation
        {
            get
            {
                if (FinalScore >= 80)
                    return "EXCELLENT PIRACY SPOT - Highly recommended due to optimal combination of factors including " +
                           GetPositiveFactors() + ". " + GetImprovementAreas();
                else if (FinalScore >= 60)
                    return "Good piracy spot - Recommended with " + GetPositiveFactors() + ". " + GetImprovementAreas();
                else if (FinalScore >= 40)
                    return "Average piracy spot - Consider other options. " + GetPositiveFactors() + ". " + GetImprovementAreas();
                else if (FinalScore >= 20)
                    return "Poor piracy spot - Not recommended due to " + GetNegativeFactors() + ". " + GetImprovementAreas();
                else
                    return "Very poor piracy spot - Avoid at all costs due to " + GetNegativeFactors() + ". " + GetImprovementAreas();
            }
        }

        private string GetPositiveFactors()
        {
            var factors = new List<string>();

            if (_system.HasIndustrialEconomy || _system.HasExtractionEconomy) factors.Add("favorable economy");
            if (_system.HasNoRings) factors.Add("lack of rings");
            if (_system.HasAnarchyGovernment) factors.Add("anarchic government");
            if (_system.HasLowSecurity) factors.Add("low security presence");
            if (_system.HasPirateFaction) factors.Add("pirate faction presence");
            if (_system.MarketDemandScore > 0.05 && _system.BestCommodity != null) factors.Add($"high market demand of {_system.BestCommodity.Name}");

            return factors.Count > 0 ? string.Join(", ", factors) : "minimal positive factors";
        }

        private string GetNegativeFactors()
        {
            var factors = new List<string>();

            if (!_system.HasIndustrialEconomy && !_system.HasExtractionEconomy) factors.Add("unfavorable economy");
            if (!_system.HasNoRings) factors.Add("presence of rings");
            if (!_system.HasAnarchyGovernment) factors.Add("strong government presence");
            if (!_system.HasLowSecurity) factors.Add("high security");
            if (!_system.HasPirateFaction) factors.Add("lack of pirate factions");
            if (_system.MarketDemandScore < 0.02) factors.Add("bad markets demand");
            if ((_system.MarketDemandScore >= 0.02 && _system.MarketDemandScore <= 0.05) && _system.BestCommodity != null) factors.Add($"low market demand of {_system.BestCommodity.Name}");

            return factors.Count > 0 ? string.Join(", ", factors) : "overall poor conditions";
        }

        private string GetImprovementAreas()
        {
            var improvements = new List<string>();

            if (!_system.HasIndustrialEconomy && !_system.HasExtractionEconomy) improvements.Add("look for industrial/extraction economies");
            if (!_system.HasNoRings) improvements.Add("prioritize systems without rings");
            if (!_system.HasAnarchyGovernment) improvements.Add("seek anarchic systems");
            if (!_system.HasLowSecurity) improvements.Add("target low security systems");
            if (!_system.HasPirateFaction) improvements.Add("find systems with pirate factions");
            if (_system.MarketDemandScore < 0.02) improvements.Add("look for high demand markets");
            if ((_system.MarketDemandScore >= 0.02 && _system.MarketDemandScore <= 0.05) && _system.BestCommodity != null) improvements.Add($"market demand of {_system.BestCommodity.Name}");


            return improvements.Count > 0 ? "\nFor better results: " + string.Join(", ", improvements) + "." : "";
        }

        public string RecommendationIcon
        {
            get
            {
                if (FinalScore >= 80) return "Checkmark24";
                if (FinalScore >= 60) return "Checkmark24";
                if (FinalScore >= 40) return "Warning24";
                return "ErrorCircle24";
            }
        }

        public SolidColorBrush RecommendationIconColor
        {
            get
            {
                if (FinalScore >= 80) return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                if (FinalScore >= 60) return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                if (FinalScore >= 40) return new SolidColorBrush(Color.FromRgb(255, 193, 7));  // Amber
                return new SolidColorBrush(Color.FromRgb(244, 67, 54));                       // Red
            }
        }
    }
}