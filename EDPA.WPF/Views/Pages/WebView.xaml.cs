using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace EDPA.WPF.Views.Pages
{
    public partial class WebView : Page
    {
        private string _systemName;
        private long _systemId;

        public WebView(string systemName = "", long systemId = 0)
        {
            InitializeComponent();
            _systemName = systemName;
            _systemId = systemId;
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if it's already initialized to avoid doing it twice
                if (WebViewInara.CoreWebView2 == null)
                {
                    // Step 1: Create the environment (if you need specific options)
                    CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync();

                    // Step 2: Ensure the CoreWebView2 is ready
                    await WebViewInara.EnsureCoreWebView2Async(env);

                    // Step 3: NOW it is safe to set the Source or call Navigate
                    WebViewInara.Source = new Uri($"https://spansh.co.uk/system/{_systemId}");
                }
            }
            catch (Exception ex)
            {
                await ShowUiMessageAsync("Error", $"Failed to initialize WebView2: {ex.Message}", "OK");
            }
        }

        private void NavigateToSystem(string systemName)
        {
            var encodedName = Uri.EscapeDataString(systemName);
            var inaraUrl = $"https://inara.cz/elite/starsystem/?search={encodedName}";
            WebViewInara.Source = new Uri(inaraUrl);
        }

        private void WebView_NavigationStarting(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
            // Show loading indicator if needed
        }

        private void WebView_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            // Hide loading indicator if needed
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            WebViewInara?.Reload();
        }

        private void OpenInBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = WebViewInara.Source.ToString(),
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowUiMessageAsync("Error", $"Failed to open browser: {ex.Message}", "OK");
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var previousPage = Application.Current.Properties["BackToDetails"] as Page;

            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                NavigationService?.Navigate(previousPage);
            }
        }
    }
}