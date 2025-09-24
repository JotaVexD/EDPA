using EDPA.Models;
using EDPA.Models.EDSM;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EDPA.Services
{
    public class PiracyScoringService
    {
        private readonly PiracyScoringConfig _config;
        private readonly EDSMService _edsmService;


        public PiracyScoringService(IConfiguration configuration, EDSMService edsmService)
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
            _config.PopulationMultipliers = scoringParams.GetSection("PopulationMultipliers").Get<Dictionary<string, double>>() ?? new Dictionary<string, double>();
            _config.DemandThresholds = scoringParams.GetSection("DemandThresholds").Get<Dictionary<string, double>>() ?? new Dictionary<string, double>();
            _config.ValuableCommodities = scoringParams.GetSection("ValuableCommodities").Get<Dictionary<string, double>>() ?? new Dictionary<string, double>();
        }

        public async Task<PiracyScoreResult> CalculateSystemScore(SystemData systemData)
        {
            
            if (systemData == null || systemData.Name == "Test")
            {
                return null;
            }

            var result = new PiracyScoreResult { SystemName = systemData.Name };

            // Calculate all component scores efficiently
            await CalculateScoresOptimized(systemData, result);

            return result;
        }

        private async Task CalculateScoresOptimized(SystemData systemData, PiracyScoreResult result)
        {
            // Calculate all simple scores first
            result.EconomyScore = CalculateEconomyScore(systemData.Economy, systemData.SecondEconomy) * _config.EconomyScoreWeight;

            // Use alternative rings scoring since API doesn't provide the data
            //result.NoRingsScore = await CalculateNoRingsScore(systemData) * _config.NoRingsScoreWeight;

            result.GovernmentScore = CalculateGovernmentScore(systemData.Government) * _config.GovernmentScoreWeight;
            result.SecurityScore = CalculateSecurityScore(systemData.Security) * _config.SecurityScoreWeight;
            result.FactionStateScore = CalculateFactionStateScore(systemData.FactionState) * _config.FactionStateScoreWeight;
            result.PopulationScore = CalculatePopulationScore(systemData.Population, _config.PopulationMultipliers) * _config.PopulationScoreWeight;

            // Set flags efficiently
            result.HasIndustrialEconomy = systemData.Economy == "Industrial";
            result.HasExtractionEconomy = systemData.Economy == "Extraction";
            result.HasNoRings = false; // We don't know - set to false or remove this property
            result.HasAnarchyGovernment = systemData.Government == "Anarchy";
            result.HasLowSecurity = systemData.Security == "Low" || systemData.Security == "None" || systemData.Security == "Anarchy";
            result.HasPirateFaction = systemData.MinorFactionPresences.Any(f =>
                f.Name?.Contains("Pirate") == true || f.Name?.Contains("Criminal") == true);

            // Calculate preliminary score
            double scoreWithoutMarket = result.EconomyScore + result.GovernmentScore 
                                      + result.SecurityScore + result.FactionStateScore + result.PopulationScore;

            double scoreWithoutMarketScaled = scoreWithoutMarket * 100;

            // Smart market data fetching - only fetch if score is promising or we already have data
            bool shouldFetchMarketData = scoreWithoutMarketScaled >= 75 ||
                                        (systemData.BestCommodities?.Count > 0);

            if (shouldFetchMarketData)
            {
                result.SkippedMarket = false;
                result.MarketDemandScore = systemData.BestCommodities?.Count > 0
                    ? CalculateMarketDemandScoreFromExistingData(systemData) * _config.MarketDemandScoreWeight
                    : await CalculateMarketDemandScoreOptimized(systemData) * _config.MarketDemandScoreWeight;
            }
            else
            {
                result.SkippedMarket = true;
                result.MarketDemandScore = 0;
            }

            result.FinalScore = Math.Round(scoreWithoutMarket + result.MarketDemandScore, 2) * 100;
            result.BestCommodity = GetBestCommoditySimple(systemData);
        }

        //private bool HasPirateFactionOptimized(SystemData systemData)
        //{
        //    // Use pre-calculated property or hashset for fast lookup
        //    return systemData.HasPirateFaction ||
        //           systemData.FactionNames.Any(name =>
        //               name.Contains("Pirate") || name.Contains("Criminal"));
        //}

        private double CalculateEconomyScore(string primaryEconomy, string secondaryEconomy)
        {
            double primary = _config.EconomyMultipliers.GetValueOrDefault(primaryEconomy, 0);
            double secondary = _config.EconomyMultipliers.GetValueOrDefault(secondaryEconomy, 0);

            return primary * 0.7 + secondary * 0.3;
        }

        //private double CalculateNoRingsScore(SystemData systemData)
        //{
        //    // Use pre-calculated properties for maximum speed
        //    return !systemData.HasRings && !systemData.HasBeltClusters ? 1.0 : 0.0;
        //}

        private double CalculateGovernmentScore(string government)
        {
            return string.IsNullOrEmpty(government)
                ? 0.0
                : _config.GovernmentMultipliers.GetValueOrDefault(government, 0.3);
        }

        private double CalculateSecurityScore(string security)
        {
            return string.IsNullOrEmpty(security)
                ? 0.0
                : _config.SecurityMultipliers.GetValueOrDefault(security, 0.0);
        }

        private double CalculateFactionStateScore(string factionState)
        {
            return string.IsNullOrEmpty(factionState)
                ? 0.0
                : _config.FactionStateMultipliers.GetValueOrDefault(factionState, 0.3);
        }

        public double CalculatePopulationScore(long population, Dictionary<string, double> multipliers)
        {
            if (population < 0) return 0.0;

            return population switch
            {
                >= 10000000000 => multipliers.GetValueOrDefault("10B+", 0),
                >= 1000000000 => multipliers.GetValueOrDefault("1B-10B", 0),
                >= 100000000 => multipliers.GetValueOrDefault("100M-1B", 0),
                >= 1000000 => multipliers.GetValueOrDefault("1M-100M", 0),
                _ => multipliers.GetValueOrDefault("<1M", 0)
            };
        }

        private async Task<double> CalculateMarketDemandScoreOptimized(SystemData systemData)
        {
            systemData.BestCommodities?.Clear();
            var stationsWithMarkets = systemData.Stations?
                .Where(s => s.HaveMarket && s.MarketId > 0)
                .ToList();

            if (stationsWithMarkets == null || stationsWithMarkets.Count == 0) return 0;

            // Process stations in parallel with limited concurrency
            var semaphore = new SemaphoreSlim(5);
            var marketTasks = stationsWithMarkets.Select(async station =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await _edsmService.GetMarketData(station.MarketId);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            var marketResults = await Task.WhenAll(marketTasks);

            // Process all commodities efficiently
            var allCommodities = new List<CommodityMarket>();
            double highThreshold = _config.DemandThresholds.GetValueOrDefault("High", 10000);

            foreach (var marketData in marketResults.Where(m => m?.Commodities != null))
            {
                foreach (var commodity in marketData.Commodities)
                {
                    if (_config.ValuableCommodities.ContainsKey(commodity.Name) &&
                        commodity.Demand >= highThreshold)
                    {
                        allCommodities.Add(new CommodityMarket
                        {
                            Name = commodity.Name,
                            BuyPrice = commodity.BuyPrice,
                            SellPrice = commodity.SellPrice,
                            Demand = commodity.Demand,
                            Stock = commodity.Stock,
                            DemandBracket = commodity.DemandBracket,
                            StockBracket = commodity.StockBracket
                        });
                    }
                }
            }

            systemData.BestCommodities = allCommodities;

            return allCommodities.Count > 0
                ? allCommodities.Max(c => _config.ValuableCommodities.GetValueOrDefault(c.Name, 0))
                : 0;
        }

        private double CalculateMarketDemandScoreFromExistingData(SystemData systemData)
        {
            return systemData.BestCommodities?
                .Where(c => c != null)
                .Select(c => _config.ValuableCommodities.GetValueOrDefault(c.Name, 0))
                .DefaultIfEmpty(0)
                .Max() ?? 0;
        }

        public CommodityMarket GetBestCommoditySimple(SystemData systemData)
        {
            return systemData?.BestCommodities?
                .Where(commodity => commodity?.Demand > 0)
                .OrderByDescending(commodity => _config.ValuableCommodities.GetValueOrDefault(commodity.Name, 0))
                .ThenByDescending(commodity => commodity.Demand)
                .FirstOrDefault();
        }

    }
}