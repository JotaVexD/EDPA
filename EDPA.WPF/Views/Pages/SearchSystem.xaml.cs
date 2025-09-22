using EDPA.Models;
using EDPA.Services;
using EDPA.WPF.Services;
using EDPA.WPF.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
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
            // Update button states based on current results
            ExportButton.IsEnabled = ApplicationStateService.Instance.SearchResults.Count > 0;
            ClearResultsButton.IsEnabled = ApplicationStateService.Instance.SearchResults.Count > 0;

            // Update status label
            if (ApplicationStateService.Instance.SearchResults.Count > 0)
            {
                // Load previous state
                ReferenceSystemTextBox.Text = ApplicationStateService.Instance.ReferenceSystem;
                MaxDistanceSlider.Value = ApplicationStateService.Instance.MaxDistance;
                _systemsData = ApplicationStateService.Instance.systemData;

                ResultsListView.SelectedItem = null;
                ResultsListView.ItemsSource = _resultsView;

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

                // Set up dependency injection with caching
                var serviceProvider = new ServiceCollection()
                    .AddSingleton<IConfiguration>(configuration)
                    .AddSingleton<HttpClient>()
                    .AddSingleton<CacheService>(new CacheService(TimeSpan.FromHours(24)))
                    .AddMemoryCache()
                    .AddSingleton<IApiKeyProvider>(ApplicationStateService.Instance)
                    .AddSingleton<EDSMService>()
                    .AddSingleton<SpanshSystemSearch>()
                    .AddSingleton<PiracyScoringService>()
                    .BuildServiceProvider();

                // Get the services
                _edsmService = serviceProvider.GetService<EDSMService>();
                _spanshSearcher = serviceProvider.GetService<SpanshSystemSearch>();
                _scoringService = serviceProvider.GetService<PiracyScoringService>();
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

            // Show as dialog and wait for result (Primary / Secondary / None)
            return await uiMessage.ShowDialogAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnalyzing || _spanshSearcher == null) return;

            try
            {
                _isAnalyzing = true;
                SetUiEnabled(false);

                // Save search parameters
                ApplicationStateService.Instance.ReferenceSystem = ReferenceSystemTextBox.Text;
                int maxDistance = (int)MaxDistanceSlider.Value;
                ApplicationStateService.Instance.MaxDistance = maxDistance;

                // Clear previous results
                ApplicationStateService.Instance.SearchResults.Clear();

                // Get reference system and distance
                string referenceSystem = ReferenceSystemTextBox.Text;
                if (string.IsNullOrEmpty(referenceSystem))
                {
                    SnackbarHelper.ShowError("Please enter a reference system.");
                    await Task.Delay(100);
                    return;
                }

                // No need to parse since we're using a slider with fixed range
                StatusLabel.Text = $"Searching for systems near {referenceSystem} (within {maxDistance} ly)...";

                // Use the SpanshSystemSearch class to get complete system data
                var systems = await _spanshSearcher.SearchSystemsNearReference(referenceSystem, maxDistance);

                StatusLabel.Text = $"Found {systems.Count} systems near {referenceSystem} (within {maxDistance} ly). Analyzing...";

                // Analyze the systems using the SystemData objects we already have
                await AnalyzeSystems(systems);
            }
            catch (Exception ex)
            {
                await ShowUiMessageAsync("Search Error", $"Error searching for systems: {ex.Message}", "OK");
                SnackbarHelper.ShowError("Search failed.");
                //StatusLabel.Text = "Search failed.";
            }
            finally
            {
                SetUiEnabled(true);
                _isAnalyzing = false;
            }
        }


        private async Task AnalyzeSystems(List<SystemData> systemsData)
        {
            if (systemsData == null || systemsData.Count == 0)
            {
                FlyoutWarning.IsOpen = true;
                SetUiEnabled(false);
                return;
            }

            // Set up progress
            ProgressBar.Value = 0;
            ProgressBar.Maximum = systemsData.Count;
            ProgressBar.Visibility = Visibility.Visible;

            // Create progress reporter
            var progress = new Progress<AnalysisProgress>(report =>
            {
                ProgressBar.Value = report.Current;

                if (report.Result != null)
                {
                    ApplicationStateService.Instance.SearchResults.Add(report.Result);
                    _resultsView.Refresh();
                }

                // Optional: Update status with current system being processed
                StatusLabel.Text = $"Analyzing... ({report.Current}/{report.Total}) - {report.SystemName}";
            });

            try
            {
                // Use a semaphore to limit concurrent requests
                var semaphore = new SemaphoreSlim(5, 5);
                var tasks = new List<Task>();

                int completedCount = 0;
                var progressHandler = (IProgress<AnalysisProgress>)progress;

                foreach (var systemData in systemsData)
                {
                    await semaphore.WaitAsync();

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var result = await _scoringService.CalculateSystemScore(systemData: systemData);
                            if (result != null)
                            {
                                result.FinalScore *= 100;

                                Interlocked.Increment(ref completedCount);
                                progressHandler.Report(new AnalysisProgress
                                {
                                    Current = completedCount,
                                    Total = systemsData.Count,
                                    SystemName = systemData.Name,
                                    Result = result
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // Report error but don't block the UI
                            await Dispatcher.InvokeAsync(async () =>
                            {
                                await ShowUiMessageAsync("Analysis Error", $"Error analyzing {systemData.Name}: {ex.Message}", "OK");
                            });
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                // Use ConfigureAwait(false) to avoid deadlocking the UI thread
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await ShowUiMessageAsync("Analysis Error", $"Error during analysis: {ex.Message}", "OK");
                });
            }

            // Final updates on UI thread
            await Dispatcher.InvokeAsync(() =>
            {
                _systemsData = systemsData;
                ApplicationStateService.Instance.systemData = systemsData;

                if (ApplicationStateService.Instance.SearchResults.Count == 0)
                {
                    FlyoutWarning.IsOpen = true;
                    SetUiEnabled(false);
                }
                else
                {
                    SnackbarHelper.ShowInfo($"Analysis complete. Found {ApplicationStateService.Instance.SearchResults.Count} results.");
                }

                ProgressBar.Visibility = Visibility.Collapsed;
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

            SnackbarHelper.ShowInfo("Results cleared. Ready to search.");

            // Update UI state
            UpdateUIState();
        }

        private void ResultsDataGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isAnalyzing)
            {
                if (ResultsListView.SelectedItem is PiracyScoreResult selectedResult && selectedResult != null)
                {
                    // Save current state before navigating away
                    ApplicationStateService.Instance.ReferenceSystem = ReferenceSystemTextBox.Text;
                    ApplicationStateService.Instance.MaxDistance = (int)MaxDistanceSlider.Value;
                    ApplicationStateService.Instance.systemData = _systemsData;

                    // Find the corresponding system data
                    SystemData sendSystem = _systemsData.Find(s => s.Name == selectedResult.SystemName);

                    if (sendSystem != null)
                    {
                        Application.Current.Properties["PreviousPageContent"] = this;
                        // Navigate to the system details page
                        var systemDetailsPage = new SystemDetailsPage(selectedResult, sendSystem);

                        // Use the navigation service if available
                        if (NavigationService != null)
                        {
                            NavigationService.Navigate(systemDetailsPage);
                        }
                        else
                        {
                            // Fallback: try to find a parent frame
                            var parentFrame = FindParentFrame(this);
                            parentFrame?.Navigate(systemDetailsPage);
                        }
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

            // Implement export functionality
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
                    using (var writer = new System.IO.StreamWriter(saveDialog.FileName))
                    {
                        // Write header
                        writer.WriteLine("System,Score,Economy,No Rings,Government,Security,Faction State,Market Demand");

                        // Write data
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

        public class AnalysisProgress
        {
            public int Current { get; set; }
            public int Total { get; set; }
            public string SystemName { get; set; }
            public PiracyScoreResult Result { get; set; }
        }

        // Public method to refresh the UI when navigating back to this page
        public void RefreshUI()
        {
            UpdateUIState();
        }
    }
}