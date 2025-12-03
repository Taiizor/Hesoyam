using BackgroundTasks;
using Foundation;
using Hesoyam.Abstractions;
using Hesoyam.Configuration;
using Hesoyam.Enums;
using Hesoyam.Services;
using Hesoyam.Shared;
using Microsoft.Extensions.Logging;

namespace Hesoyam.Platforms.MacCatalyst
{
    /// <summary>
    /// macOS Catalyst-specific implementation of the background service using BGTaskScheduler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation leverages BGTaskScheduler API (shared with iOS), which provides:
    /// <list type="bullet">
    ///     <item><description>Background App Refresh for maintenance tasks</description></item>
    ///     <item><description>Background Processing for longer tasks</description></item>
    ///     <item><description>System-managed scheduling based on device usage patterns</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Requirements:</strong>
    /// <list type="bullet">
    ///     <item><description>Minimum macOS Catalyst 14.0</description></item>
    ///     <item><description>BGTaskSchedulerPermittedIdentifiers in Info.plist</description></item>
    ///     <item><description>Background Modes capability</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="MacCatalystBackgroundService"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    internal sealed class MacCatalystBackgroundService(ILogger<MacCatalystBackgroundService> logger, IServiceProvider serviceProvider) : PlatformBackgroundServiceBase(logger)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly Dictionary<string, TaskRegistration> _registrations = [];
        private bool _isInitialized;

        /// <summary>
        /// Gets the bundle identifier for task identifiers.
        /// </summary>
        private static string BundleIdentifier => NSBundle.MainBundle.BundleIdentifier ?? "com.hesoyam";

        /// <inheritdoc />
        public override bool IsSupported => true;

        /// <inheritdoc />
        public override Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
            {
                return Task.CompletedTask;
            }

            Logger.LogInformation("Initializing macOS Catalyst BGTaskScheduler background service");
            _isInitialized = true;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task<bool> RegisterPlatformTaskAsync(TaskRegistration registration, CancellationToken cancellationToken)
        {
            try
            {
                TaskConfiguration config = registration.Configuration;
                string taskIdentifier = GetFullTaskIdentifier(registration.Identifier);

                // Register the task handler with BGTaskScheduler
                bool registered = BGTaskScheduler.Shared.Register(
                    taskIdentifier,
                    null,
                    task => HandleBackgroundTask(task, registration));

                if (!registered)
                {
                    Logger.LogWarning(
                        "Failed to register macOS Catalyst background task {Identifier}. " +
                        "Ensure the identifier is added to BGTaskSchedulerPermittedIdentifiers in Info.plist",
                        taskIdentifier);
                    return Task.FromResult(false);
                }

                // Store registration
                _registrations[registration.Identifier] = registration;

                // Schedule the task
                ScheduleBackgroundTask(registration);

                Logger.LogInformation("Successfully registered macOS Catalyst background task {Identifier}", taskIdentifier);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to register macOS Catalyst background task for {Identifier}", registration.Identifier);
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc />
        protected override Task<bool> UnregisterPlatformTaskAsync(string identifier, CancellationToken cancellationToken)
        {
            try
            {
                string taskIdentifier = GetFullTaskIdentifier(identifier);
                BGTaskScheduler.Shared.Cancel(taskIdentifier);
                _registrations.Remove(identifier);

                Logger.LogInformation("Successfully unregistered macOS Catalyst background task {Identifier}", identifier);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to unregister macOS Catalyst background task for {Identifier}", identifier);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Schedules a background task with BGTaskScheduler.
        /// </summary>
        /// <param name="registration">The task registration.</param>
        private void ScheduleBackgroundTask(TaskRegistration registration)
        {
            TaskConfiguration config = registration.Configuration;
            string taskIdentifier = GetFullTaskIdentifier(registration.Identifier);

            BGTaskRequest request;

            if (config.Type == TaskType.Periodic)
            {
                // Use BGAppRefreshTaskRequest for periodic tasks
                BGAppRefreshTaskRequest refreshRequest = new(taskIdentifier);

                DateTimeOffset earliestDate = DateTimeOffset.UtcNow.Add(config.InitialDelay > TimeSpan.Zero
                    ? config.InitialDelay
                    : config.Interval);

                refreshRequest.EarliestBeginDate = (NSDate)earliestDate.DateTime;
                request = refreshRequest;
            }
            else
            {
                // Use BGProcessingTaskRequest for one-time or longer tasks
                BGProcessingTaskRequest processingRequest = new(taskIdentifier)
                {
                    RequiresNetworkConnectivity = config.NetworkRequirement != NetworkType.None,
                    RequiresExternalPower = config.RequiresCharging
                };

                if (config.InitialDelay > TimeSpan.Zero)
                {
                    processingRequest.EarliestBeginDate = (NSDate)DateTimeOffset.UtcNow.Add(config.InitialDelay).DateTime;
                }

                request = processingRequest;
            }

            BGTaskScheduler.Shared.Submit(request, out NSError? error);

            if (error != null)
            {
                Logger.LogWarning(
                    "Failed to submit macOS Catalyst background task {Identifier}: {Error}",
                    taskIdentifier,
                    error.LocalizedDescription);
            }
            else
            {
                Logger.LogDebug("Submitted macOS Catalyst background task {Identifier}", taskIdentifier);
            }
        }

        /// <summary>
        /// Handles the execution of a background task.
        /// </summary>
        /// <param name="task">The BGTask instance.</param>
        /// <param name="registration">The task registration.</param>
        private void HandleBackgroundTask(BGTask task, TaskRegistration registration)
        {
            TaskConfiguration config = registration.Configuration;
            using CancellationTokenSource cts = new();

            task.ExpirationHandler = () =>
            {
                Logger.LogWarning("macOS Catalyst background task {Identifier} expired", registration.Identifier);
                cts.Cancel();
            };

            Task.Run(async () =>
            {
                try
                {
                    TaskExecutor executor = _serviceProvider.GetRequiredService<TaskExecutor>();
                    bool success = await executor.ExecuteAsync(registration, cts.Token).ConfigureAwait(false);

                    task.SetTaskCompleted(success);

                    // Reschedule if periodic
                    if (config.Type == TaskType.Periodic && !cts.Token.IsCancellationRequested)
                    {
                        ScheduleBackgroundTask(registration);
                    }
                }
                catch (OperationCanceledException)
                {
                    task.SetTaskCompleted(false);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "macOS Catalyst background task {Identifier} failed", registration.Identifier);
                    task.SetTaskCompleted(false);
                }
            });
        }

        /// <summary>
        /// Gets the full task identifier with bundle identifier prefix.
        /// </summary>
        /// <param name="identifier">The short task identifier.</param>
        /// <returns>The full task identifier.</returns>
        private static string GetFullTaskIdentifier(string identifier)
        {
            return $"{BundleIdentifier}.background.{identifier}";
        }
    }
}