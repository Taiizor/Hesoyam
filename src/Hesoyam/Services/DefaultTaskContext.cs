using Hesoyam.Abstractions;
using Hesoyam.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Hesoyam.Services
{
    /// <summary>
    /// Provides the execution context for background tasks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements <see cref="ITaskContext"/> and provides access to task metadata,
    /// configuration, progress reporting, and state management during task execution.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="DefaultTaskContext"/> class.
    /// </remarks>
    /// <param name="taskId">The unique task identifier.</param>
    /// <param name="configuration">The task configuration.</param>
    /// <param name="attemptNumber">The current attempt number.</param>
    /// <param name="storageService">The storage service for state persistence.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="progressCallback">Optional callback for progress updates.</param>
    /// <param name="messageCallback">Optional callback for progress messages.</param>
    internal sealed class DefaultTaskContext(
        string taskId,
        TaskConfiguration configuration,
        int attemptNumber,
        ITaskStorageService storageService,
        ILogger logger,
        Action<int, int>? progressCallback = null,
        Action<string>? messageCallback = null) : ITaskContext
    {
        private readonly ITaskStorageService _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <inheritdoc />
        public Guid ExecutionId { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public int AttemptNumber { get; } = attemptNumber;

        /// <inheritdoc />
        public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public string TaskId { get; } = taskId ?? throw new ArgumentNullException(nameof(taskId));

        /// <inheritdoc />
        public TaskConfiguration Configuration { get; } = configuration ?? throw new ArgumentNullException(nameof(configuration));

        /// <inheritdoc />
        public Task ReportProgressAsync(int current, int total)
        {
            double percentage = total > 0 ? (current * 100.0 / total) : 0;
            _logger.LogDebug("Task {TaskId} progress: {Current}/{Total} ({Percentage:F1}%)", TaskId, current, total, percentage);
            progressCallback?.Invoke(current, total);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ReportProgressAsync(string message)
        {
            _logger.LogDebug("Task {TaskId} progress: {Message}", TaskId, message);
            messageCallback?.Invoke(message);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public T? GetState<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            // Use Task.Run to avoid potential deadlocks when called from synchronous context
            return Task.Run(async () => await _storageService.GetStateAsync<T>(TaskId, key).ConfigureAwait(false))
                .GetAwaiter()
                .GetResult();
        }

        /// <inheritdoc />
        public Task SetStateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string key, T value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            return _storageService.SaveStateAsync(TaskId, key, value);
        }

        /// <inheritdoc />
        public Task ClearStateAsync()
        {
            return _storageService.ClearStateAsync(TaskId);
        }
    }
}