using ElitePiracyTracker.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Net.Http;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using System.Threading.Tasks;
using static EliteAPI.Events.MarketEvent;

namespace ElitePiracyTracker.Services
{

    public class PiracyScoringService
    {
        private readonly PiracyScoringConfig _config;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly EDSMService _edsmService;

        public PiracyScoringService(IConfiguration configuration, HttpClient httpClient, EDSMService edsmService)
        {
            _edsmService = edsmService;

            // Load scoring configuration
            _config = new PiracyScoringConfig();
            configuration.GetSection("ScoringWeights").Bind(_config);

            // Load scoring parameters
            var scoringParams = configuration.GetSection("ScoringParameters");
            _config.EconomyMultipliers = scoringParams.GetSection("EconomyMultipliers").Get<Dictionary<string, double>>() ?? new Dictionary<string, double>();
            _config.GovernmentMultipliers = scoringParams.GetSection("GovernmentMultipliers").Get<Dictionary<string, double>>() ?? new Dictionary<string, double>();
            _config.SecurityMultipliers = scoringParams.GetSection("SecurityMultipliers").Get<Dictionary<string, double>>() ?? new Dictionary<string, double>();
            _config.FactionStateMultipliers = scoringParams.GetSection("FactionStateMultipliers").Get<Dictionary<string, double>>() ?? new Dictionary<string, double>();
            _config.DemandThresholds = scoringParams.GetSection("DemandThresholds").Get<Dictionary<string, double>>() ?? new Dictionary<string, double>();
            _config.ValuableCommodities = scoringParams.GetSection("ValuableCommodities").Get<Dictionary<string, double>>() ?? new Dictionary<string, double>();
        }

        public async Task<PiracyScoreResult> CalculateSystemScore(string systemName)
        {
            // Get complete system data with a single call
            var systemData = await _edsmService.GetCompleteSystemData(systemName);

            var result = new PiracyScoreResult { SystemName = systemName };

            // Calculate all component scores
            result.EconomyScore = CalculateEconomyScore(systemData.Economy, systemData.SecondEconomy) * _config.EconomyScoreWeight;
            result.NoRingsScore = CalculateNoRingsScore(systemData.Rings, systemData.Planets) * _config.NoRingsScoreWeight;
            result.GovernmentScore = CalculateGovernmentScore(systemData.Government) * _config.GovernmentScoreWeight;
            result.SecurityScore = CalculateSecurityScore(systemData.Security) * _config.SecurityScoreWeight;
            result.FactionStateScore = CalculateFactionStateScore(systemData.FactionState) * _config.FactionStateScoreWeight;
            result.MarketDemandScore = CalculateMarketDemandScore(systemData) * _config.MarketDemandScoreWeight;

            // Calculate final score
            result.FinalScore = Math.Round(result.EconomyScore + result.NoRingsScore +
                                         result.GovernmentScore +result.SecurityScore +
                                         result.FactionStateScore + result.MarketDemandScore, 2);

            return result;
        }

        private double CalculateEconomyScore(string primaryEconomy, string secondaryEconomy)
        {
            double primary = 0.0;
            double secondary = 0.0;

            // Get primary economy multiplier
            if (!string.IsNullOrEmpty(primaryEconomy) &&
                _config.EconomyMultipliers.TryGetValue(primaryEconomy, out var p))
            {
                primary = p;
            }

            // Get secondary economy multiplier
            if (!string.IsNullOrEmpty(secondaryEconomy) &&
                _config.EconomyMultipliers.TryGetValue(secondaryEconomy, out var s))
            {
                secondary = s;
            }

            // Weighted 70/30 blend
            return (primary * 0.7) + (secondary * 0.3);
        }

        private double CalculateNoRingsScore(List<Ring> rings, List<Planet> planets)
        {
            // Check for rings and asteroid belts
            bool hasRings = rings.Count > 0;
            bool hasBeltClusters = planets.Any(p => p.Type != null && p.Type.Equals("Belt Cluster", StringComparison.OrdinalIgnoreCase));

            // No rings or belts = good for piracy (score of 1.0)
            return (!hasRings && !hasBeltClusters) ? 1.0 : 0.0;
        }

        private double CalculateGovernmentScore(string government)
        {
            if (string.IsNullOrEmpty(government)) return 0.0;

            // Get government multiplier
            if (_config.GovernmentMultipliers.TryGetValue(government, out var multiplier))
            {
                return multiplier;
            }

            // Default value for unknown governments
            return 0.3;
        }

        private double CalculateSecurityScore(string security)
        {
            if (string.IsNullOrEmpty(security)) return 0.0;

            if (_config.SecurityMultipliers.TryGetValue(security, out var multiplier))
            {
                return multiplier;
            }

            return 0.0;
        }

        private double CalculateFactionStateScore(string factionState)
        {
            if (string.IsNullOrEmpty(factionState)) return 0.0;

            // Get faction state multiplier
            if (_config.FactionStateMultipliers.TryGetValue(factionState, out var multiplier))
            {
                return multiplier;
            }

            // Default value for unknown faction states
            return 0.3;
        }

        private double CalculateMarketDemandScore(SystemData systemData)
        {
            if (systemData.BestCommoditie.Count == 0) return 0;

            double bestScore = 0;

            foreach (var commodity in systemData.BestCommoditie)
            {
                if (_config.ValuableCommodities.TryGetValue(commodity.Name, out double multiplier))
                {
                    if (multiplier > bestScore)
                        bestScore = multiplier;
                }
            }

            return bestScore;
        }

    }
}