using Hesoyam.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Hesoyam.Services
{
    /// <summary>
    /// Default in-memory implementation of the task storage service.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation stores task registrations and state in memory with optional
    /// persistence to the platform's secure storage. It is suitable for most use cases
    /// and provides efficient access to task data.
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong> All operations are thread-safe.
    /// </para>
    /// <para>
    /// <strong>Persistence:</strong> Data is persisted to platform preferences storage
    /// when the application is suspended or explicitly requested.
    /// </para>
    /// </remarks>
    internal sealed class DefaultTaskStorageService : ITaskStorageService
    {
        private readonly ConcurrentDictionary<string, TaskRegistration> _registrations = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _states = new();
        private readonly ILogger<DefaultTaskStorageService> _logger;
        private readonly SemaphoreSlim _persistLock = new(1, 1);
        private readonly IPreferencesProvider _preferencesProvider;

        private const string RegistrationsKey = "hesoyam_task_registrations";
        private const string StatesKey = "hesoyam_task_states";

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTaskStorageService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="preferencesProvider">The preferences provider for persistent storage.</param>
        public DefaultTaskStorageService(ILogger<DefaultTaskStorageService> logger, IPreferencesProvider? preferencesProvider = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _preferencesProvider = preferencesProvider ?? new InMemoryPreferencesProvider();

            // Load persisted data asynchronously but handle exceptions
            Task.Run(async () =>
            {
                try
                {
                    await LoadFromStorageAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load persisted task data during initialization");
                }
            });
        }

        /// <inheritdoc />
        public Task SaveRegistrationAsync(TaskRegistration registration, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(registration);

            _registrations[registration.Identifier] = registration;
            _logger.LogDebug("Saved registration for task {Identifier}", registration.Identifier);

            // Fire and forget persistence
            _ = PersistToStorageAsync();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<TaskRegistration?> GetRegistrationAsync(string identifier, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

            _registrations.TryGetValue(identifier, out TaskRegistration? registration);
            return Task.FromResult(registration);
        }

        /// <inheritdoc />
        public Task<IEnumerable<TaskRegistration>> GetAllRegistrationsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<TaskRegistration>>([.. _registrations.Values]);
        }

        /// <inheritdoc />
        public Task<bool> RemoveRegistrationAsync(string identifier, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

            bool result = _registrations.TryRemove(identifier, out _);

            if (result)
            {
                _logger.LogDebug("Removed registration for task {Identifier}", identifier);
                _ = PersistToStorageAsync();
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc />
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public Task SaveStateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string taskIdentifier, string key, T value, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(taskIdentifier);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            ConcurrentDictionary<string, string> taskStates = _states.GetOrAdd(taskIdentifier, _ => new ConcurrentDictionary<string, string>());
            string serializedValue = JsonSerializer.Serialize(value, HesoyamJsonContext.Default.Options);
            taskStates[key] = serializedValue;

            _logger.LogDebug("Saved state key {Key} for task {Identifier}", key, taskIdentifier);

            // Fire and forget persistence
            _ = PersistToStorageAsync();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public Task<T?> GetStateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string taskIdentifier, string key, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(taskIdentifier);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            if (_states.TryGetValue(taskIdentifier, out ConcurrentDictionary<string, string>? taskStates) &&
                taskStates.TryGetValue(key, out string? serializedValue))
            {
                try
                {
                    return Task.FromResult(JsonSerializer.Deserialize<T>(serializedValue, HesoyamJsonContext.Default.Options));
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize state key {Key} for task {Identifier}", key, taskIdentifier);
                }
            }

            return Task.FromResult<T?>(default);
        }

        /// <inheritdoc />
        public Task ClearStateAsync(string taskIdentifier, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(taskIdentifier);

            _states.TryRemove(taskIdentifier, out _);
            _logger.LogDebug("Cleared all state for task {Identifier}", taskIdentifier);

            // Fire and forget persistence
            _ = PersistToStorageAsync();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Persists the current state to platform storage.
        /// </summary>
        private async Task PersistToStorageAsync()
        {
            if (!await _persistLock.WaitAsync(0).ConfigureAwait(false))
            {
                // Another persistence operation is in progress
                return;
            }

            try
            {
                // Serialize registrations
                Dictionary<string, StoredRegistration> registrationData = _registrations.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new StoredRegistration
                    {
                        TaskTypeName = kvp.Value.TaskTypeName,
                        Configuration = kvp.Value.Configuration,
                        CreatedAt = kvp.Value.CreatedAt
                    });

                string registrationsJson = JsonSerializer.Serialize(registrationData, HesoyamJsonContext.Default.DictionaryStringStoredRegistration);
                _preferencesProvider.Set(RegistrationsKey, registrationsJson);

                // Serialize states
                Dictionary<string, Dictionary<string, string>> statesData = _states.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToDictionary(s => s.Key, s => s.Value));

                string statesJson = JsonSerializer.Serialize(statesData, HesoyamJsonContext.Default.DictionaryStringDictionaryStringString);
                _preferencesProvider.Set(StatesKey, statesJson);

                _logger.LogDebug("Persisted {RegistrationCount} registrations and {StateCount} task states",
                    registrationData.Count,
                    statesData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist task data to storage");
            }
            finally
            {
                _persistLock.Release();
            }
        }

        /// <summary>
        /// Loads persisted data from platform storage.
        /// </summary>
        [UnconditionalSuppressMessage("Trimming", "IL2057:Unrecognized value passed to the parameter 'typeName' of method 'System.Type.GetType(String)'.",
            Justification = "Type names are stored by the application itself and are known at compile time.")]
        private async Task LoadFromStorageAsync()
        {
            await _persistLock.WaitAsync().ConfigureAwait(false);

            try
            {
                // Load registrations
                string registrationsJson = _preferencesProvider.Get(RegistrationsKey, string.Empty);
                if (!string.IsNullOrEmpty(registrationsJson))
                {
                    Dictionary<string, StoredRegistration>? storedRegistrations = JsonSerializer.Deserialize(registrationsJson, HesoyamJsonContext.Default.DictionaryStringStoredRegistration);
                    if (storedRegistrations != null)
                    {
                        foreach (KeyValuePair<string, StoredRegistration> kvp in storedRegistrations)
                        {
                            Type? taskType = Type.GetType(kvp.Value.TaskTypeName);
                            if (taskType != null)
                            {
                                _registrations[kvp.Key] = new TaskRegistration
                                {
                                    TaskType = taskType,
                                    Configuration = kvp.Value.Configuration,
                                    CreatedAt = kvp.Value.CreatedAt
                                };
                            }
                            else
                            {
                                _logger.LogWarning("Could not resolve task type {TypeName} during load", kvp.Value.TaskTypeName);
                            }
                        }
                    }
                }

                // Load states
                string statesJson = _preferencesProvider.Get(StatesKey, string.Empty);
                if (!string.IsNullOrEmpty(statesJson))
                {
                    Dictionary<string, Dictionary<string, string>>? storedStates = JsonSerializer.Deserialize(statesJson, HesoyamJsonContext.Default.DictionaryStringDictionaryStringString);
                    if (storedStates != null)
                    {
                        foreach (KeyValuePair<string, Dictionary<string, string>> kvp in storedStates)
                        {
                            ConcurrentDictionary<string, string> taskStates = new(kvp.Value);
                            _states[kvp.Key] = taskStates;
                        }
                    }
                }

                _logger.LogDebug("Loaded {RegistrationCount} registrations and {StateCount} task states from storage",
                    _registrations.Count,
                    _states.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load task data from storage");
            }
            finally
            {
                _persistLock.Release();
            }
        }

    }

    /// <summary>
    /// In-memory preferences provider for non-MAUI environments.
    /// </summary>
    internal sealed class InMemoryPreferencesProvider : IPreferencesProvider
    {
        private readonly ConcurrentDictionary<string, string> _storage = new();

        /// <inheritdoc />
        public string Get(string key, string defaultValue)
        {
            return _storage.TryGetValue(key, out string? value) ? value : defaultValue;
        }

        /// <inheritdoc />
        public void Set(string key, string value)
        {
            _storage[key] = value;
        }
    }
}