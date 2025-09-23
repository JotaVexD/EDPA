using EDPA.Models;
using EDPA.Models.EDSM;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace EDPA.Services
{
    public class PiracyScoringService
    {
        private readonly PiracyScoringConfig _config;
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
            _config.PopulationMultipliers = scoringParams.GetSection("PopulationMultipliers").Get<Dictionary<string, double>>() ?? new Dictionary<string, double>();
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
                    var systems = await _spanshSearcher.SearchSystemsNearReference(systemName, 0);
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

            if (systemData == null || systemData.Name == "Test")
            {
                return null;
            }

            var result = new PiracyScoreResult { SystemName = systemData.Name };

            // Calculate all component scores
            result.EconomyScore = CalculateEconomyScore(systemData.Economy, systemData.SecondEconomy) * _config.EconomyScoreWeight;
            result.NoRingsScore = CalculateNoRingsScore(systemData) * _config.NoRingsScoreWeight;
            result.GovernmentScore = CalculateGovernmentScore(systemData.Government) * _config.GovernmentScoreWeight;
            result.SecurityScore = CalculateSecurityScore(systemData.Security) * _config.SecurityScoreWeight;
            result.FactionStateScore = CalculateFactionStateScore(systemData.FactionState) * _config.FactionStateScoreWeight;
            result.PopulationScore = CalculatePopulationScore(systemData.Population, _config.PopulationMultipliers) * _config.PopulationScoreWeight;

            result.HasIndustrialEconomy = systemData.Economy == "Industrial";
            result.HasExtractionEconomy = systemData.Economy == "Extraction";
            result.HasNoRings = systemData.Rings.Count == 0;
            result.HasAnarchyGovernment = systemData.Government == "Anarchy";
            result.HasLowSecurity = systemData.Security == "Low" || systemData.Security == "None" || systemData.Security == "Anarchy";
            result.HasPirateFaction = systemData.MinorFactionPresences.Any(f => f.Name.Contains("Pirate") || f.Name.Contains("Criminal"));

            // Calculate score without market demand
            double scoreWithoutMarket = result.EconomyScore + result.NoRingsScore +
                                       result.GovernmentScore + result.SecurityScore +
                                       result.FactionStateScore + result.PopulationScore;

            // Scale to 0-100 for comparison
            double scoreWithoutMarketScaled = scoreWithoutMarket * 100;

            // Check if we already have market data in the cached system
            bool hasMarketData = systemData.BestCommoditie != null && systemData.BestCommoditie.Count > 0;

            if (scoreWithoutMarketScaled >= 70 || hasMarketData)
            {
                result.SkippedMarket = false;

                if (hasMarketData)
                {
                    // Use existing market data from cache
                    result.MarketDemandScore = CalculateMarketDemandScoreFromExistingData(systemData) * _config.MarketDemandScoreWeight;
                }
                else
                {
                    // Fetch new market data
                    result.MarketDemandScore = await CalculateMarketDemandScore(systemData) * _config.MarketDemandScoreWeight;
                }
            }
            else
            {
                result.SkippedMarket = true;
                result.MarketDemandScore = 0;
            }

            // Calculate final score
            result.FinalScore = Math.Round(scoreWithoutMarket + result.MarketDemandScore, 2) * 100;

            // Get Best Commodity for display
            result.BestCommodity = GetBestCommoditySimple(systemData);

            // Cache the result if we have a system name
            if (!string.IsNullOrEmpty(systemName))
            {
                _resultCache[systemName] = result;
            }

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
            return primary * 0.7 + secondary * 0.3;
        }

        private double CalculateNoRingsScore(SystemData systemData)
        {
            // Check for rings
            bool hasRings = systemData.Rings.Count > 0;

            // Check for asteroid belts (Belt Cluster planets)
            bool hasBeltClusters = systemData.Planets.Any(p => p.Type != null &&
                p.Type.Equals("Belt Cluster", StringComparison.OrdinalIgnoreCase));

            // No rings or belts = good for piracy (score of 1.0)
            return !hasRings && !hasBeltClusters ? 1.0 : 0.0;
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

        public CommodityMarket GetBestCommoditySimple(SystemData systemData)
        {
            return systemData?.BestCommoditie?
                .Where(commodity => commodity != null && commodity.Demand > 0)
                .OrderByDescending(commodity => _config.ValuableCommodities.ContainsKey(commodity.Name)
                    ? _config.ValuableCommodities[commodity.Name]
                    : 0)  // Priority 1: Highest weight first
                .ThenByDescending(commodity => commodity.Demand)  // Priority 2: Highest demand for tie-breaking
                .FirstOrDefault();
        }

        public double CalculatePopulationScore(long population, Dictionary<string, double> multipliers)
        {
            if (population < 0)
                return 0.0; // Or throw an exception

            // Check ranges in order of most specific to least specific
            if (population >= 100000000 && population < 1000000000)
                return multipliers["100M-1B"];

            if (population >= 1000000 && population < 100000000)
                return multipliers["1M-100M"];

            if (population >= 1000000000 && population < 10000000000)
                return multipliers["1B-10B"];

            if (population >= 10000000000)
                return multipliers["10B+"];
            else
                // Default case: population < 1,000,000
                return multipliers["<1M"];
        }
        private async Task<double> CalculateMarketDemandScore(SystemData systemData)
        {
            // Clear any existing data to avoid duplicates
            systemData.BestCommoditie.Clear();

            if (systemData.Stations.Count == 0) return 0;

            double bestScore = 0;
            bool foundCommodities = false;

            // Get market data for each station with a market
            foreach (var station in systemData.Stations.Where(s => s.HaveMarket && s.MarketId > 0))
            {
                var marketData = await _edsmService.GetMarketData(station.MarketId);
                if (marketData != null && marketData.Commodities != null)
                {
                    foreach (var commodity in marketData.Commodities)
                    {
                        // Use the correct threshold - fix this line
                        double highThreshold = _config.DemandThresholds.ContainsKey("High")
                            ? _config.DemandThresholds["High"]
                            : 10000; // Default threshold

                        if (_config.ValuableCommodities.ContainsKey(commodity.Name) &&
                            commodity.Demand >= highThreshold)
                        {
                            if (_config.ValuableCommodities.TryGetValue(commodity.Name, out double multiplier))
                            {
                                var commodityMarket = new CommodityMarket
                                {
                                    Name = commodity.Name,
                                    BuyPrice = commodity.BuyPrice,
                                    SellPrice = commodity.SellPrice,
                                    Demand = commodity.Demand,
                                    Stock = commodity.Stock,
                                    DemandBracket = commodity.DemandBracket,
                                    StockBracket = commodity.StockBracket
                                };

                                systemData.BestCommoditie.Add(commodityMarket);
                                foundCommodities = true;

                                if (multiplier > bestScore)
                                {
                                    bestScore = multiplier;
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"Found {systemData.BestCommoditie.Count} commodities for {systemData.Name}");
            return bestScore;
        }

        private double CalculateMarketDemandScoreFromExistingData(SystemData systemData)
        {
            if (systemData.BestCommoditie == null || systemData.BestCommoditie.Count == 0)
                return 0;

            double bestScore = 0;
            foreach (var commodity in systemData.BestCommoditie)
            {
                if (_config.ValuableCommodities.TryGetValue(commodity.Name, out double multiplier))
                {
                    if (multiplier > bestScore)
                    {
                        bestScore = multiplier;
                    }
                }
            }
            return bestScore;
        }

        public List<CommodityMarket> ConvertEdsmCommoditiesToCommodityMarkets(List<EDSMCommodity> edsmCommodities)
        {
            return edsmCommodities.Select(edsmCommodity => new CommodityMarket
            {
                Name = edsmCommodity.Name,
                BuyPrice = edsmCommodity.BuyPrice,
                SellPrice = edsmCommodity.SellPrice,
                Demand = edsmCommodity.Demand,
                Stock = edsmCommodity.Stock,
                DemandBracket = edsmCommodity.DemandBracket,
                StockBracket = edsmCommodity.StockBracket
            }).ToList();
        }
    }
}