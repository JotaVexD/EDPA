using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using EDPA.Models.EDSM;

namespace EDPA.Services
{
    public class EDSMService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly SemaphoreSlim _rateLimiter;

        public EDSMService(HttpClient httpClient, IConfiguration configuration, IApiKeyProvider apiKeyProvider = null)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:EDSM:BaseUrl"];

            if (apiKeyProvider != null && apiKeyProvider.IsApiConfigured)
            {
                _apiKey = apiKeyProvider.GetEdsmApiKey();
            }

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("EDSM API key is not configured. Please set it in settings.");
            }

            // Simple rate limiter - 2 concurrent requests
            _rateLimiter = new SemaphoreSlim(2, 2);

            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ElitePiracyTracker/1.0.0");
        }

        public async Task<EDSMMarketData> GetMarketData(long marketId)
        {
            await _rateLimiter.WaitAsync();
            try
            {
                var url = $"api-system-v1/stations/market?marketId={marketId}&apiKey={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<EDSMMarketData>(content);
                }

                return null;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
    }
}