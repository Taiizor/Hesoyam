using Hesoyam.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Hesoyam.Shared
{
    /// <summary>
    /// Base class for platform-specific background service implementations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides common functionality shared across all platform implementations,
    /// including task registration tracking and logging infrastructure.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="PlatformBackgroundServiceBase"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    internal abstract class PlatformBackgroundServiceBase(ILogger logger) : IPlatformBackgroundService
    {
        /// <summary>
        /// The logger instance for platform operations.
        /// </summary>
        protected readonly ILogger Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// Thread-safe collection of registered task identifiers.
        /// </summary>
        protected readonly ConcurrentDictionary<string, TaskRegistration> RegisteredTasks = new();

        /// <inheritdoc />
        public abstract bool IsSupported { get; }

        /// <inheritdoc />
        public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            Logger.LogDebug("Initializing platform background service");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual async Task<bool> RegisterTaskAsync(TaskRegistration registration, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(registration);

            Logger.LogDebug("Registering task {Identifier} with platform service", registration.Identifier);

            // Store in our tracking dictionary
            RegisteredTasks[registration.Identifier] = registration;

            // Perform platform-specific registration
            return await RegisterPlatformTaskAsync(registration, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public virtual async Task<bool> UnregisterTaskAsync(string identifier, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

            Logger.LogDebug("Unregistering task {Identifier} from platform service", identifier);

            // Remove from tracking
            RegisteredTasks.TryRemove(identifier, out _);

            // Perform platform-specific unregistration
            return await UnregisterPlatformTaskAsync(identifier, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public virtual Task<IEnumerable<string>> GetRegisteredTasksAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<string>>([.. RegisteredTasks.Keys]);
        }

        /// <inheritdoc />
        public virtual Task<bool> RequestPermissionsAsync(CancellationToken cancellationToken = default)
        {
            Logger.LogDebug("Platform permissions not required or not implemented");
            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public virtual Task<bool> HasPermissionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Performs platform-specific task registration.
        /// </summary>
        /// <param name="registration">The task registration.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> if registration succeeded; otherwise, <see langword="false"/>.</returns>
        protected abstract Task<bool> RegisterPlatformTaskAsync(TaskRegistration registration, CancellationToken cancellationToken);

        /// <summary>
        /// Performs platform-specific task unregistration.
        /// </summary>
        /// <param name="identifier">The task identifier.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> if unregistration succeeded; otherwise, <see langword="false"/>.</returns>
        protected abstract Task<bool> UnregisterPlatformTaskAsync(string identifier, CancellationToken cancellationToken);
    }
}