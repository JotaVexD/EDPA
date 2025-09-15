using ElitePiracyTracker.Models;
using ElitePiracyTracker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;

class Program
{
    static async Task Main(string[] args)
    {
        // Set up configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Set up dependency injection
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<HttpClient>()
            .AddMemoryCache()
            .AddSingleton<EDSMService>()
            .AddSingleton<PiracyScoringService>()
            .BuildServiceProvider();

        // Get the services
        var edsmService = serviceProvider.GetService<EDSMService>();
        var scoringService = serviceProvider.GetService<PiracyScoringService>();

        // Test the EDSM connection first
        //Console.WriteLine("Testing EDSM API connection...");
        //var connectionSuccessful = await edsmService.TestEDSMConnection();

        //if (!connectionSuccessful)
        //{
        //    Console.WriteLine("EDSM API connection test failed. Please check your configuration.");
        //    Console.WriteLine("1. Make sure your API key is correct in appsettings.json");
        //    Console.WriteLine("2. Check that you have a stable internet connection");
        //    Console.WriteLine("Note: You can use EDSM without an API key, but with rate limits");
        //    return;
        //}

        Console.WriteLine("=== Elite Dangerous Piracy Spot Finder ===");
        Console.WriteLine("Using EDSM API for system data");
        Console.WriteLine("Scoring based on: Traffic, Security, Faction State, and Ring Presence");
        Console.WriteLine("Enter system names to analyze (comma-separated), or 'quit' to exit:");

        while (true)
        {
            Console.Write("\nSystems to analyze: ");
            string input = Console.ReadLine();

            if (input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                break;

            var systemNames = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(s => s.Trim())
                                 .Where(s => !string.IsNullOrEmpty(s))
                                 .ToList();

            if (!systemNames.Any())
            {
                Console.WriteLine("Please enter at least one system name.");
                continue;
            }

            Console.WriteLine($"\nAnalyzing {systemNames.Count} systems...");

            var results = new List<PiracyScoreResult>();
            foreach (var systemName in systemNames)
            {
                Console.WriteLine($"- Fetching data for {systemName}...");
                var result = await scoringService.CalculateSystemScore(systemName);
                if (result != null)
                {
                    results.Add(result);
                }
                else
                {
                    Console.WriteLine($"  Failed to get data for {systemName}");
                }
            }

            // Display results
            Console.WriteLine("\n=== PIRACY SPOT ANALYSIS RESULTS ===");
            foreach (var result in results.OrderByDescending(r => r.FinalScore))
            {
                result.FinalScore *= 100;
                Console.WriteLine($"\n{result}");

                // Recommendation
                if (result.FinalScore >= 90)
                    Console.WriteLine("  ⭐ EXCELLENT PIRACY SPOT - Highly recommended!");
                else if (result.FinalScore >= 80)
                    Console.WriteLine("  ✓ Good piracy spot - Worth checking out");
                else if (result.FinalScore >= 70)
                    Console.WriteLine("  ~ Moderate piracy spot - Some potential");
                else
                    Console.WriteLine("  ✗ Poor piracy spot - Not recommended");
            }
        }

        Console.WriteLine("Thank you for using the Piracy Spot Finder. Fly dangerous, Commander! o7");
    }
}
