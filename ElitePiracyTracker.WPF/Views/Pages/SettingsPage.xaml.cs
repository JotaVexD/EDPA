// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using ElitePiracyTracker.Services;
using ElitePiracyTracker.WPF.Services;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Appearance;

namespace ElitePiracyTracker.WPF.Views.Pages;

/// <summary>
/// Interaction logic for SettingsPage.xaml
/// </summary>
public partial class SettingsPage
{
    private string _apiKey;
    private readonly ApplicationStateService _appState;

    public event PropertyChangedEventHandler PropertyChanged;

    public string ApiKey
    {
        get => _apiKey;
        set
        {
            _apiKey = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ApiKey)));
        }
    }
    public SettingsPage()
    {
        InitializeComponent();
        _appState = ApplicationStateService.Instance;
        InfoBox.IsOpen = false;

        // Load current API key (will be empty for security)
        ApiKey = _appState.GetEdsmApiKey();

        // Show masked version for existing keys
        if (!string.IsNullOrEmpty(ApiKey))
        {
            ApiKeyPasswordBox.Password = "••••••••••••••••"; // Masked
        }

        AppVersionTextBlock.Text = $"EDPA - Elite Dangerous Piracy Analytics - {GetAssemblyVersion()}";
    }

    private void OnLightThemeRadioButtonChecked(object sender, RoutedEventArgs e)
    {
        Wpf.Ui.Appearance.ApplicationThemeManager.Apply(ApplicationTheme.Light);
    }

    private void OnDarkThemeRadioButtonChecked(object sender, RoutedEventArgs e)
    {
        Wpf.Ui.Appearance.ApplicationThemeManager.Apply(ApplicationTheme.Dark);
    }

    // In your SearchSystem.xaml.cs or a settings page
    private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var cacheService = new CacheService(TimeSpan.FromHours(12));
            cacheService.ClearAllCache();
            InfoBox.IsOpen = true;
            InfoBox.Severity = Wpf.Ui.Controls.InfoBarSeverity.Success;
            InfoBox.Message = "All cached data has been cleared.";
        }
        catch (Exception ex) {
            InfoBox.IsOpen = true;
            InfoBox.Severity = Wpf.Ui.Controls.InfoBarSeverity.Success;
            InfoBox.Message = $"Failed to clear cache.{ex.Message}";
        }
    }

    private static string GetAssemblyVersion()
    {
        return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? string.Empty;
    }

    private async void SaveApiKeyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // If the password is masked, don't save it again
            if (ApiKeyPasswordBox.Password == "••••••••••••••••")
            {
                ShowInfo("API key is already saved.", "Your API key is configured and secure.");
                return;
            }

            if (string.IsNullOrWhiteSpace(ApiKeyPasswordBox.Password))
            {
                ShowError("API key cannot be empty.", "Please enter your EDSM API key.");
                return;
            }

            // Save the actual API key
            _appState.SaveEdsmApiKey(ApiKeyPasswordBox.Password);

            ShowSuccess("API key saved successfully!", "Your EDSM API key has been securely stored.");

            // Clear the password box for security
            ApiKeyPasswordBox.Password = "••••••••••••••••";
        }
        catch (Exception ex)
        {
            ShowError("Failed to save API key", $"Error: {ex.Message}");
        }
    }

    private void ShowSuccess(string title, string message)
    {
        InfoBox.Title = title;
        InfoBox.Message = message;
        InfoBox.Severity = Wpf.Ui.Controls.InfoBarSeverity.Success;
        InfoBox.IsOpen = true;
    }

    private void ShowError(string title, string message)
    {
        InfoBox.Title = title;
        InfoBox.Message = message;
        InfoBox.Severity = Wpf.Ui.Controls.InfoBarSeverity.Error;
        InfoBox.IsOpen = true;
    }

    private void ShowInfo(string title, string message)
    {
        InfoBox.Title = title;
        InfoBox.Message = message;
        InfoBox.Severity = Wpf.Ui.Controls.InfoBarSeverity.Informational;
        InfoBox.IsOpen = true;
    }
}
