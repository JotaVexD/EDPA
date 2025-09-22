using ElitePiracyTracker.Models.EDSM;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using ElitePiracyTracker.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace ElitePiracyTracker.Services
{
    public class EDSMService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly IMemoryCache _cache;
        private readonly SemaphoreSlim _rateLimiter;
        private readonly PiracyScoringConfig _config;
        private int _remainingRequests = 30; // Conservative estimate
        private DateTime _lastRateLimitUpdate = DateTime.UtcNow;
        private readonly CacheService _cacheService;

        public EDSMService(HttpClient httpClient, IConfiguration configuration, IMemoryCache memoryCache, CacheService cacheService, IApiKeyProvider apiKeyProvider = null)
        {
            _httpClient = httpClient;

            _cacheService = cacheService;
            _baseUrl = configuration["ApiSettings:EDSM:BaseUrl"];

            if (apiKeyProvider != null && apiKeyProvider.IsApiConfigured)
            {
                _apiKey = apiKeyProvider.GetEdsmApiKey();
            }
            //_apiKey = ApplicationStateService.Instance.GetEdsmApiKey();

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("EDSM API key is not configured. Please set it in settings.");
            }
            _cache = memoryCache;

            // Load scoring configuration
            _config = new PiracyScoringConfig();
            // Load scoring parameters
            var scoringParams = configuration.GetSection("ScoringParameters");
            _config.DemandThresholds = scoringParams.GetSection("DemandThresholds").Get<Dictionary<string, double>>() ?? new Dictionary<string, double>();
            _config.ValuableCommodities = scoringParams.GetSection("ValuableCommodities").Get<Dictionary<string, double>>() ?? new Dictionary<string, double>();


            // Initialize rate limiter with 2 concurrent requests (respectful to EDSM)
            _rateLimiter = new SemaphoreSlim(2, 2);

            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ElitePiracyTracker/1.0.0");
        }

        private async Task<T> ExecuteWithRateLimiting<T>(Func<Task<T>> apiCall, string cacheKey = null, TimeSpan? cacheDuration = null)
        {
            // Check cache first
            if (cacheKey != null && _cache.TryGetValue(cacheKey, out T cachedResult))
            {
                return cachedResult;
            }

            await _rateLimiter.WaitAsync();
            try
            {
                // Check rate limits
                await CheckRateLimits();

                var result = await apiCall();

                // Cache the result if needed
                if (cacheKey != null && cacheDuration.HasValue && result != null)
                {
                    _cache.Set(cacheKey, result, cacheDuration.Value);
                }

                return result;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        private async Task CheckRateLimits()
        {
            // If we're running low on requests, delay accordingly
            if (_remainingRequests < 5)
            {
                var timeSinceLastUpdate = DateTime.UtcNow - _lastRateLimitUpdate;
                if (timeSinceLastUpdate.TotalMinutes < 60)
                {
                    // Estimated time until reset (60 minutes from last update)
                    var delayTime = TimeSpan.FromMinutes(60 - timeSinceLastUpdate.TotalMinutes);
                    await Task.Delay(delayTime);
                    _remainingRequests = 30; // Reset estimate after waiting
                }
            }
        }

        private void UpdateRateLimits(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("X-Rate-Limit-Remaining", out var remainingValues))
            {
                if (int.TryParse(remainingValues.FirstOrDefault(), out int remaining))
                {
                    _remainingRequests = remaining;
                    _lastRateLimitUpdate = DateTime.UtcNow;
                }
            }
        }

        public async Task<EDSMSystemResponse> GetSystemData(string systemName)
        {
            var cacheKey = $"edsm_system_{systemName.ToLower()}";
            return await ExecuteWithRateLimiting(async () =>
            {
                var url = $"api-v1/system?systemName={Uri.EscapeDataString(systemName)}&showInformation=1";

                if (!string.IsNullOrEmpty(_apiKey))
                {
                    url += $"&apiKey={_apiKey}";
                }

                var response = await _httpClient.GetAsync(url);
                UpdateRateLimits(response);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<EDSMSystemResponse>(content);
                }

                return null;
            }, cacheKey, TimeSpan.FromHours(1)); // Cache system data for 1 hour
        }

        public async Task<EDSMSystemResponse> GetBodiesData(string systemName)
        {
            var cacheKey = $"edsm_bodies_{systemName.ToLower()}";
            return await ExecuteWithRateLimiting(async () =>
            {
                var url = $"api-system-v1/bodies?systemName={Uri.EscapeDataString(systemName)}";

                if (!string.IsNullOrEmpty(_apiKey))
                {
                    url += $"&apiKey={_apiKey}";
                }

                var response = await _httpClient.GetAsync(url);
                UpdateRateLimits(response);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<EDSMSystemResponse>(content);
                }

                return null;
            }, cacheKey, TimeSpan.FromHours(6)); // Cache bodies data longer (less frequently updated)
        }

        public async Task<List<EDSMSstation>> GetStations(string systemName)
        {
            var cacheKey = $"edsm_stations_{systemName.ToLower()}";
            return await ExecuteWithRateLimiting(async () =>
            {
                var url = $"api-system-v1/stations?systemName={Uri.EscapeDataString(systemName)}";

                if (!string.IsNullOrEmpty(_apiKey))
                {
                    url += $"&apiKey={_apiKey}";
                }

                var response = await _httpClient.GetAsync(url);
                UpdateRateLimits(response);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var stationResponse = JsonConvert.DeserializeObject<EDSMSstationResponse>(content);
                    return stationResponse?.Stations ?? new List<EDSMSstation>();
                }

                return new List<EDSMSstation>();
            }, cacheKey, TimeSpan.FromHours(3)); // Cache stations for 3 hours
        }

        public async Task<EDSMMarketData> GetMarketData(long marketId)
        {
            var cacheKey = $"edsm_market_{marketId}";
            return await ExecuteWithRateLimiting(async () =>
            {
                var url = $"api-system-v1/stations/market?marketId={marketId}";

                if (!string.IsNullOrEmpty(_apiKey))
                {
                    url += $"&apiKey={_apiKey}";
                }

                var response = await _httpClient.GetAsync(url);
                UpdateRateLimits(response);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<EDSMMarketData>(content);
                }

                return null;
            }, cacheKey, TimeSpan.FromMinutes(30)); // Cache market data for 30 minutes (more volatile)
        }

        public async Task<SystemData> GetCompleteSystemData(string systemName)
        {
            var cacheKey = $"edsm_complete_{systemName.ToLower()}";

            return await ExecuteWithRateLimiting(async () =>
            {
                // Use parallel processing for multiple API calls
                var systemTask = GetSystemData(systemName);
                var bodiesTask = GetBodiesData(systemName);
                var stationsTask = GetStations(systemName);

                await Task.WhenAll(systemTask, bodiesTask, stationsTask);

                var systemData = new SystemData { Name = systemName };
                var systemResult = await systemTask;
                var bodiesResult = await bodiesTask;
                var stationsResult = await stationsTask;

                // Process system data
                if (systemResult != null && systemResult.Information != null)
                {
                    systemData.Security = systemResult.Information.Security;
                    systemData.Population = systemResult.Information.Population;
                    systemData.Economy = systemResult.Information.Economy;
                    systemData.SecondEconomy = systemResult.Information.SecondEconomy;
                    systemData.Government = systemResult.Information.Government;
                    systemData.Allegiance = systemResult.Information.Allegiance;
                    systemData.ControllingFaction = systemResult.Information.ControllingFaction;
                    systemData.FactionState = systemResult.Information.FactionState;
                }

                // Process traffic data
                if (systemResult != null && systemResult.Traffic != null)
                {
                    systemData.TrafficData = new TrafficData
                    {
                        Total = systemResult.Traffic.Total,
                        Week = systemResult.Traffic.Week,
                        Day = systemResult.Traffic.Day
                    };
                }

                // Process bodies data
                if (bodiesResult != null && bodiesResult.Bodies != null)
                {
                    foreach (var body in bodiesResult.Bodies)
                    {
                        systemData.Planets.Add(new Planet
                        {
                            Name = body.Name,
                            Type = body.Type,
                            HasRings = body.Rings != null && body.Rings.Count > 0,
                            DistanceFromArrivalLS = body.DistanceToArrival
                        });

                        if (body.Rings != null)
                        {
                            foreach (var ring in body.Rings)
                            {
                                systemData.Rings.Add(new Ring
                                {
                                    Type = ring.Type,
                                    Composition = ring.Type,
                                    IsPristine = false,
                                    DistanceFromSystemEntry = body.DistanceToArrival
                                });
                            }
                        }
                    }
                }

                // Process stations and market data in parallel
                if (stationsResult != null && stationsResult.Count > 0)
                {
                    var marketTasks = new List<Task>();

                    foreach (var station in stationsResult.Where(s => s.HaveMarket && s.MarketId > 0))
                    {
                        systemData.Stations.Add(new Station
                        {
                            Id = station.Id,
                            MarketId = station.MarketId,
                            Name = station.Name,
                            Type = station.Type,
                            DistanceToArrival = station.DistanceToArrival,
                            Allegiance = station.Allegiance,
                            Government = station.Government,
                            Economy = station.Economy,
                            HaveMarket = station.HaveMarket,
                            HaveShipyard = station.HaveShipyard,
                            ControllingFaction = station.ControllingFaction?.Name
                        });

                        // Queue market data task
                        marketTasks.Add(Task.Run(async () =>
                        {
                            var marketData = await GetMarketData(station.MarketId);
                            if (marketData != null && marketData.Commodities != null)
                            {
                                foreach (var commodity in marketData.Commodities)
                                {
                                    _config.DemandThresholds.TryGetValue("High", out var thresholds);

                                    
                                    if (_config.ValuableCommodities.ContainsKey(commodity.Name) && commodity.Demand >= thresholds)
                                    {
                                        systemData.BestCommoditie.Add(new CommodityMarket
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
                        }));
                    }

                    // Wait for all market data tasks to complete
                    await Task.WhenAll(marketTasks);
                }

                return systemData;
            }, cacheKey, TimeSpan.FromHours(2)); // Cache complete system data for 2 hours
        }
    }
}
