using EDPA.Models;
using System.Windows.Media;
using Wpf.Ui;

namespace EDPA.WPF.ViewModels
{
    public class SystemDetailsViewModel
    {
        private readonly PiracyScoreResult _scoreResult;
        private readonly SystemData _systemData;

        public SystemDetailsViewModel(PiracyScoreResult scoreResult, SystemData systemData)
        {
            _scoreResult = scoreResult;
            _systemData = systemData;
        }

        public string SystemName => _scoreResult.SystemName;
        public double FinalScore => _scoreResult.FinalScore;
        public double EconomyScore => _scoreResult.EconomyScore;
        public double NoRingsScore => _scoreResult.NoRingsScore;
        public double GovernmentScore => _scoreResult.GovernmentScore;
        public double SecurityScore => _scoreResult.SecurityScore;
        public double PopulationScore => _scoreResult.PopulationScore;
        public double FactionStateScore => _scoreResult.FactionStateScore;
        public double MarketDemandScore => _scoreResult.MarketDemandScore;
        public string EconomyDescription => $"{_systemData.Economy} / {_systemData.SecondEconomy}";
        public string GovernmentDescription => _systemData.Government; 
        public string PopulationDescription => $"{_systemData.Population:n0}";
        public string NoRingsDescription => "No rings present";
        public string SecurityDescription => _systemData.Government;
        public string FactionStateDescription => _systemData.FactionState;
        public string MarketDemandDescription
        {
            get
            {
                if (_scoreResult.BestCommodity.Name == null)
                    return "No demand on the system";
                else
                    return "Demand of " + _scoreResult.BestCommodity.Name + " with a quantity of " + _scoreResult.BestCommodity.Demand;
            }
        }
        
        public string Recommendation
        {
            get
            {
                if (FinalScore >= 85)
                    return "EXCELLENT PIRACY SPOT - Highly recommended due to optimal combination of factors including " +
                           GetPositiveFactors() + ". " + GetImprovementAreas();
                else if (FinalScore >= 75)
                    return "Good piracy spot - Recommended with " + GetPositiveFactors() + ". " + GetImprovementAreas();
                else if (FinalScore >= 60)
                    return "Average piracy spot - Consider other options. " + GetPositiveFactors() + ". " + GetImprovementAreas();
                else if (FinalScore >= 45)
                    return "Poor piracy spot - Not recommended due to " + GetNegativeFactors() + ". " + GetImprovementAreas();
                else
                    return "Very poor piracy spot - Avoid at all costs due to " + GetNegativeFactors() + ". " + GetImprovementAreas();
            }
        }

        private string GetPositiveFactors()
        {
            var factors = new List<string>();

            if (_scoreResult.HasIndustrialEconomy || _scoreResult.HasExtractionEconomy) factors.Add("\n\t- Favorable economy");
            if (_scoreResult.HasNoRings) factors.Add("\n\t- Lack of rings");
            if (_scoreResult.HasAnarchyGovernment) factors.Add("\n\t- Anarchic government");
            if (_scoreResult.HasLowSecurity) factors.Add("\n\t- Low security presence");
            if (_scoreResult.HasPirateFaction) factors.Add("\n\t- Pirate faction presence");
            if (_scoreResult.MarketDemandScore > 0.05 && _scoreResult.BestCommodity != null) factors.Add($"\n\t- High market demand of {_scoreResult.BestCommodity.Name}");

            return factors.Count > 0 ? string.Join(", ", factors) : "minimal positive factors";
        }

        private string GetNegativeFactors()
        {
            var factors = new List<string>();

            if (!_scoreResult.HasIndustrialEconomy && !_scoreResult.HasExtractionEconomy) factors.Add("\n\t- Unfavorable economy");
            if (!_scoreResult.HasNoRings) factors.Add("\n\t- Presence of rings");
            if (!_scoreResult.HasAnarchyGovernment) factors.Add("\n\t- Strong government presence");
            if (!_scoreResult.HasLowSecurity) factors.Add("\n\t- High security");
            if (!_scoreResult.HasPirateFaction) factors.Add("\n\t- Lack of pirate factions");
            if (_scoreResult.MarketDemandScore < 0.02) factors.Add("\n\t- Bad markets demand");
            if (_scoreResult.MarketDemandScore >= 0.02 && _scoreResult.MarketDemandScore <= 0.05 && _scoreResult.BestCommodity != null) factors.Add($"\n\t- Low market demand of {_scoreResult.BestCommodity.Name}");

            return factors.Count > 0 ? string.Join(", ", factors) : "overall poor conditions";
        }

        private string GetImprovementAreas()
        {
            var improvements = new List<string>();

            if (!_scoreResult.HasIndustrialEconomy && !_scoreResult.HasExtractionEconomy) improvements.Add("\n\t- Look for industrial/extraction economies");
            if (!_scoreResult.HasNoRings) improvements.Add("\n\t- Prioritize systems without rings");
            if (!_scoreResult.HasAnarchyGovernment) improvements.Add("\n\t- Seek anarchic systems");
            if (!_scoreResult.HasLowSecurity) improvements.Add("\n\t- Target low security systems");
            if (!_scoreResult.HasPirateFaction) improvements.Add("\n\t- Find systems with pirate factions");
            if (_scoreResult.MarketDemandScore < 0.02) improvements.Add("\n\t- Look for high demand markets");
            if (_scoreResult.MarketDemandScore >= 0.02 && _scoreResult.MarketDemandScore <= 0.05 && _scoreResult.BestCommodity != null) improvements.Add($"\n\t- The market have a demand of {_scoreResult.BestCommodity.Name} that is nice but not the best");


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