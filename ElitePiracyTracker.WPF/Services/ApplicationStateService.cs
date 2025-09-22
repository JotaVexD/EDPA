using ElitePiracyTracker.Models;
using ElitePiracyTracker.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ElitePiracyTracker.WPF.Services
{
    public class ApplicationStateService : INotifyPropertyChanged, IApiKeyProvider
    {
        private static ApplicationStateService _instance;
        private readonly SavedSystemsService _savedSystemsService;
        private readonly SecureSettingsService _secureSettings;

        public static ApplicationStateService Instance => _instance ??= new ApplicationStateService();

        // API Configuration properties
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
        public event PropertyChangedEventHandler PropertyChanged;

        // Search-related properties (not persisted)
        public ObservableCollection<PiracyScoreResult> SearchResults { get; set; } = new ObservableCollection<PiracyScoreResult>();
        public List<SystemData> systemData { get; set; } = new List<SystemData>();
        public string ReferenceSystem { get; set; } = "Sol";
        public int MaxDistance { get; set; } = 10;

        // Saved systems properties (persisted)
        public ObservableCollection<PiracyScoreResult> SavedSystemResults { get; set; } = new ObservableCollection<PiracyScoreResult>();
        public List<SystemData> systemSavedData { get; set; } = new List<SystemData>();
        public string ReferenceSavedSystem { get; set; } = "";

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

            this.systemSavedData = systemData;
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
    }
}