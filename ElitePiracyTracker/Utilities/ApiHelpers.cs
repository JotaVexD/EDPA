using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace ElitePiracyTracker.Utilities
{
    public static class ApiHelpers
    {
        public static async Task<T> ReadAsJsonAsync<T>(this HttpContent content)
        {
            var json = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static HttpContent ToJsonContent(this object data)
        {
            var json = JsonConvert.SerializeObject(data);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        public static async Task<string> DebugApiCall(HttpClient client, string url, HttpMethod method, HttpContent content)
        {
            try
            {
                var request = new HttpRequestMessage(method, url);
                if (content != null)
                {
                    request.Content = content;
                }

                var response = await client.SendAsync(request);

                var responseContent = await response.Content.ReadAsStringAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType;

                Console.WriteLine($"API Debug: {url}");
                Console.WriteLine($"Status Code: {response.StatusCode}");
                Console.WriteLine($"Content Type: {contentType}");
                Console.WriteLine($"Response (first 200 chars): {responseContent.Substring(0, Math.Min(200, responseContent.Length))}");

                return responseContent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Debug Error: {ex.Message}");
                return null;
            }
        }
    }
}
