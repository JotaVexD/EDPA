using ElitePiracyTracker.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ElitePiracyTracker.Services
{
    public class PiracyScoringService
    {
        private readonly PiracyScoringConfig _config;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly EDSMService _edsmService;
        private readonly SpanshSystemSearch _spanshSearcher;
        private readonly Dictionary<string, SystemData> _systemCache = new Dictionary<string, SystemData>();
        private readonly Dictionary<string, PiracyScoreResult> _resultCache = new Dictionary<string, PiracyScoreResult>();

        public PiracyScoringService(IConfiguration configuration, HttpClient httpClient, EDSMService edsmService, SpanshSystemSearch spanshSearcher)
        {
            _edsmService = edsmService;
            _spanshSearcher = spanshSearcher;

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

        public async Task<PiracyScoreResult> CalculateSystemScore(string systemName = null, SystemData systemData = null)
        {
            // If we don't have system data, try to get it
            if (systemData == null && !string.IsNullOrEmpty(systemName))
            {
                // Check cache first
                if (_resultCache.TryGetValue(systemName, out var cachedResult))
                {
                    return cachedResult;
                }

                // Check system data cache
                if (!_systemCache.TryGetValue(systemName, out systemData))
                {
                    // First try to get data from Spansh
                    var systems = await _spanshSearcher.SearchSystemsNearReference(systemName, 0, 1);
                    systemData = systems?.FirstOrDefault(s => s.Name.Equals(systemName, StringComparison.OrdinalIgnoreCase));

                    // If not found in Spansh, fall back to EDSM
                    if (systemData == null)
                    {
                        systemData = await _edsmService.GetCompleteSystemData(systemName);
                    }

                    if (systemData == null)
                    {
                        return null;
                    }

                    // Cache the system data
                    _systemCache[systemName] = systemData;
                }
            }

            if (systemData == null)
            {
                return null;
            }

            var result = new PiracyScoreResult { SystemName = systemData.Name };

            // Calculate all component scores except market demand
            result.EconomyScore = CalculateEconomyScore(systemData.Economy, systemData.SecondEconomy) * _config.EconomyScoreWeight;
            result.NoRingsScore = CalculateNoRingsScore(systemData) * _config.NoRingsScoreWeight;
            result.GovernmentScore = CalculateGovernmentScore(systemData.Government) * _config.GovernmentScoreWeight;
            result.SecurityScore = CalculateSecurityScore(systemData.Security) * _config.SecurityScoreWeight;
            result.FactionStateScore = CalculateFactionStateScore(systemData.FactionState) * _config.FactionStateScoreWeight;

            // Calculate score without market demand
            double scoreWithoutMarket = result.EconomyScore + result.NoRingsScore +
                                       result.GovernmentScore + result.SecurityScore +
                                       result.FactionStateScore;

            // Scale to 0-100 for comparison
            double scoreWithoutMarketScaled = scoreWithoutMarket * 100;

            // Only calculate market demand if the score is already 70+
            if (scoreWithoutMarketScaled >= 70)
            {
                result.SkippedMarket = false;
                result.MarketDemandScore = await CalculateMarketDemandScore(systemData) * _config.MarketDemandScoreWeight;
            }
            else
            {
                result.SkippedMarket = true;
                result.MarketDemandScore = 0;
            }

            // Calculate final score
            result.FinalScore = Math.Round(scoreWithoutMarket + result.MarketDemandScore, 2);

            // Cache the result if we have a system name
            if (!string.IsNullOrEmpty(systemName))
            {
                _resultCache[systemName] = result;
            }

            return result;
        }

        // Add this method to your PiracyScoringService class
        public async Task<PiracyScoreResult> CalculateSystemScoreFromData(SystemData systemData)
        {
            if (systemData == null)
            {
                return null;
            }

            var result = new PiracyScoreResult { SystemName = systemData.Name };

            // Calculate all component scores except market demand
            result.EconomyScore = CalculateEconomyScore(systemData.Economy, systemData.SecondEconomy) * _config.EconomyScoreWeight;
            result.NoRingsScore = CalculateNoRingsScore(systemData) * _config.NoRingsScoreWeight;
            result.GovernmentScore = CalculateGovernmentScore(systemData.Government) * _config.GovernmentScoreWeight;
            result.SecurityScore = CalculateSecurityScore(systemData.Security) * _config.SecurityScoreWeight;
            result.FactionStateScore = CalculateFactionStateScore(systemData.FactionState) * _config.FactionStateScoreWeight;

            // Calculate score without market demand
            double scoreWithoutMarket = result.EconomyScore + result.NoRingsScore +
                                       result.GovernmentScore + result.SecurityScore +
                                       result.FactionStateScore;

            // Scale to 0-100 for comparison
            double scoreWithoutMarketScaled = scoreWithoutMarket * 100;

            // Only calculate market demand if the score is already 70+
            if (scoreWithoutMarketScaled >= 70)
            {
                result.SkippedMarket = false;
                result.MarketDemandScore = await CalculateMarketDemandScore(systemData) * _config.MarketDemandScoreWeight;
            }
            else
            {
                result.SkippedMarket = true;
                result.MarketDemandScore = 0;
            }

            // Calculate final score
            result.FinalScore = Math.Round(scoreWithoutMarket + result.MarketDemandScore, 2);

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

        private double CalculateNoRingsScore(SystemData systemData)
        {
            // Check for rings
            bool hasRings = systemData.Rings.Count > 0;

            // Check for asteroid belts (Belt Cluster planets)
            bool hasBeltClusters = systemData.Planets.Any(p => p.Type != null &&
                p.Type.Equals("Belt Cluster", StringComparison.OrdinalIgnoreCase));

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

        private async Task<double> CalculateMarketDemandScore(SystemData systemData)
        {
            if (systemData.Stations.Count == 0) return 0;

            double bestScore = 0;

            // Get market data for each station with a market
            foreach (var station in systemData.Stations.Where(s => s.HaveMarket && s.MarketId > 0))
            {
                var marketData = await _edsmService.GetMarketData(station.MarketId);
                if (marketData != null && marketData.Commodities != null)
                {
                    foreach (var commodity in marketData.Commodities)
                    {
                        _config.DemandThresholds.TryGetValue("High", out var thresholds);

                        if (_config.ValuableCommodities.ContainsKey(commodity.Name) && commodity.Demand >= thresholds)
                        {
                            if (_config.ValuableCommodities.TryGetValue(commodity.Name, out double multiplier))
                            {
                                if (multiplier > bestScore)
                                    bestScore = multiplier;
                            }
                        }
                    }
                }
            }

            return bestScore;
        }
    }
}