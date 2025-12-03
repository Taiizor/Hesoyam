namespace Hesoyam.Abstractions
{
    /// <summary>
    /// Interface for preferences storage abstraction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides an abstraction layer over platform-specific preferences storage.
    /// On MAUI platforms, this is implemented using the Preferences API. On other platforms,
    /// an in-memory implementation is used by default.
    /// </para>
    /// </remarks>
    public interface IPreferencesProvider
    {
        /// <summary>
        /// Gets a value from preferences.
        /// </summary>
        /// <param name="key">The key to retrieve.</param>
        /// <param name="defaultValue">The default value if the key doesn't exist.</param>
        /// <returns>The stored value or the default value.</returns>
        string Get(string key, string defaultValue);

        /// <summary>
        /// Sets a value in preferences.
        /// </summary>
        /// <param name="key">The key to store.</param>
        /// <param name="value">The value to store.</param>
        void Set(string key, string value);
    }
}