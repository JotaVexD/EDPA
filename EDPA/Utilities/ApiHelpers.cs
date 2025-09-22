using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace EDPA.Utilities
{
    public static class ApiHelpers
    {
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

        public class ApiException : Exception
        {
            public string ResponseContent { get; }

            public ApiException(string message) : base(message) { }

            public ApiException(string message, string responseContent) : base(message)
            {
                ResponseContent = responseContent;
            }

            public ApiException(string message, Exception innerException) : base(message, innerException) { }
        }

        // Interface for error handling (can be implemented to show flyouts, log errors, etc.)
        public interface IErrorHandler
        {
            void HandleApiError(ApiException ex);
            void HandleHttpError(HttpRequestException ex);
            void HandleUnexpectedError(Exception ex, string context = null);
        }
    }
}
