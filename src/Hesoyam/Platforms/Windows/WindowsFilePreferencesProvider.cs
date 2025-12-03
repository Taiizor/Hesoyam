using Hesoyam.Abstractions;
using Hesoyam.Services;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Hesoyam.Platforms.Windows
{
    /// <summary>
    /// File-based preferences provider for Windows that works in both packaged and unpackaged scenarios.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation stores preferences in a JSON file in the application's local data folder,
    /// avoiding the use of <c>ApplicationData.Current</c> which only works in packaged applications.
    /// </para>
    /// </remarks>
    internal sealed class WindowsFilePreferencesProvider : IPreferencesProvider
    {
        private readonly ConcurrentDictionary<string, string> _cache = new();
        private readonly string _filePath;
        private readonly object _fileLock = new();
        private bool _isLoaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsFilePreferencesProvider"/> class.
        /// </summary>
        public WindowsFilePreferencesProvider()
        {
            // Use LocalApplicationData which works for both packaged and unpackaged apps
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string hesoyamFolder = Path.Combine(appDataPath, "Hesoyam");

            // Ensure directory exists
            if (!Directory.Exists(hesoyamFolder))
            {
                Directory.CreateDirectory(hesoyamFolder);
            }

            _filePath = Path.Combine(hesoyamFolder, "preferences.json");
        }

        /// <inheritdoc />
        public string Get(string key, string defaultValue)
        {
            EnsureLoaded();
            return _cache.TryGetValue(key, out string? value) ? value : defaultValue;
        }

        /// <inheritdoc />
        public void Set(string key, string value)
        {
            EnsureLoaded();
            _cache[key] = value;
            Save();
        }

        /// <summary>
        /// Ensures the preferences are loaded from disk.
        /// </summary>
        private void EnsureLoaded()
        {
            if (_isLoaded)
            {
                return;
            }

            lock (_fileLock)
            {
                if (_isLoaded)
                {
                    return;
                }

                try
                {
                    if (File.Exists(_filePath))
                    {
                        string json = File.ReadAllText(_filePath);
                        Dictionary<string, string>? data = JsonSerializer.Deserialize(json, HesoyamJsonContext.Default.DictionaryStringString);
                        if (data != null)
                        {
                            foreach (KeyValuePair<string, string> kvp in data)
                            {
                                _cache[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
                {
                    // If loading fails due to file access or JSON parsing errors, start with empty cache
                    // This is intentional - we want to gracefully handle corrupted or inaccessible files
                }

                _isLoaded = true;
            }
        }

        /// <summary>
        /// Saves the preferences to disk.
        /// </summary>
        private void Save()
        {
            lock (_fileLock)
            {
                try
                {
                    Dictionary<string, string> data = new(_cache);
                    string json = JsonSerializer.Serialize(data, HesoyamJsonContext.Default.DictionaryStringString);
                    File.WriteAllText(_filePath, json);
                }
                catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
                {
                    // Silently handle save errors due to file access or serialization issues
                    // This is intentional - background task preferences are best-effort
                }
            }
        }
    }
}
