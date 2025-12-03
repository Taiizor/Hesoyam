using Hesoyam.Abstractions;
using Hesoyam.Configuration;
using Hesoyam.Enums;
using Microsoft.Extensions.Logging;

namespace Hesoyam.Services
{
    /// <summary>
    /// Default implementation of the task scheduler that coordinates between storage and platform services.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This scheduler provides a unified API for scheduling and managing background tasks across
    /// all supported platforms. It delegates platform-specific operations to <see cref="IPlatformBackgroundService"/>
    /// and handles task storage through <see cref="ITaskStorageService"/>.
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong> All public methods are thread-safe.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="DefaultTaskScheduler"/> class.
    /// </remarks>
    /// <param name="platformService">The platform-specific background service.</param>
    /// <param name="storageService">The task storage service.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    internal sealed class DefaultTaskScheduler(
        IPlatformBackgroundService platformService,
        ITaskStorageService storageService,
        ILogger<DefaultTaskScheduler> logger,
        IServiceProvider serviceProvider) : ITaskScheduler
    {
        private readonly IPlatformBackgroundService _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
        private readonly ITaskStorageService _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly ILogger<DefaultTaskScheduler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly SemaphoreSlim _lock = new(1, 1);

        /// <inheritdoc />
        public Task<bool> ScheduleAsync<TTask>(TaskConfiguration configuration, CancellationToken cancellationToken = default)
            where TTask : class, IBackgroundTask
        {
            return ScheduleAsync(typeof(TTask), configuration, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> ScheduleAsync(Type taskType, TaskConfiguration configuration, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(taskType);
            ArgumentNullException.ThrowIfNull(configuration);

            if (!typeof(IBackgroundTask).IsAssignableFrom(taskType))
            {
                throw new ArgumentException(
                    $"Type {taskType.FullName} must implement {nameof(IBackgroundTask)}.",
                    nameof(taskType));
            }

            if (string.IsNullOrWhiteSpace(configuration.Identifier))
            {
                throw new ArgumentException(
                    "Task identifier cannot be null, empty, or whitespace.",
                    nameof(configuration));
            }

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _logger.LogInformation(
                    "Scheduling task {TaskType} with identifier {Identifier}",
                    taskType.Name,
                    configuration.Identifier);

                // Create the registration
                TaskRegistration registration = new()
                {
                    TaskType = taskType,
                    Configuration = configuration.Clone()
                };

                // Save to storage for persistence
                await _storageService.SaveRegistrationAsync(registration, cancellationToken).ConfigureAwait(false);

                // Register with platform service
                bool result = await _platformService.RegisterTaskAsync(registration, cancellationToken).ConfigureAwait(false);

                if (result)
                {
                    _logger.LogInformation(
                        "Successfully scheduled task {Identifier} of type {TaskType}",
                        configuration.Identifier,
                        taskType.Name);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to schedule task {Identifier} with platform service",
                        configuration.Identifier);
                }

                return result;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc />
        public async Task<bool> CancelAsync(string identifier, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _logger.LogInformation("Cancelling task {Identifier}", identifier);

                // Unregister from platform
                bool result = await _platformService.UnregisterTaskAsync(identifier, cancellationToken).ConfigureAwait(false);

                // Remove from storage
                await _storageService.RemoveRegistrationAsync(identifier, cancellationToken).ConfigureAwait(false);

                // Clear any associated state
                await _storageService.ClearStateAsync(identifier, cancellationToken).ConfigureAwait(false);

                if (result)
                {
                    _logger.LogInformation("Successfully cancelled task {Identifier}", identifier);
                }
                else
                {
                    _logger.LogDebug("Task {Identifier} was not found in platform service", identifier);
                }

                return result;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc />
        public async Task<int> CancelByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tag);

            _logger.LogInformation("Cancelling all tasks with tag {Tag}", tag);

            IEnumerable<TaskRegistration> registrations = await _storageService.GetAllRegistrationsAsync(cancellationToken).ConfigureAwait(false);
            int count = 0;

            foreach (TaskRegistration registration in registrations)
            {
                if (registration.Configuration.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    bool result = await CancelAsync(registration.Identifier, cancellationToken).ConfigureAwait(false);
                    if (result)
                    {
                        count++;
                    }
                }
            }

            _logger.LogInformation("Cancelled {Count} tasks with tag {Tag}", count, tag);
            return count;
        }

        /// <inheritdoc />
        public async Task<int> CancelAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Cancelling all scheduled tasks");

            IEnumerable<TaskRegistration> registrations = await _storageService.GetAllRegistrationsAsync(cancellationToken).ConfigureAwait(false);
            int count = 0;

            foreach (TaskRegistration registration in registrations)
            {
                bool result = await CancelAsync(registration.Identifier, cancellationToken).ConfigureAwait(false);
                if (result)
                {
                    count++;
                }
            }

            _logger.LogInformation("Cancelled {Count} tasks", count);
            return count;
        }

        /// <inheritdoc />
        public async Task<TaskExecutionStatus?> GetStatusAsync(string identifier, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

            TaskRegistration? registration = await _storageService.GetRegistrationAsync(identifier, cancellationToken).ConfigureAwait(false);
            if (registration == null)
            {
                return null;
            }

            // Get status from storage
            TaskExecutionStatus? status = await _storageService.GetStateAsync<TaskExecutionStatus?>(
                identifier,
                "execution_status",
                cancellationToken).ConfigureAwait(false);

            return status ?? TaskExecutionStatus.Pending;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<string>> GetScheduledTasksAsync(CancellationToken cancellationToken = default)
        {
            IEnumerable<TaskRegistration> registrations = await _storageService.GetAllRegistrationsAsync(cancellationToken).ConfigureAwait(false);
            return registrations.Select(r => r.Identifier).ToList().AsReadOnly();
        }

        /// <inheritdoc />
        public async Task<bool> IsScheduledAsync(string identifier, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

            TaskRegistration? registration = await _storageService.GetRegistrationAsync(identifier, cancellationToken).ConfigureAwait(false);
            return registration != null;
        }

        /// <inheritdoc />
        public async Task<TaskConfiguration?> GetConfigurationAsync(string identifier, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

            TaskRegistration? registration = await _storageService.GetRegistrationAsync(identifier, cancellationToken).ConfigureAwait(false);
            return registration?.Configuration.Clone();
        }
    }
}