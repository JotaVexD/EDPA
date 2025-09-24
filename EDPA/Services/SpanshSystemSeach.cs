using EDPA.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const int MaxPageSize = 500;
        private int page;

        public SpanshSystemSearch()
        {
            _httpClient = new HttpClient();
            SetupDefaultHeaders();
        }

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

        public async Task<List<SystemData>> SearchSystemsNearReference(string referenceSystem, int maxDistanceLy)
        {
            page = 0;

            try
            {
                var searchReference = await CreateSearch(referenceSystem, maxDistanceLy);
                if (string.IsNullOrEmpty(searchReference))
                {
                    throw new Exception("Failed to create search - no search reference returned");
                }

                await Task.Delay(500);

                var systems = await GetAllSearchResults(searchReference);
                return systems;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Spansh system search: {ex.Message}");
                return await GetSystemDataFallback(referenceSystem);
            }
        }

        private async Task<List<SystemData>> GetSystemDataFallback(string systemName)
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

                    var systemData = ParseSystemDataOptimized(systemElement);
                    return new List<SystemData> { systemData };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fallback API call failed: {ex.Message}");
            }

            return new List<SystemData>();
        }

        private async Task<string> CreateSearch(string referenceSystem, int maxDistanceLy)
        {
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
                size = MaxPageSize,
                reference_system = referenceSystem
            };

            string json = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://spansh.co.uk/api/systems/search/save");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();

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
            int page = 0;
            bool hasMoreResults = true;

            while (hasMoreResults)
            {
                try
                {
                    var url = page == 0
                        ? $"https://spansh.co.uk/api/systems/search/recall/{searchReference}"
                        : $"https://spansh.co.uk/api/systems/search/recall/{searchReference}/{page}";

                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("results", out var resultsElement) ||
                        resultsElement.ValueKind != JsonValueKind.Array ||
                        !resultsElement.EnumerateArray().Any())
                    {
                        break;
                    }

                    var pageSystems = new List<SystemData>();
                    foreach (var system in resultsElement.EnumerateArray())
                    {
                        var systemData = ParseSystemDataOptimized(system);
                        if (systemData != null && systemData.Name != "Test")
                        {
                            pageSystems.Add(systemData);
                        }
                    }

                    systems.AddRange(pageSystems);

                    if (pageSystems.Count < MaxPageSize)
                    {
                        hasMoreResults = false;
                    }
                    else
                    {
                        page++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving page {page}: {ex.Message}");
                    hasMoreResults = false;
                }
            }

            Console.WriteLine($"Retrieved {systems.Count} systems total");
            return systems;
        }


        private SystemData ParseSystemDataOptimized(JsonElement systemElement)
        {
            var systemData = new SystemData();

            // Parse only essential properties
            if (systemElement.TryGetProperty("name", out var nameElement))
                systemData.Name = nameElement.GetString() ?? string.Empty;

            systemData.Security = GetStringProperty(systemElement, "security");
            systemData.Population = GetInt64Property(systemElement, "population", 0);
            systemData.Economy = GetStringProperty(systemElement, "primary_economy");
            systemData.SecondEconomy = GetStringProperty(systemElement, "secondary_economy");
            systemData.Government = GetStringProperty(systemElement, "government");
            systemData.FactionState = GetStringProperty(systemElement, "controlling_minor_faction_state");

            // Parse stations (needed for market data) - with carrier filtering
            if (systemElement.TryGetProperty("stations", out var stationsElement))
                ParseStationsOptimized(systemData, stationsElement);

            // Parse factions (needed for pirate faction check)
            if (systemElement.TryGetProperty("minor_faction_presences", out var factionsElement))
                ParseFactionsOptimized(systemData, factionsElement);

            return systemData;
        }

        private void ParseFactionsOptimized(SystemData systemData, JsonElement factionsElement)
        {
            foreach (var faction in factionsElement.EnumerateArray())
            {
                if (faction.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
                {
                    var factionName = nameElement.GetString();
                    systemData.MinorFactionPresences.Add(new MinorFactionPresences { Name = factionName });
                }
            }
        }

        private void ParseStationsOptimized(SystemData systemData, JsonElement stationsElement)
        {
            foreach (var station in stationsElement.EnumerateArray())
            {
                // Skip carrier stations - only if type exists and contains "Carrier"
                if (station.TryGetProperty("type", out var stationTypeElement) &&
                    stationTypeElement.ValueKind == JsonValueKind.String)
                {
                    var stationType = stationTypeElement.GetString();
                    if (stationType?.Contains("Carrier") == true)
                        continue;
                }

                var systemStation = new Station
                {
                    Name = station.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : string.Empty,
                    Type = station.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : string.Empty,
                    HaveMarket = station.TryGetProperty("has_market", out var marketElement) && marketElement.GetBoolean(),
                    MarketId = station.TryGetProperty("market_id", out var marketIdElement) ? marketIdElement.GetInt64() : 0,
                    DistanceToArrival = station.TryGetProperty("distance_to_arrival", out var distanceElement)
                        ? distanceElement.GetDouble()
                        : 0
                };

                systemData.Stations.Add(systemStation);
            }
        }

        // Helper methods for faster property access
        private string GetStringProperty(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
                ? prop.GetString() ?? string.Empty
                : string.Empty;
        }

        private long GetInt64Property(JsonElement element, string propertyName, long defaultValue)
        {
            return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number
                ? prop.GetInt64()
                : defaultValue;
        }

    }
}