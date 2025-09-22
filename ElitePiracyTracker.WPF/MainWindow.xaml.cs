// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using ElitePiracyTracker.Services;
using ElitePiracyTracker.WPF.Services;
using ElitePiracyTracker.WPF.Views.Pages;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ElitePiracyTracker.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public ISnackbarService SnackbarService { get; private set; }
    private readonly ApplicationStateService _appState;

    public MainWindow()
    {
        DataContext = this;
        _appState = ApplicationStateService.Instance;

        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);

        // Initialize silent cache management
        InitializeCacheManagement();

        InitializeComponent();

        // Initialize the snackbar service
        SnackbarService = new SnackbarService();
        SnackbarService.SetSnackbarPresenter(RootSnackbarPresenter);

        // Subscribe to configuration changes
        _appState.ApiConfigurationChanged += OnApiConfigurationChanged;

        // Wait until the window is loaded to navigate
        Loaded += OnMainWindowLoaded;
    }

    private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Now the MainFrame should be fully initialized
        CheckApiConfigurationAndNavigate();

        // Remove the event handler so it doesn't fire again
        Loaded -= OnMainWindowLoaded;
    }

    private void CheckApiConfigurationAndNavigate()
    {
        try
        {
            if (!_appState.IsApiConfigured)
            {
                // Navigate to settings if API is not configured
                MainFrame.Navigate(typeof(SettingsPage));
                DashboardPage.Visibility = Visibility.Collapsed;
                SavedSystems.Visibility = Visibility.Collapsed;
                SearchSystem.Visibility = Visibility.Collapsed;
            }
            else
            {
                DashboardPage.Visibility = Visibility.Visible;
                SavedSystems.Visibility = Visibility.Visible;
                SearchSystem.Visibility = Visibility.Visible;
                // Navigate to dashboard if API is configured
                MainFrame.Navigate(typeof(DashboardPage));
               
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Navigation error: {ex.Message}");
            // Fallback to dashboard if navigation fails
            MainFrame.Navigate(typeof(DashboardPage));
        }
    }

    private void OnApiConfigurationChanged(object sender, bool isConfigured)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                if (isConfigured)
                {
                    DashboardPage.Visibility = Visibility.Visible;
                    SavedSystems.Visibility = Visibility.Visible;
                    SearchSystem.Visibility = Visibility.Visible;
                }
                else if (!isConfigured)
                {
                    DashboardPage.Visibility = Visibility.Collapsed;
                    SavedSystems.Visibility = Visibility.Collapsed;
                    SearchSystem.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API configuration change error: {ex.Message}");
            }
        });
    }

    private void InitializeCacheManagement()
    {
        // Automatically clear expired cache on startup
        Task.Run(() =>
        {
            try
            {
                var cacheService = new CacheService(TimeSpan.FromHours(24));
                cacheService.ClearExpiredCache();
                Console.WriteLine("Expired cache cleared automatically");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Automatic cache cleanup failed: {ex.Message}");
            }
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        // Unsubscribe from events
        _appState.ApiConfigurationChanged -= OnApiConfigurationChanged;
        base.OnClosed(e);
    }
}