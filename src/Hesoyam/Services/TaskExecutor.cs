using Hesoyam.Abstractions;
using Hesoyam.Configuration;
using Hesoyam.Enums;
using Microsoft.Extensions.Logging;

namespace Hesoyam.Services
{
    /// <summary>
    /// Executes background tasks and manages their lifecycle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The task executor is responsible for:
    /// <list type="bullet">
    ///     <item><description>Resolving task instances from the DI container</description></item>
    ///     <item><description>Creating the execution context</description></item>
    ///     <item><description>Handling retries and backoff</description></item>
    ///     <item><description>Managing execution timeouts</description></item>
    ///     <item><description>Updating task status</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="TaskExecutor"/> class.
    /// </remarks>
    /// <param name="serviceProvider">The service provider for resolving task instances.</param>
    /// <param name="storageService">The storage service for state and status management.</param>
    /// <param name="logger">The logger instance.</param>
    internal sealed class TaskExecutor(
        IServiceProvider serviceProvider,
        ITaskStorageService storageService,
        ILogger<TaskExecutor> logger)
    {
        private readonly ITaskStorageService _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly ILogger<TaskExecutor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// Executes a registered task.
        /// </summary>
        /// <param name="registration">The task registration containing type and configuration.</param>
        /// <param name="cancellationToken">A token to cancel the execution.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns <see langword="true"/> if the task completed successfully; otherwise, <see langword="false"/>.
        /// </returns>
        public async Task<bool> ExecuteAsync(TaskRegistration registration, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(registration);

            TaskConfiguration config = registration.Configuration;
            string taskId = config.Identifier;

            _logger.LogInformation("Starting execution of task {TaskId}", taskId);

            await UpdateStatusAsync(taskId, TaskExecutionStatus.Running).ConfigureAwait(false);

            int attemptNumber = 0;
            Exception? lastException = default;

            while (attemptNumber <= config.MaxRetries)
            {
                attemptNumber++;
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _logger.LogDebug("Task {TaskId} attempt {AttemptNumber}", taskId, attemptNumber);

                    // Create a timeout cancellation token
                    using CancellationTokenSource timeoutCts = new(config.ExecutionTimeout);
                    using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                    // Execute the task
                    bool success = await ExecuteAttemptAsync(registration, attemptNumber, linkedCts.Token).ConfigureAwait(false);

                    if (success)
                    {
                        await UpdateStatusAsync(taskId, TaskExecutionStatus.Completed).ConfigureAwait(false);
                        _logger.LogInformation("Task {TaskId} completed successfully", taskId);
                        return true;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    await UpdateStatusAsync(taskId, TaskExecutionStatus.Cancelled).ConfigureAwait(false);
                    _logger.LogInformation("Task {TaskId} was cancelled", taskId);
                    throw;
                }
                catch (OperationCanceledException)
                {
                    lastException = new TimeoutException($"Task {taskId} exceeded the execution timeout of {config.ExecutionTimeout}");
                    _logger.LogWarning("Task {TaskId} timed out on attempt {AttemptNumber}", taskId, attemptNumber);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogError(ex, "Task {TaskId} failed on attempt {AttemptNumber}", taskId, attemptNumber);
                }

                // Calculate backoff delay for retry
                if (attemptNumber <= config.MaxRetries)
                {
                    TimeSpan delay = CalculateBackoffDelay(config, attemptNumber);
                    _logger.LogDebug("Task {TaskId} will retry after {Delay}", taskId, delay);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            // All attempts failed
            await UpdateStatusAsync(taskId, TaskExecutionStatus.Failed).ConfigureAwait(false);

            _logger.LogError(
                lastException,
                "Task {TaskId} failed after {AttemptCount} attempts",
                taskId,
                attemptNumber);

            return false;
        }

        /// <summary>
        /// Executes a single attempt of the task.
        /// </summary>
        private async Task<bool> ExecuteAttemptAsync(TaskRegistration registration, int attemptNumber, CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();

            // Resolve the task from the service provider
            if (scope.ServiceProvider.GetService(registration.TaskType) is not IBackgroundTask task)
            {
                _logger.LogError("Could not resolve task type {TaskType} from service provider", registration.TaskType.FullName);
                return false;
            }

            // Create the execution context
            ILoggerFactory loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            DefaultTaskContext context = new(
                registration.Identifier,
                registration.Configuration,
                attemptNumber,
                _storageService,
                loggerFactory.CreateLogger($"Hesoyam.Task.{registration.Identifier}"));

            // Execute the task
            await task.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Calculates the backoff delay for a retry attempt.
        /// </summary>
        private static TimeSpan CalculateBackoffDelay(TaskConfiguration config, int attemptNumber)
        {
            double multiplier = Math.Pow(config.RetryBackoffMultiplier, attemptNumber - 1);
            double delayMs = config.InitialRetryDelay.TotalMilliseconds * multiplier;

            // Cap at 1 hour
            double maxDelayMs = TimeSpan.FromHours(1).TotalMilliseconds;
            return TimeSpan.FromMilliseconds(Math.Min(delayMs, maxDelayMs));
        }

        /// <summary>
        /// Updates the execution status in storage.
        /// </summary>
        private Task UpdateStatusAsync(string taskId, TaskExecutionStatus status)
        {
            return _storageService.SaveStateAsync(taskId, "execution_status", status);
        }
    }
}