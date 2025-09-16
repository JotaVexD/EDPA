using ElitePiracyTracker.Models;
using System.Collections.ObjectModel;

namespace ElitePiracyTracker.WPF.Services
{
    public class ApplicationStateService
    {
        private static ApplicationStateService _instance;
        public static ApplicationStateService Instance => _instance ??= new ApplicationStateService();

        public ObservableCollection<PiracyScoreResult> SearchResults { get; set; } = new ObservableCollection<PiracyScoreResult>();
        public string ReferenceSystem { get; set; } = "Sol";
        public int MaxDistance { get; set; } = 10;

        private ApplicationStateService() { }
    }
}