using Hesoyam.Abstractions;
using Hesoyam.Configuration;
using Hesoyam.Enums;
using Hesoyam.Services;
using Microsoft.Extensions.Logging;

namespace Hesoyam.Shared
{
    /// <summary>
    /// Default/fallback implementation of the background service for unsupported platforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation is used when the current platform does not support native
    /// background task scheduling. It uses in-process timers to simulate background
    /// execution, which only works while the application is running.
    /// </para>
    /// <para>
    /// <strong>Limitations:</strong>
    /// <list type="bullet">
    ///     <item><description>Tasks only execute while the application is in the foreground</description></item>
    ///     <item><description>No persistence across app restarts</description></item>
    ///     <item><description>No system-level constraint support</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="DefaultPlatformBackgroundService"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    internal sealed class DefaultPlatformBackgroundService(ILogger<DefaultPlatformBackgroundService> logger, IServiceProvider serviceProvider) : PlatformBackgroundServiceBase(logger)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly Dictionary<string, Timer> _timers = [];
        private readonly Lock _timerLock = new();

        /// <inheritdoc />
        public override bool IsSupported => false;

        /// <inheritdoc />
        public override Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            Logger.LogWarning(
                "Current platform does not support native background task scheduling. " +
                "Tasks will only execute while the application is running.");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task<bool> RegisterPlatformTaskAsync(TaskRegistration registration, CancellationToken cancellationToken)
        {
            lock (_timerLock)
            {
                // Cancel existing timer if any
                if (_timers.TryGetValue(registration.Identifier, out Timer? existingTimer))
                {
                    existingTimer.Dispose();
                    _timers.Remove(registration.Identifier);
                }

                TaskConfiguration config = registration.Configuration;

                // Calculate timing
                TimeSpan dueTime = config.InitialDelay > TimeSpan.Zero ? config.InitialDelay : TimeSpan.Zero;
                TimeSpan period = config.Type == TaskType.Periodic ? config.Interval : Timeout.InfiniteTimeSpan;

                // Create timer
                Timer timer = new(
                    async _ => await ExecuteTaskAsync(registration).ConfigureAwait(false),
                    null,
                    dueTime,
                    period);

                _timers[registration.Identifier] = timer;

                Logger.LogInformation(
                    "Registered in-process timer for task {Identifier} (due: {DueTime}, period: {Period})",
                    registration.Identifier,
                    dueTime,
                    period);

                return Task.FromResult(true);
            }
        }

        /// <inheritdoc />
        protected override Task<bool> UnregisterPlatformTaskAsync(string identifier, CancellationToken cancellationToken)
        {
            lock (_timerLock)
            {
                if (_timers.TryGetValue(identifier, out Timer? timer))
                {
                    timer.Dispose();
                    _timers.Remove(identifier);

                    Logger.LogInformation("Unregistered in-process timer for task {Identifier}", identifier);
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Executes the registered task.
        /// </summary>
        /// <param name="registration">The task registration.</param>
        private async Task ExecuteTaskAsync(TaskRegistration registration)
        {
            try
            {
                Logger.LogDebug("Executing task {Identifier} via in-process timer", registration.Identifier);

                using CancellationTokenSource cts = new(registration.Configuration.ExecutionTimeout);
                TaskExecutor executor = _serviceProvider.GetRequiredService<Services.TaskExecutor>();
                await executor.ExecuteAsync(registration, cts.Token).ConfigureAwait(false);

                // For one-shot tasks, dispose the timer after execution
                if (registration.Configuration.Type != TaskType.Periodic)
                {
                    await UnregisterPlatformTaskAsync(registration.Identifier, CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "In-process task execution failed for {Identifier}", registration.Identifier);
            }
        }
    }
}