using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace EDPA.Services
{
    public class CacheService
    {
        private readonly string _cacheDirectory;
        private readonly TimeSpan _cacheDuration;
        private readonly JsonSerializerOptions _jsonOptions;

        public CacheService(TimeSpan cacheDuration)
        {
            _cacheDuration = cacheDuration;
            _cacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EDPA", "Cache"
            );

            Directory.CreateDirectory(_cacheDirectory);

            // Pre-configure JSON options for performance
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false, // ❌ Remove indentation - huge space savings!
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> createItem, TimeSpan? customExpiry = null)
        {
            var cacheFile = GetCacheFilePath(cacheKey);
            var expiry = customExpiry ?? _cacheDuration;

            // Fast path: check if file exists and is fresh
            if (File.Exists(cacheFile) && !IsFileExpired(cacheFile, expiry))
            {
                try
                {
                    return await ReadAndDeserialize<T>(cacheFile);
                }
                catch
                {
                    try { File.Delete(cacheFile); } catch { }
                }
            }

            // Slow path: create and cache
            var newData = await createItem();
            await SerializeAndWrite(cacheFile, newData);
            return newData;
        }

        private async Task<T> ReadAndDeserialize<T>(string filePath)
        {
            await using var fileStream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<T>(fileStream, _jsonOptions);
        }

        private async Task SerializeAndWrite<T>(string filePath, T data)
        {
            var tempFile = Path.Combine(Path.GetDirectoryName(filePath), Guid.NewGuid() + ".tmp");

            await using (var tempStream = File.Create(tempFile))
            {
                await JsonSerializer.SerializeAsync(tempStream, data, _jsonOptions);
            }

            File.Move(tempFile, filePath, true);
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

        private bool IsFileExpired(string filePath, TimeSpan expiry)
        {
            var fileInfo = new FileInfo(filePath);
            return DateTime.UtcNow - fileInfo.LastWriteTimeUtc > expiry;
        }
    }

    public class CacheEntry<T>
    {
        public T Data { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CacheInfo
    {
        public bool Exists { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsExpired { get; set; }
        public string FilePath { get; set; }
    }
}