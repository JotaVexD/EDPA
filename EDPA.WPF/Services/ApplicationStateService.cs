using EDPA.Models;
using EDPA.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace EDPA.WPF.Services
{
    public class ApplicationStateService : INotifyPropertyChanged, IApiKeyProvider
    {
        private static ApplicationStateService _instance;
        private readonly SavedSystemsService _savedSystemsService;
        private readonly SecureSettingsService _secureSettings;

        public static ApplicationStateService Instance => _instance ??= new ApplicationStateService();

        private bool _isApiConfigured;
        public bool IsApiConfigured
        {
            get => _isApiConfigured;
            private set
            {
                if (_isApiConfigured != value)
                {
                    _isApiConfigured = value;
                    OnPropertyChanged();
                    ApiConfigurationChanged?.Invoke(this, value);
                }
            }
        }

        // IApiKeyProvider implementation
        string IApiKeyProvider.GetEdsmApiKey() => _secureSettings.GetEdsmApiKey();
        bool IApiKeyProvider.IsApiConfigured => IsApiConfigured;

        public event EventHandler<bool> ApiConfigurationChanged;
        public event EventHandler<bool> IsAnalyzing;
        public event PropertyChangedEventHandler PropertyChanged;

        // Search-related properties (not persisted)
        public List<PiracyScoreResult> SearchResults { get; set; } = new List<PiracyScoreResult>();
        public List<SystemData> systemData { get; set; } = new List<SystemData>();
        public string ReferenceSystem { get; set; } = "Sol";
        public int MaxDistance { get; set; } = 10;
        public int TotalSearches { get; set; }
        public ObservableCollection<SystemData> SavedSystems { get; set; } = new();
        public DateTime? LastSearchTime { get; set; }
        public double AverageSearchTimeMs { get; set; }
        public double CacheHitRate { get; set; }

        // Saved systems properties (persisted)
        public ObservableCollection<PiracyScoreResult> SavedSystemResults { get; set; } = new ObservableCollection<PiracyScoreResult>();
        public List<SystemData> systemSavedData { get; set; } = new List<SystemData>();
        public string ReferenceSavedSystem { get; set; } = "";

        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 50; // Adjust as needed
        public int TotalPages => (int)Math.Ceiling((double)SearchResults.Count / PageSize);

        private ApplicationStateService()
        {
            _savedSystemsService = new SavedSystemsService();
            _secureSettings = new SecureSettingsService();

            // Check API configuration on startup
            CheckApiConfiguration();

            _ = LoadSavedSystemsAsync();
        }

        public void CheckApiConfiguration()
        {
            IsApiConfigured = _secureSettings.HasValidSettings();
        }

        public string GetEdsmApiKey()
        {
            return _secureSettings.GetEdsmApiKey();
        }

        public void SaveEdsmApiKey(string apiKey)
        {
            var settings = _secureSettings.LoadSettings();
            var oldValue = IsApiConfigured;

            settings.EdsmApiKey = apiKey?.Trim() ?? string.Empty;
            settings.IsConfigured = !string.IsNullOrWhiteSpace(apiKey);
            _secureSettings.SaveSettings(settings);

            // Always update the property to ensure UI binding updates
            IsApiConfigured = settings.IsConfigured;

            // Always raise the event, even if the value didn't change
            // (in case we want to force navigation)
            ApiConfigurationChanged?.Invoke(this, IsApiConfigured);

            Console.WriteLine($"API key saved. IsApiConfigured: {IsApiConfigured}, Event raised: {ApiConfigurationChanged != null}");
        }

        public async Task LoadSavedSystemsAsync()
        {
            var (systems, systemData) = await _savedSystemsService.LoadSystemsAsync();

            SavedSystemResults.Clear();
            foreach (var system in systems)
            {
                SavedSystemResults.Add(system);
            }

            systemSavedData = systemData;
        }

        public async Task SaveSavedSystemsAsync()
        {
            await _savedSystemsService.SaveSystemsAsync(
                new List<PiracyScoreResult>(SavedSystemResults),
                systemSavedData
            );
        }

        public async Task ClearSavedSystemsAsync()
        {
            SavedSystemResults.Clear();
            systemSavedData.Clear();
            _savedSystemsService.ClearSavedSystems();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<PiracyScoreResult> CurrentPageResults
        {
            get
            {
                var skip = (CurrentPage - 1) * PageSize;
                return new ObservableCollection<PiracyScoreResult>(
                    SearchResults.OrderByDescending(r => r.FinalScore)
                               .Skip(skip)
                               .Take(PageSize)
                );
            }
        }

        public void GoToPage(int page)
        {
            if (page >= 1 && page <= TotalPages)
            {
                CurrentPage = page;
                OnPropertyChanged(nameof(CurrentPageResults));
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(TotalPages));
            }
        }

        public void NextPage()
        {
            if (CurrentPage < TotalPages)
                GoToPage(CurrentPage + 1);
        }

        public void PreviousPage()
        {
            if (CurrentPage > 1)
                GoToPage(CurrentPage - 1);
        }

        public void ClearAllData()
        {
            SearchResults.Clear();
            systemData.Clear();
            CurrentPage = 1;

            OnPropertyChanged(nameof(SearchResults));
            OnPropertyChanged(nameof(systemData));
            OnPropertyChanged(nameof(CurrentPageResults));
            OnPropertyChanged(nameof(TotalPages));
        }
    }
}