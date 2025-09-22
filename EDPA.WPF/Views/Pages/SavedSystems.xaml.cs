using EDPA.Models;
using EDPA.WPF.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Wpf.Ui.Controls;

namespace EDPA.WPF.Views.Pages
{
    public partial class SavedSystems : Page
    {
        private ICollectionView _resultsView;
        private List<PiracyScoreResult> _currentSavedResults = new List<PiracyScoreResult>();
        private List<SystemData> _systemsSavedData;
        private static SavedSystems _instance;
        private bool _isInitialized = false;
        private TimeSpan _expirationTime = TimeSpan.FromHours(24);

        public SavedSystems()
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

                // Load saved systems from persistent storage
                await ApplicationStateService.Instance.LoadSavedSystemsAsync();

                // Load previous state (only the search text, not max distance)
                SearchTextBox.Text = ApplicationStateService.Instance.ReferenceSavedSystem;
                _systemsSavedData = ApplicationStateService.Instance.systemSavedData;

                // Set up the sorted results view with filtering
                _resultsView = CollectionViewSource.GetDefaultView(ApplicationStateService.Instance.SavedSystemResults);
                _resultsView.SortDescriptions.Add(new SortDescription("FinalScore", ListSortDirection.Descending));
                _resultsView.Filter = FilterSystems;

                ResultsListView.SelectedItem = null;
                ResultsListView.ItemsSource = _resultsView;

                _isInitialized = true;
            }

            // Check for expired systems (they should already be filtered out on load, but double-check)
            CheckAndRemoveExpiredSystems();
            UpdateUIState();
        }

        // Filter function for the search box
        private bool FilterSystems(object item)
        {
            if (string.IsNullOrEmpty(SearchTextBox.Text))
                return true;

            if (item is PiracyScoreResult system)
            {
                return system.SystemName.ToLower().Contains(SearchTextBox.Text.ToLower());
            }

            return false;
        }

        // Event handler for search text changes
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Refresh the filter
            _resultsView?.Refresh();

            UpdateUIState();
        }

        private void UpdateUIState()
        {
            // Update button states based on current results
            int totalCount = ApplicationStateService.Instance.SavedSystemResults.Count;
            int filteredCount = _resultsView?.Cast<object>().Count() ?? 0;

            // Update status label
            if (totalCount == 0)
            {
                StatusLabel.Text = "No saved systems.";
            }
            else if (filteredCount == 0 && !string.IsNullOrEmpty(SearchTextBox.Text))
            {
                StatusLabel.Text = "No systems match your search.";
            }
            else if (filteredCount == totalCount)
            {
                StatusLabel.Text = $"Showing all {totalCount} saved systems.";
            }
            else
            {
                StatusLabel.Text = $"Showing {filteredCount} of {totalCount} systems.";
            }

            // Update selection
            ResultsListView.SelectedItem = null;
            ResultsListView.ItemsSource = _resultsView;
        }

        private void ResultsSavedDataGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsListView.SelectedItem is PiracyScoreResult selectedResult && selectedResult != null)
            {
                // Save current state before navigating away
                ApplicationStateService.Instance.ReferenceSavedSystem = SearchTextBox.Text;

                // Find the corresponding system data
                SystemData sendSystem = _systemsSavedData.Find(s => s.Name == selectedResult.SystemName);

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

        private Frame FindParentFrame(DependencyObject child)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && !(parent is Frame))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as Frame;
        }

        private async void CheckAndRemoveExpiredSystems()
        {
            int expiredCount = 0;
            var now = DateTime.Now;

            var systemsToRemove = ApplicationStateService.Instance.SavedSystemResults
                .Where(system => IsSystemExpired(system, now))
                .ToList();

            foreach (var expiredSystem in systemsToRemove)
            {
                ApplicationStateService.Instance.SavedSystemResults.Remove(expiredSystem);

                var systemDataToRemove = _systemsSavedData?.FirstOrDefault(sd => sd.Name == expiredSystem.SystemName);
                if (systemDataToRemove != null)
                {
                    _systemsSavedData?.Remove(systemDataToRemove);
                }

                expiredCount++;
            }

            ApplicationStateService.Instance.systemSavedData = _systemsSavedData;

            // Persist the changes after removing expired systems
            if (expiredCount > 0)
            {
                await ApplicationStateService.Instance.SaveSavedSystemsAsync();

                var expirationMessage = $"Removed {expiredCount} expired system(s). Data was older than 12 hours.";
                StatusLabel.Text = expirationMessage;

                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    await Task.Delay(5000);
                    if (StatusLabel.Text == expirationMessage)
                    {
                        UpdateUIState();
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private async void ClearAllSaved_Click(object sender, RoutedEventArgs e)
        {
            await ApplicationStateService.Instance.ClearSavedSystemsAsync();
            UpdateUIState();
            StatusLabel.Text = "All saved systems cleared.";
        }

        private bool IsSystemExpired(PiracyScoreResult system, DateTime currentTime)
        {
            // If the system doesn't have a save timestamp, we can't determine expiration
            // You might want to add a SaveTimestamp property to PiracyScoreResult
            if (system.SaveTimestamp == DateTime.MinValue)
                return false;

            return (currentTime - system.SaveTimestamp) > _expirationTime;
        }
    }
}