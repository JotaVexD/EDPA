using EDPA.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EDPA.Services
{
    public class SpanshSystemSearch
    {
        private readonly HttpClient _httpClient;
        private readonly CacheService _cacheService;
        private PiracyScoringService _piracyScoringService;
        private const int MaxPageSize = 500;
        private int page;
        public List<PiracyScoreResult> systemsScore { get; set; }

        public SpanshSystemSearch(CacheService cacheService)
        {
            _httpClient = new HttpClient();
            _cacheService = cacheService ?? new CacheService(TimeSpan.FromHours(24));
            SetupDefaultHeaders();
        }

        public List<PiracyScoreResult> GetResult()
        {
            return systemsScore;
        }

        public CacheService GetCacheService()
        {
            return _cacheService;
        }

        // Keep your existing constructor for backward compatibility
        public SpanshSystemSearch() : this(new CacheService(TimeSpan.FromHours(24))) { }

        private void SetupDefaultHeaders()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("accept", "application/json, text/javascript, */*; q=0.01");
            _httpClient.DefaultRequestHeaders.Add("accept-language", "en-US,en;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("origin", "https://spansh.co.uk");
            _httpClient.DefaultRequestHeaders.Add("priority", "u=1, i");
            _httpClient.DefaultRequestHeaders.Add("referer", "https://spansh.co.uk/systems");
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Chromium\";v=\"140\", \"Not=A?Brand\";v=\"24\", \"Brave\";v=\"140\"");
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            _httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
            _httpClient.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
            _httpClient.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
            _httpClient.DefaultRequestHeaders.Add("sec-fetch-site", "same-origin");
            _httpClient.DefaultRequestHeaders.Add("sec-gpc", "1");
            _httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
        }

        public async Task<List<SystemData>> SearchSystemsNearReference(string referenceSystem, int maxDistanceLy, PiracyScoringService piracyScoringService)
        {
            //systemsScore = new List<PiracyScoreResult>();
            // Create a unique cache key for this search
            var cacheKey = $"Search_{referenceSystem}_{maxDistanceLy}";
            _piracyScoringService = piracyScoringService;
            page = 0;

            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                try
                {
                    // Step 1: Create the search request
                    var searchReference = await CreateSearch(referenceSystem, maxDistanceLy);

                    if (string.IsNullOrEmpty(searchReference))
                    {
                        throw new Exception("Failed to create search - no search reference returned");
                    }

                    // Add a small delay to ensure the search is ready
                    await Task.Delay(500);

                    // Step 2: Retrieve ALL search results with pagination
                    var systems = await GetAllSearchResults(searchReference);
                    return systems;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in Spansh system search: {ex.Message}");
                    // Fall back to individual system lookup
                    return await GetSystemDataFallback(referenceSystem);
                }
            });
        }

        private async Task<List<SystemData>> GetSystemDataFallback(string systemName)
        {
            var cacheKey = $"System_{systemName}";

            return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                try
                {
                    var url = $"https://spansh.co.uk/api/systems/{Uri.EscapeDataString(systemName)}";

                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    var response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        using JsonDocument doc = JsonDocument.Parse(responseBody);
                        var systemElement = doc.RootElement;

                        var systemData = ParseSystemData(systemElement);
                        return new List<SystemData> { systemData };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fallback API call failed: {ex.Message}");
                }

                return new List<SystemData>();
            });
        }

        public async Task UpdateSearchCache(string cacheKey, List<SystemData> systemsData)
        {
            try
            {
                await _cacheService.GetOrCreateAsync(cacheKey, async () => systemsData, TimeSpan.FromHours(24));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating search cache: {ex.Message}");
            }
        }

        private async Task<string> CreateSearch(string referenceSystem, int maxDistanceLy)
        {
            // Use MaxPageSize to get the maximum systems per page
            var payload = new
            {
                filters = new
                {
                    distance = new
                    {
                        min = "0",
                        max = maxDistanceLy.ToString()
                    }
                },
                sort = Array.Empty<object>(),
                size = MaxPageSize, // Get maximum systems per page
                reference_system = referenceSystem
            };

            string json = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://spansh.co.uk/api/systems/search/save");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();

            // Parse the response to get the search reference
            using JsonDocument doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("search_reference", out var searchRefElement))
            {
                return searchRefElement.GetString();
            }

            throw new Exception("Search reference not found in response");
        }

        private async Task<List<SystemData>> GetAllSearchResults(string searchReference)
        {
            var systems = new List<SystemData>();
            var semaphore = new SemaphoreSlim(50); // Limit concurrent scoring tasks
            bool hasMoreResults = true;

            while (hasMoreResults)
            {
                try
                {
                    var url = page == 0
                        ? $"https://spansh.co.uk/api/systems/search/recall/{searchReference}"
                        : $"https://spansh.co.uk/api/systems/search/recall/{searchReference}/{page}";

                    using var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("results", out var resultsElement) ||
                        resultsElement.ValueKind != JsonValueKind.Array)
                    {
                        break;
                    }

                    // Parse systems in parallel
                    var systemsArray = resultsElement.EnumerateArray().ToArray();
                    var pageSystems = new ConcurrentBag<SystemData>();
                    var scoringTasks = new List<Task>();

                    foreach (var system in systemsArray)
                    {
                        if (system.TryGetProperty("name", out var name) && name.GetString() == "Test")
                            continue;

                        await semaphore.WaitAsync();
                        scoringTasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                var systemData = ParseSystemData(system);
                                if (systemData != null)
                                {
                                    var scoreResult = await _piracyScoringService.CalculateSystemScore(systemData: systemData);
                                    systemData.SystemScore = new List<PiracyScoreResult> { scoreResult };
                                    pageSystems.Add(systemData);
                                }
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }));
                    }

                    await Task.WhenAll(scoringTasks);
                    systems.AddRange(pageSystems);

                    // Check if last page
                    if (systemsArray.Length < MaxPageSize)
                    {
                        hasMoreResults = false;
                    }
                    else
                    {
                        page++;
                        await Task.Delay(100); // Reduced delay
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving page {page}: {ex.Message}");
                    hasMoreResults = false;
                }
            }

            return systems;
        }

        private SystemData ParseSystemData(JsonElement systemElement)
        {
            var systemData = new SystemData();

            // Basic system info
            if (systemElement.TryGetProperty("name", out var nameElement))
            {
                systemData.Name = nameElement.GetString();
            }

            if (systemElement.TryGetProperty("security", out var securityElement))
            {
                systemData.Security = securityElement.GetString();
            }

            if (systemElement.TryGetProperty("population", out var populationElement))
            {
                systemData.Population = populationElement.GetInt64();
            }

            if (systemElement.TryGetProperty("primary_economy", out var primaryEconomyElement))
            {
                systemData.Economy = primaryEconomyElement.GetString();
            }

            if (systemElement.TryGetProperty("secondary_economy", out var secondaryEconomyElement))
            {
                systemData.SecondEconomy = secondaryEconomyElement.GetString();
            }

            if (systemElement.TryGetProperty("government", out var governmentElement))
            {
                systemData.Government = governmentElement.GetString();
            }

            if (systemElement.TryGetProperty("controlling_minor_faction_state", out var factionStateElement))
            {
                systemData.FactionState = factionStateElement.GetString();
            }

            // Parse bodies and check for rings
            if (systemElement.TryGetProperty("bodies", out var bodiesElement) && bodiesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var body in bodiesElement.EnumerateArray())
                {
                    var planet = new Planet();

                    if (body.TryGetProperty("name", out var bodyNameElement))
                    {
                        planet.Name = bodyNameElement.GetString();
                    }

                    if (body.TryGetProperty("type", out var bodyTypeElement))
                    {
                        planet.Type = bodyTypeElement.GetString();
                    }

                    if (body.TryGetProperty("distance_to_arrival", out var distanceElement))
                    {
                        planet.DistanceFromArrivalLS = distanceElement.GetDouble();
                    }

                    // Check for rings and add them to the system
                    if (body.TryGetProperty("rings", out var ringsElement) && ringsElement.ValueKind == JsonValueKind.Array)
                    {
                        planet.HasRings = true;

                        foreach (var ring in ringsElement.EnumerateArray())
                        {
                            var systemRing = new Ring();

                            if (ring.TryGetProperty("name", out var ringNameElement))
                            {
                                systemRing.Type = ringNameElement.GetString();
                            }

                            if (ring.TryGetProperty("type", out var ringTypeElement))
                            {
                                systemRing.Composition = ringTypeElement.GetString();
                            }

                            if (body.TryGetProperty("distance_to_arrival", out var ringDistanceElement))
                            {
                                systemRing.DistanceFromSystemEntry = ringDistanceElement.GetDouble();
                            }

                            systemData.Rings.Add(systemRing);
                        }
                    }

                    systemData.Planets.Add(planet);
                }
            }

            // Parse stations and their market IDs
            if (systemElement.TryGetProperty("stations", out var stationsElement) && stationsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var station in stationsElement.EnumerateArray())
                {
                    var systemStation = new Station();

                    if (station.TryGetProperty("name", out var stationNameElement))
                    {
                        systemStation.Name = stationNameElement.GetString();
                    }

                    if (station.TryGetProperty("type", out var stationTypeElement))
                    {
                        systemStation.Type = stationTypeElement.GetString();
                    }

                    if (station.TryGetProperty("distance_to_arrival", out var stationDistanceElement))
                    {
                        systemStation.DistanceToArrival = stationDistanceElement.GetDouble();
                    }

                    if (station.TryGetProperty("has_market", out var hasMarketElement))
                    {
                        systemStation.HaveMarket = hasMarketElement.GetBoolean();
                    }

                    if (station.TryGetProperty("market_id", out var marketIdElement))
                    {
                        systemStation.MarketId = marketIdElement.GetInt64();
                    }

                    systemData.Stations.Add(systemStation);
                }
            }

            if (systemElement.TryGetProperty("minor_faction_presences", out var minorFactionPresenceElement) && minorFactionPresenceElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var faction in minorFactionPresenceElement.EnumerateArray())
                {

                    var minorFaction = new MinorFactionPresences();

                    if (faction.TryGetProperty("name", out var minorFactionNameElement))
                    {
                        minorFaction.Name = minorFactionNameElement.GetString();
                    }

                    if (faction.TryGetProperty("state", out var minorFactionStateElement))
                    {
                        minorFaction.State = minorFactionStateElement.GetString();
                    }

                    if (faction.TryGetProperty("influence", out var minorFactionInfluenceElement))
                    {
                        minorFaction.Influence = minorFactionInfluenceElement.GetDouble();
                    }

                    systemData.MinorFactionPresences.Add(minorFaction);
                }
            }

            return systemData;
        }
    }
}