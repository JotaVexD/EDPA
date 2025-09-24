using EDPA.Models;
using EDPA.Models.EDSM;
using EDPA.Services;
using EDPA.WPF.Services;
using EDPA.WPF.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Navigation;
using Wpf.Ui.Controls;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace EDPA.WPF.Views.Pages
{
    public partial class SearchSystem : Page
    {
        private PiracyScoringService _scoringService;
        private EDSMService _edsmService;
        private SpanshSystemSearch _spanshSearcher;
        private CacheService _cacheService;
        private List<SystemData> _systemsData;
        private ICollectionView _resultsView;
        private List<PiracyScoreResult> _currentResults = new List<PiracyScoreResult>();
        private bool _isAnalyzing = false;
        private bool _isInitialized = false;
        private static SearchSystem _instance;

        public static SearchSystem Instance => _instance;

        public SearchSystem()
        {
            InitializeComponent();

            _instance = this;
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                // Enable page state preservation
                this.KeepAlive = true;

                // Load previous state
                ReferenceSystemTextBox.Text = ApplicationStateService.Instance.ReferenceSystem;
                MaxDistanceSlider.Value = ApplicationStateService.Instance.MaxDistance;
                _systemsData = ApplicationStateService.Instance.systemData;

                // Set up the sorted results view
                _resultsView = CollectionViewSource.GetDefaultView(ApplicationStateService.Instance.SearchResults);
                _resultsView.SortDescriptions.Add(new SortDescription("FinalScore", ListSortDirection.Descending));

                ResultsListView.SelectedItem = null;
                ResultsListView.ItemsSource = _resultsView;

                await InitializeServicesAsync();
                _isInitialized = true;
            }

            // Always update the UI state when the page is loaded
            UpdateUIState();
        }

        private void UpdateUIState()
        {
            ResultsListView.SelectedItem = null;
            // Update button states based on current results
            ExportButton.IsEnabled = ApplicationStateService.Instance.SearchResults.Count > 0;
            ClearResultsButton.IsEnabled = ApplicationStateService.Instance.SearchResults.Count > 0;

            // Update status label
            if (ApplicationStateService.Instance.SearchResults.Count > 0)
            {
                var bestSystem = ApplicationStateService.Instance.SearchResults.OrderByDescending(r => r.FinalScore).First();
                StatusLabel.Text = $"Found {ApplicationStateService.Instance.SearchResults.Count} results. Best system: {bestSystem.SystemName} ({bestSystem.FinalScore:F2}/100)";
            }
            else
            {
                StatusLabel.Text = "Ready to search.";
            }
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                // Set up configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                // Initialize the Services WPF
                ScoringWeightsProvider.Initialize(configuration);

                // Set up dependency injection - simplified without memory cache
                var serviceProvider = new ServiceCollection()
                    .AddSingleton<IConfiguration>(configuration)
                    .AddSingleton<HttpClient>()
                    .AddSingleton<IApiKeyProvider>(ApplicationStateService.Instance)
                    .AddSingleton<EDSMService>() // Now only needs HttpClient, IConfiguration, IApiKeyProvider
                    .AddSingleton<SpanshSystemSearch>()
                    .AddSingleton<PiracyScoringService>()
                    .AddSingleton<CacheService>(new CacheService(TimeSpan.FromHours(24)))
                    .BuildServiceProvider();

                // Get the services
                _edsmService = serviceProvider.GetService<EDSMService>();
                _spanshSearcher = serviceProvider.GetService<SpanshSystemSearch>();
                _scoringService = serviceProvider.GetService<PiracyScoringService>();
                _cacheService = serviceProvider.GetService<CacheService>();
            }
            catch (Exception ex)
            {
                await ShowUiMessageAsync("Initialization Error", $"Failed to initialize services: {ex.Message}", "OK");
            }
        }


        private async Task<MessageBoxResult> ShowUiMessageAsync(
            string title,
            string content,
            string primaryText = "OK",
            string secondaryText = null,
            string closeText = null)
        {
            var uiMessage = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryText
            };

            if (!string.IsNullOrWhiteSpace(secondaryText))
            {
                uiMessage.SecondaryButtonText = secondaryText;
                uiMessage.IsSecondaryButtonEnabled = true;
            }

            if (!string.IsNullOrWhiteSpace(closeText))
            {
                uiMessage.CloseButtonText = closeText;
                uiMessage.IsCloseButtonEnabled = true;
            }

            return await uiMessage.ShowDialogAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnalyzing || _spanshSearcher == null) return;

            try
            {
                _isAnalyzing = true;
                SetUiEnabled(false);

                string referenceSystem = ReferenceSystemTextBox.Text;
                int maxDistance = (int)MaxDistanceSlider.Value;

                ApplicationStateService.Instance.ReferenceSystem = referenceSystem;
                ApplicationStateService.Instance.MaxDistance = maxDistance;
                ApplicationStateService.Instance.SearchResults.Clear();

                if (string.IsNullOrEmpty(referenceSystem))
                {
                    SnackbarHelper.ShowError("Please enter a reference system.");
                    return;
                }

                StatusLabel.Text = $"Searching for systems near {referenceSystem} (within {maxDistance} ly)...";

                // Single cache key for fully scored results
                var cacheKey = $"Search_{referenceSystem}_{maxDistance}";

                // Use GetOrCreateAsync - much cleaner!
                var scoredSystems = await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () => await FetchAndScoreSystems(referenceSystem, maxDistance),
                    TimeSpan.FromHours(24)
                );

                if (scoredSystems != null && scoredSystems.Count > 0)
                {
                    await DisplayScoredSystems(scoredSystems);
                    SnackbarHelper.ShowSuccess($"Found {scoredSystems.Count} systems.");
                }
                else
                {
                    SnackbarHelper.ShowError("No systems found or analysis failed.");
                }
            }
            catch (Exception ex)
            {
                await ShowUiMessageAsync("Search Error", $"Error searching for systems: {ex.Message}", "OK");
                SnackbarHelper.ShowError("Search failed.");
            }
            finally
            {
                SetUiEnabled(true);
                _isAnalyzing = false;
            }
        }

        private async Task<List<SystemData>> FetchAndScoreSystems(string referenceSystem, int maxDistance)
        {
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.Value = 0;

            try
            {
                // Step 1: Fetch raw data from Spansh
                StatusLabel.Text = $"Fetching systems from Spansh...";
                var rawSystems = await _spanshSearcher.SearchSystemsNearReference(referenceSystem, maxDistance);

                if (rawSystems == null || rawSystems.Count == 0)
                {
                    return new List<SystemData>();
                }

                // Step 2: Score all systems with progress tracking
                StatusLabel.Text = $"Scoring {rawSystems.Count} systems...";
                ProgressBar.Maximum = rawSystems.Count;

                var scoredSystems = await ScoreSystemsInParallel(rawSystems);
                return scoredSystems;
            }
            finally
            {
                ProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<List<SystemData>> ScoreSystemsInParallel(List<SystemData> systems)
        {
            // Batch processing for better memory usage
            const int batchSize = 100;
            var results = new List<SystemData>();

            for (int i = 0; i < systems.Count; i += batchSize)
            {
                var batch = systems.Skip(i).Take(batchSize).ToList();
                var batchResults = await ProcessBatch(batch, i, systems.Count);
                results.AddRange(batchResults);

                // Force GC after each batch to manage memory
                GC.Collect(2, GCCollectionMode.Optimized, false, true);
            }


            return results;
        }

        private async Task<List<SystemData>> ProcessBatch(List<SystemData> batch, int startIndex, int totalCount)
        {
            var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
            var batchResults = new SystemData[batch.Count];
            var tasks = new List<Task>();

            for (int i = 0; i < batch.Count; i++)
            {
                var index = i;
                var system = batch[index];

                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var scoreResult = await _scoringService.CalculateSystemScore(systemData: system);
                        system.SystemScore.Add(scoreResult);
                        batchResults[index] = system;

                        // Update progress less frequently for better performance
                        if (index % 10 == 0) // Update every 10 systems instead of each one
                        {
                            await Dispatcher.InvokeAsync(() =>
                            {
                                ProgressBar.Value = startIndex + index + 1;
                                StatusLabel.Text = $"Scoring... ({startIndex + index + 1}/{totalCount})";
                            });
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return batchResults.Where(s => s != null).ToList();
        }

        private async Task DisplayScoredSystems(List<SystemData> scoredSystems)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                foreach (var system in scoredSystems)
                {
                    ApplicationStateService.Instance.SearchResults.AddRange(system.SystemScore);
                }

                _systemsData = scoredSystems;
                ApplicationStateService.Instance.systemData = scoredSystems;
                _resultsView.Refresh();
                UpdateUIState();
            });
        }

        private void ClosePopup_Click(object sender, RoutedEventArgs e)
        {
            FlyoutWarning.IsOpen = false;
            SetUiEnabled(true);
        }

        private void SetUiEnabled(bool enabled)
        {
            SearchButton.IsEnabled = enabled;
            ExportButton.IsEnabled = enabled && ApplicationStateService.Instance.SearchResults.Count > 0;
            ReferenceSystemTextBox.IsEnabled = enabled;
            MaxDistanceSlider.IsEnabled = enabled;
            ClearResultsButton.IsEnabled = enabled && ApplicationStateService.Instance.SearchResults.Count > 0;
        }

        private void ClearResultsButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationStateService.Instance.SearchResults.Clear();
            ApplicationStateService.Instance.systemData.Clear();
            _resultsView.Refresh();

            SnackbarHelper.ShowInfo("Results cleared. Ready to search.");
            UpdateUIState();
        }

        private void ResultsDataGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isAnalyzing && ResultsListView.SelectedItem is PiracyScoreResult selectedResult && selectedResult != null)
            {
                ApplicationStateService.Instance.ReferenceSystem = ReferenceSystemTextBox.Text;
                ApplicationStateService.Instance.MaxDistance = (int)MaxDistanceSlider.Value;
                ApplicationStateService.Instance.systemData = _systemsData;

                var sendSystem = _systemsData.Find(s => s.Name == selectedResult.SystemName);
                if (sendSystem != null)
                {
                    Application.Current.Properties["PreviousPageContent"] = this;
                    var systemDetailsPage = new SystemDetailsPage(selectedResult, sendSystem);

                    if (NavigationService != null)
                    {
                        NavigationService.Navigate(systemDetailsPage);
                    }
                    else
                    {
                        var parentFrame = FindParentFrame(this);
                        parentFrame?.Navigate(systemDetailsPage);
                    }
                }
            }
        }

        private Frame FindParentFrame(DependencyObject child)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is Frame))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as Frame;
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplicationStateService.Instance.SearchResults.Count == 0)
            {
                await ShowUiMessageAsync("Export Error", "No results to export.", "OK");
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Export Results",
                FileName = $"piracy_analysis_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new StreamWriter(saveDialog.FileName))
                    {
                        writer.WriteLine("System,Score,Economy,No Rings,Government,Security,Faction State,Market Demand");
                        foreach (var result in ApplicationStateService.Instance.SearchResults.OrderByDescending(r => r.FinalScore))
                        {
                            writer.WriteLine($"\"{result.SystemName}\",{result.FinalScore:F2},{result.EconomyScore:F2},{result.NoRingsScore:F2},{result.GovernmentScore:F2},{result.SecurityScore:F2},{result.FactionStateScore:F2},{result.MarketDemandScore:F2}");
                        }
                    }
                    await ShowUiMessageAsync("Export Successful", $"Results exported to {saveDialog.FileName}", "OK");
                }
                catch (Exception ex)
                {
                    await ShowUiMessageAsync("Export Error", $"Error exporting results: {ex.Message}", "OK");
                }
            }
        }
    }
}