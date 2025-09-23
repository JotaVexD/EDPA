// CacheService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace EDPA.Services
{
    public class CacheService
    {
        private readonly string _cacheDirectory;
        private readonly TimeSpan _cacheDuration;

        public CacheService(TimeSpan cacheDuration)
        {
            _cacheDuration = cacheDuration;
            _cacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EDPA",
                "Cache"
            );

            // Ensure cache directory exists
            Directory.CreateDirectory(_cacheDirectory);
        }

        public async Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> createItem, TimeSpan? customExpiry = null)
        {
            var expiry = customExpiry ?? _cacheDuration;
            var cacheFile = GetCacheFilePath(cacheKey);

            // Try to read from cache
            if (File.Exists(cacheFile))
            {
                try
                {
                    var cacheEntry = await ReadCacheEntry<T>(cacheFile);
                    if (cacheEntry != null && !IsExpired(cacheEntry.CreatedAt, expiry))
                    {
                        return cacheEntry.Data;
                    }
                }
                catch (Exception ex)
                {
                    // If cache is corrupted, delete it and continue
                    File.Delete(cacheFile);
                }
            }

            // Create new data and cache it
            var newData = await createItem();
            await WriteCacheEntry(cacheFile, newData);
            return newData;
        }

        public void ClearExpiredCache()
        {
            if (!Directory.Exists(_cacheDirectory)) return;

            foreach (var file in Directory.GetFiles(_cacheDirectory, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);

                    // Simple regex to extract the createdAt date without full JSON parsing
                    var match = System.Text.RegularExpressions.Regex.Match(
                        json,
                        @"""createdAt"":\s*""([^""]+)""");

                    if (match.Success &&
                        DateTime.TryParse(match.Groups[1].Value, out var createdAt) &&
                        IsExpired(createdAt, _cacheDuration))
                    {
                        File.Delete(file);
                        Console.WriteLine($"Deleted expired cache: {Path.GetFileName(file)}");
                    }
                }
                catch
                {
                    // If anything fails, delete the corrupted file
                    File.Delete(file);
                }
            }
        }

        public void ClearAllCache()
        {
            if (Directory.Exists(_cacheDirectory))
            {
                Directory.Delete(_cacheDirectory, true);
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        private string GetCacheFilePath(string cacheKey)
        {
            // Create a safe filename from the cache key
            var safeKey = string.Join("_", cacheKey.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_cacheDirectory, $"{safeKey}.json");
        }

        private async Task<CacheEntry<T>> ReadCacheEntry<T>(string filePath)
        {
            var json = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true // Important for deserialization
            };
            return JsonSerializer.Deserialize<CacheEntry<T>>(json, options);
        }

        private async Task WriteCacheEntry<T>(string filePath, T data)
        {
            var cacheEntry = new CacheEntry<T>
            {
                Data = data,
                CreatedAt = DateTime.UtcNow
            };

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Ensure consistent naming
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(cacheEntry, jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }

        private bool IsExpired(DateTime createdAt, TimeSpan expiry)
        {
            return DateTime.UtcNow - createdAt > expiry;
        }
    }

    public class CacheEntry<T>
    {
        public T Data { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}