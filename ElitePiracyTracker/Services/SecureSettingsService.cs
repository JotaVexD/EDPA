// EliteDangerousPiracy/Services/SecureSettingsService.cs
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ElitePiracyTracker.Services
{
    public class SecureSettingsService : IApiKeyProvider
    {
        private readonly string _settingsFilePath;
        private readonly byte[] _entropy = Encoding.UTF8.GetBytes("EDPA");

        public bool IsApiConfigured => HasValidSettings();

        public SecureSettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "EDPA");
            Directory.CreateDirectory(appFolder);
            _settingsFilePath = Path.Combine(appFolder, "secure_settings.dat");
        }

        public class AppSettings
        {
            public string EdsmApiKey { get; set; } = string.Empty;
            public bool IsConfigured { get; set; }
        }

        public string GetEdsmApiKey()
        {
            var settings = LoadSettings();
            return settings.EdsmApiKey;
        }

        public AppSettings LoadSettings()
        {
            if (!File.Exists(_settingsFilePath))
                return new AppSettings();

            try
            {
                var encryptedData = File.ReadAllBytes(_settingsFilePath);
                var decryptedData = ProtectedData.Unprotect(encryptedData, _entropy, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(decryptedData);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings);
            var data = Encoding.UTF8.GetBytes(json);
            var encryptedData = ProtectedData.Protect(data, _entropy, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_settingsFilePath, encryptedData);
        }

        public bool HasValidSettings()
        {
            var settings = LoadSettings();
            return settings.IsConfigured && !string.IsNullOrWhiteSpace(settings.EdsmApiKey);
        }
    }
}