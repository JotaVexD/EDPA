using ElitePiracyTracker.Models;
using ElitePiracyTracker.WPF.Models;
using ElitePiracyTracker.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ElitePiracyTracker.WPF.Views.Pages
{
    public partial class SystemDetailsPage : Page
    {
        public SystemDetailsPage(PiracyScoreResult selectedSystem)
        {
            InitializeComponent();
            DataContext = new SystemDetailsViewModel(selectedSystem);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back to the search page
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                // Fallback: create a new instance of the search page
                var searchPage = new SearchSystem();
                NavigationService?.Navigate(searchPage);
            }
        }
    }
}