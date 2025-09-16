using ElitePiracyTracker.Models;
using ElitePiracyTracker.Services;
using ElitePiracyTracker.WPF.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;
using Wpf.Ui.Controls;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace ElitePiracyTracker.WPF.Views.Pages
{
    public partial class SearchSystem : Page
    {
        private PiracyScoringService _scoringService;
        private EDSMService _edsmService;
        private SpanshSystemSearch _spanshSearcher;
        private ICollectionView _resultsView;
        private List<PiracyScoreResult> _currentResults = new List<PiracyScoreResult>();
        private bool _isAnalyzing = false;
        public bool IsDisposed { get; private set; }


        public SearchSystem()
        {
            InitializeComponent();

            // Enable page state preservation
            this.KeepAlive = true;

            // Load previous state
            ReferenceSystemTextBox.Text = ApplicationStateService.Instance.ReferenceSystem;

            MaxDistanceSlider.Value = ApplicationStateService.Instance.MaxDistance;

            // Set up the sorted results view
            _resultsView = CollectionViewSource.GetDefaultView(ApplicationStateService.Instance.SearchResults);
            _resultsView.SortDescriptions.Add(new SortDescription("FinalScore", ListSortDirection.Descending));

            ResultsListView.ItemsSource = _resultsView;

            Loaded += async (s, e) =>
            {
                await InitializeServicesAsync();
            };
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

                // Set up dependency injection
                var serviceProvider = new ServiceCollection()
                    .AddSingleton<IConfiguration>(configuration)
                    .AddSingleton<HttpClient>()
                    .AddMemoryCache()
                    .AddSingleton<EDSMService>()
                    .AddSingleton<SpanshSystemSearch>()
                    .AddSingleton<PiracyScoringService>()
                    .BuildServiceProvider();

                // Get the services
                _edsmService = serviceProvider.GetService<EDSMService>();
                _spanshSearcher = serviceProvider.GetService<SpanshSystemSearch>();
                _scoringService = serviceProvider.GetService<PiracyScoringService>();

                //StatusLabel.Text = "Services initialized. Ready to analyze systems.";
            }
            catch (Exception ex)
            {
                // Use modern WPF-UI dialog (async)
                await ShowUiMessageAsync("Initialization Error", $"Failed to initialize services: {ex.Message}", "OK");

                //StatusLabel.Text = "Service initialization failed. Check appsettings.json.";
            }
        }

        // ---------------------
        // Modern WPF-UI Message helper
        // ---------------------
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
            if (_isAnalyzing) return;

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
                    await ShowUiMessageAsync("Input Error", "Please enter a reference system.", "OK");
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
                StatusLabel.Text = "Search failed.";
            }
            finally
            {
                SetUiEnabled(true);
                _isAnalyzing = false;
            }
        }

        private async Task AnalyzeSystems(List<SystemData> systemsData)
        {
            // Set up progress
            ProgressBar.Value = 0;
            ProgressBar.Maximum = systemsData.Count;
            ProgressBar.Visibility = Visibility.Visible;

            StatusLabel.Text = $"Analyzing {systemsData.Count} systems...";

            // Use a semaphore to limit concurrent requests
            var semaphore = new System.Threading.SemaphoreSlim(5, 5);
            var tasks = new List<Task>();

            foreach (var systemData in systemsData)
            {
                // Wait for an available slot
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        // Update UI on the main thread
                        await Dispatcher.InvokeAsync(() =>
                        {
                            StatusLabel.Text = $"Analyzing {systemData.Name}...";
                        });

                        // Use the system data directly
                        var result = await _scoringService.CalculateSystemScore(systemData: systemData);
                        if (result != null)
                        {
                            // Scale final score to 0-100
                            result.FinalScore *= 100;

                            // Update UI on the main thread
                            await Dispatcher.InvokeAsync(() =>
                            {
                                ApplicationStateService.Instance.SearchResults.Add(result);
                                // Refresh the sorted view
                                _resultsView.Refresh();
                                ProgressBar.Value++;
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        // Show modern dialog on UI thread
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

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            StatusLabel.Text = $"Analysis complete. Found {ApplicationStateService.Instance.SearchResults.Count} results.";

            // Highlight the best system
            if (ApplicationStateService.Instance.SearchResults.Count > 0)
            {
                var bestSystem = ApplicationStateService.Instance.SearchResults.OrderByDescending(r => r.FinalScore).First();
                StatusLabel.Text += $" Best system: {bestSystem.SystemName} ({bestSystem.FinalScore:F2}/100)";
            }

            ProgressBar.Visibility = Visibility.Collapsed;
        }

        private void SetUiEnabled(bool enabled)
        {
            SearchButton.IsEnabled = enabled;
            ExportButton.IsEnabled = enabled && _currentResults.Count > 0;
            ReferenceSystemTextBox.IsEnabled = enabled;
            MaxDistanceSlider.IsEnabled = enabled;
            ClearResultsButton.IsEnabled = enabled && _currentResults.Count > 0;
        }

        private void ClearResultsButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationStateService.Instance.SearchResults.Clear();
            StatusLabel.Text = "Results cleared. Ready to search.";
        }

        private void ResultsDataGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsListView.SelectedItem is PiracyScoreResult selectedResult)
            {
                // Save current state before navigating away
                ApplicationStateService.Instance.ReferenceSystem = ReferenceSystemTextBox.Text;

                // Save slider value
                ApplicationStateService.Instance.MaxDistance = (int)MaxDistanceSlider.Value;

                // Navigate to the system details page
                var systemDetailsPage = new SystemDetailsPage(selectedResult);

                // Use the navigation service if available
                if (NavigationService != null)
                {
                    NavigationService.Navigate(systemDetailsPage);
                }
                else
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                }
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
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
                        foreach (var result in _currentResults.OrderByDescending(r => r.FinalScore))
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