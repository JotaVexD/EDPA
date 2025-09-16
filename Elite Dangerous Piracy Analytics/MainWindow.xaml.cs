using ElitePiracyTracker.Models;   // From your old project
using ElitePiracyTracker.Services; // From your old project
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Windows;

public partial class MainWindow : UiWindow
{
    private readonly PiracyScoringService _scoringService;
    private readonly EDSMService _edsmService;
    private readonly SpanshSystemSearch _spanshSearcher;

    public MainWindow()
    {
        InitializeComponent();
        InitializeServices(); // Your existing initialization logic
    }

    private void InitializeServices()
    {
        // Paste your service initialization code from your old WinForms project here
        // It should work exactly the same way!
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<HttpClient>()
            .AddMemoryCache()
            .AddSingleton<EDSMService>()
            .AddSingleton<SpanshSystemSearch>()
            .AddSingleton<PiracyScoringService>()
            .BuildServiceProvider();

        _edsmService = serviceProvider.GetService<EDSMService>();
        _spanshSearcher = serviceProvider.GetService<SpanshSystemSearch>();
        _scoringService = serviceProvider.GetService<PiracyScoringService>();
    }
}