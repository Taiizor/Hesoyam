using BackgroundTasks;
using Foundation;
using Hesoyam.Abstractions;
using Hesoyam.Configuration;
using Hesoyam.Enums;
using Hesoyam.Services;
using Hesoyam.Shared;
using Microsoft.Extensions.Logging;
using UIKit;

namespace Hesoyam.Platforms.iOS
{
    /// <summary>
    /// iOS-specific implementation of the background service using BGTaskScheduler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation leverages iOS's BGTaskScheduler API, which provides:
    /// <list type="bullet">
    ///     <item><description>Background App Refresh for short maintenance tasks</description></item>
    ///     <item><description>Background Processing for longer tasks during optimal conditions</description></item>
    ///     <item><description>System-managed scheduling based on device usage patterns</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Requirements:</strong>
    /// <list type="bullet">
    ///     <item><description>Minimum iOS 13.0</description></item>
    ///     <item><description>BGTaskSchedulerPermittedIdentifiers in Info.plist</description></item>
    ///     <item><description>Background Modes capability (Background fetch, Background processing)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Limitations:</strong>
    /// <list type="bullet">
    ///     <item><description>Background execution time is limited (approx. 30 seconds for refresh tasks)</description></item>
    ///     <item><description>Task scheduling is at the discretion of the system</description></item>
    ///     <item><description>Tasks may not run at exact scheduled times</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="iOSBackgroundService"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    internal sealed class iOSBackgroundService(ILogger<iOSBackgroundService> logger, IServiceProvider serviceProvider) : PlatformBackgroundServiceBase(logger)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private bool _isInitialized;

        /// <summary>
        /// Gets the bundle identifier prefix for task identifiers.
        /// </summary>
        private static string BundleIdentifier => NSBundle.MainBundle.BundleIdentifier ?? "com.hesoyam";

        /// <inheritdoc />
        public override bool IsSupported => UIDevice.CurrentDevice.CheckSystemVersion(13, 0);

        /// <inheritdoc />
        public override Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
            {
                return Task.CompletedTask;
            }

            Logger.LogInformation("Initializing iOS BGTaskScheduler background service");

            // Note: BGTaskScheduler.Shared.Register must be called before app finishes launching
            // This is typically done in AppDelegate.FinishedLaunching

            _isInitialized = true;
            Logger.LogDebug("iOS background service initialized");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task<bool> RegisterPlatformTaskAsync(TaskRegistration registration, CancellationToken cancellationToken)
        {
            try
            {
                TaskConfiguration config = registration.Configuration;
                string taskIdentifier = GetFullTaskIdentifier(registration.Identifier);

                // Register the task handler
                bool registered = BGTaskScheduler.Shared.Register(
                    taskIdentifier,
                    null,
                    task => HandleBackgroundTask(task, registration));

                if (!registered)
                {
                    Logger.LogWarning(
                        "Failed to register iOS background task {Identifier}. " +
                        "Ensure the identifier is added to BGTaskSchedulerPermittedIdentifiers in Info.plist",
                        taskIdentifier);
                    return Task.FromResult(false);
                }

                // Schedule the task
                ScheduleBackgroundTask(registration);

                Logger.LogInformation("Successfully registered iOS background task {Identifier}", taskIdentifier);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to register iOS background task for {Identifier}", registration.Identifier);
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

                Logger.LogInformation("Successfully unregistered iOS background task {Identifier}", taskIdentifier);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to unregister iOS background task for {Identifier}", identifier);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Schedules a background task with the iOS system.
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

                // Calculate earliest begin date
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
                    // Configure constraints
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
                    "Failed to submit iOS background task {Identifier}: {Error}",
                    taskIdentifier,
                    error.LocalizedDescription);
            }
            else
            {
                Logger.LogDebug("Submitted iOS background task {Identifier}", taskIdentifier);
            }
        }

        /// <summary>
        /// Handles the execution of a background task.
        /// </summary>
        /// <param name="task">The iOS background task.</param>
        /// <param name="registration">The task registration.</param>
        private void HandleBackgroundTask(BGTask task, TaskRegistration registration)
        {
            TaskConfiguration config = registration.Configuration;
            using CancellationTokenSource cts = new();

            // Set up expiration handler
            task.ExpirationHandler = () =>
            {
                Logger.LogWarning("iOS background task {Identifier} expired", registration.Identifier);
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
                    Logger.LogError(ex, "iOS background task {Identifier} failed", registration.Identifier);
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
            return $"{BundleIdentifier}.{identifier}";
        }
    }
}