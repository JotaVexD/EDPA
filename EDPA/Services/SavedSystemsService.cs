// SavedSystemsService.cs
using EDPA.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace EDPA.Services
{
    public class SavedSystemsService
    {
        private readonly string _storagePath;
        private readonly TimeSpan _expirationTime = TimeSpan.FromHours(12);

        public SavedSystemsService()
        {
            _storagePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EDPA",
                "saved_systems.json"
            );

            // Ensure directory exists
            var directory = Path.GetDirectoryName(_storagePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public async Task SaveSystemsAsync(List<PiracyScoreResult> systems, List<SystemData> systemData)
        {
            try
            {
                var storageData = new SavedSystemsStorage
                {
                    Systems = systems,
                    SystemData = systemData,
                    LastSaved = DateTime.Now
                };

                var json = JsonSerializer.Serialize(storageData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_storagePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving systems: {ex.Message}");
            }
        }

        public async Task<(List<PiracyScoreResult> systems, List<SystemData> systemData)> LoadSystemsAsync()
        {
            try
            {
                if (!File.Exists(_storagePath))
                    return (new List<PiracyScoreResult>(), new List<SystemData>());

                var json = await File.ReadAllTextAsync(_storagePath);
                var storageData = JsonSerializer.Deserialize<SavedSystemsStorage>(json);

                if (storageData == null)
                    return (new List<PiracyScoreResult>(), new List<SystemData>());

                // Remove expired systems on load
                var now = DateTime.Now;
                var validSystems = new List<PiracyScoreResult>();
                var validSystemData = new List<SystemData>();

                for (int i = 0; i < storageData.Systems.Count; i++)
                {
                    var system = storageData.Systems[i];
                    if (now - system.SaveTimestamp <= _expirationTime)
                    {
                        validSystems.Add(system);
                        if (i < storageData.SystemData.Count)
                        {
                            validSystemData.Add(storageData.SystemData[i]);
                        }
                    }
                }

                // If we removed expired systems, save the cleaned list
                if (validSystems.Count != storageData.Systems.Count)
                {
                    await SaveSystemsAsync(validSystems, validSystemData);
                }

                return (validSystems, validSystemData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading saved systems: {ex.Message}");
                return (new List<PiracyScoreResult>(), new List<SystemData>());
            }
        }

        public void ClearSavedSystems()
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    File.Delete(_storagePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing saved systems: {ex.Message}");
            }
        }
    }

    public class SavedSystemsStorage
    {
        public List<PiracyScoreResult> Systems { get; set; } = new List<PiracyScoreResult>();
        public List<SystemData> SystemData { get; set; } = new List<SystemData>();
        public DateTime LastSaved { get; set; }
    }
}