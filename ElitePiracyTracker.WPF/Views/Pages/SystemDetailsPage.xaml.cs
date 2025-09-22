using ElitePiracyTracker.Models;
using ElitePiracyTracker.WPF.Services;
using ElitePiracyTracker.WPF.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace ElitePiracyTracker.WPF.Views.Pages
{
    public partial class SystemDetailsPage : Page
    {
        private readonly PiracyScoreResult _scoreResult;
        private readonly SystemData _systemData;
        private bool _isAlreadySaved = false;

        public SystemDetailsPage(PiracyScoreResult selectedSystem, SystemData systemsData)
        {
            InitializeComponent();
            _scoreResult = selectedSystem;
            _systemData = systemsData;
            DataContext = new SystemDetailsViewModel(selectedSystem, systemsData);

            // Check if system is already saved
            CheckIfSystemIsAlreadySaved();

            // Update button visibility based on source and saved status
            UpdateSaveButtonVisibility();
        }

        private void CheckIfSystemIsAlreadySaved()
        {
            if (_scoreResult == null || string.IsNullOrEmpty(_scoreResult.SystemName))
                return;

            // Check if the system already exists in saved results
            _isAlreadySaved = ApplicationStateService.Instance.SavedSystemResults
                .Any(savedSystem => savedSystem.SystemName == _scoreResult.SystemName);
        }

        private void UpdateSaveButtonVisibility()
        {
            // Hide save button if coming from SavedSystems page OR if already saved
            if (_isAlreadySaved)
            {
                SaveButton.Visibility = Visibility.Collapsed;

                // Optional: Show a message or different button for already saved systems
                if (_isAlreadySaved)
                {
                    AlreadySavedText.Visibility = Visibility.Visible;
                    RemoveButton.Visibility = Visibility.Visible; // Optional: Add remove functionality
                }
            }
            else
            {
                SaveButton.Visibility = Visibility.Visible;
                AlreadySavedText.Visibility = Visibility.Collapsed;
                RemoveButton.Visibility = Visibility.Collapsed;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var previousPage = Application.Current.Properties["PreviousPageContent"] as Page;

            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                NavigationService?.Navigate(previousPage);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAlreadySaved) return;

            // Add timestamp
            _scoreResult.SaveTimestamp = DateTime.Now;

            // Ensure collections exist
            if (ApplicationStateService.Instance.systemSavedData == null)
                ApplicationStateService.Instance.systemSavedData = new List<SystemData>();

            // Save the system
            ApplicationStateService.Instance.SavedSystemResults.Add(_scoreResult);
            ApplicationStateService.Instance.systemSavedData.Add(_systemData);

            // Persist to storage
            await ApplicationStateService.Instance.SaveSavedSystemsAsync();

            // Update UI
            _isAlreadySaved = true;
            UpdateSaveButtonVisibility();

            SnackbarHelper.ShowSuccess($"{_scoreResult.SystemName} has been saved!");
        }

        // Optional: Add remove functionality for already saved systems
        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAlreadySaved) return;

            try
            {
                // Remove from saved results
                var systemToRemove = ApplicationStateService.Instance.SavedSystemResults
                    .FirstOrDefault(s => s.SystemName == _scoreResult.SystemName);

                if (systemToRemove != null)
                {
                    ApplicationStateService.Instance.SavedSystemResults.Remove(systemToRemove);
                }

                // Remove from system data
                var systemDataToRemove = ApplicationStateService.Instance.systemSavedData
                    ?.FirstOrDefault(sd => sd.Name == _scoreResult.SystemName);

                if (systemDataToRemove != null)
                {
                    ApplicationStateService.Instance.systemSavedData.Remove(systemDataToRemove);
                }

                // Persist the changes to saved_systems.json
                await ApplicationStateService.Instance.SaveSavedSystemsAsync();

                // Update the UI
                _isAlreadySaved = false;
                UpdateSaveButtonVisibility();

                SnackbarHelper.ShowCaution($"{_scoreResult.SystemName} has been removed.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing system from storage: {ex.Message}");

                // Show error message
                RemoveStatusText.Text = "✗ Error removing system";
                RemoveStatusText.Visibility = Visibility.Visible;

                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    await Task.Delay(3000);
                    RemoveStatusText.Visibility = Visibility.Collapsed;
                }));
            }
        }
    }
}