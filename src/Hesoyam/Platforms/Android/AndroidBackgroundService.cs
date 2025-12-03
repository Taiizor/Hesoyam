using Android.Content;
using AndroidX.Work;
using Hesoyam.Abstractions;
using Hesoyam.Configuration;
using Hesoyam.Enums;
using Hesoyam.Services;
using Hesoyam.Shared;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using AndroidNetworkType = AndroidX.Work.NetworkType;
using NetworkType = Hesoyam.Enums.NetworkType;

namespace Hesoyam.Platforms.Android
{
    /// <summary>
    /// Android-specific implementation of the background service using WorkManager.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation leverages Android's WorkManager API, which provides:
    /// <list type="bullet">
    ///     <item><description>Guaranteed task execution even after app restart or device reboot</description></item>
    ///     <item><description>Constraint-based scheduling (network, charging, battery, idle)</description></item>
    ///     <item><description>Automatic retry with configurable backoff</description></item>
    ///     <item><description>Battery-efficient execution</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Requirements:</strong>
    /// <list type="bullet">
    ///     <item><description>Minimum Android API level 24 (Android 7.0)</description></item>
    ///     <item><description>AndroidX.Work.Runtime NuGet package</description></item>
    ///     <item><description>RECEIVE_BOOT_COMPLETED permission for reboot persistence</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="AndroidBackgroundService"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    internal sealed class AndroidBackgroundService(ILogger<AndroidBackgroundService> logger, IServiceProvider serviceProvider) : PlatformBackgroundServiceBase(logger)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private static readonly ConcurrentDictionary<string, TaskRegistration> _pendingExecutions = new();

        /// <inheritdoc />
        public override bool IsSupported => true;

        /// <inheritdoc />
        public override Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("Initializing Android WorkManager background service");

            // WorkManager is automatically initialized by AndroidX
            WorkManager workManager = WorkManager.GetInstance(Platform.AppContext);
            Logger.LogDebug("WorkManager instance obtained successfully");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task<bool> RegisterPlatformTaskAsync(TaskRegistration registration, CancellationToken cancellationToken)
        {
            try
            {
                TaskConfiguration config = registration.Configuration;
                WorkManager workManager = WorkManager.GetInstance(Platform.AppContext);

                // Store registration for worker access
                _pendingExecutions[registration.Identifier] = registration;

                // Build constraints
                Constraints.Builder constraintsBuilder = new();

                // Network constraint
                AndroidNetworkType androidNetworkType = config.NetworkRequirement switch
                {
                    NetworkType.None => AndroidNetworkType.NotRequired!,
                    NetworkType.Connected => AndroidNetworkType.Connected!,
                    NetworkType.Unmetered => AndroidNetworkType.Unmetered!,
                    NetworkType.WiFi => AndroidNetworkType.Unmetered!, // WiFi maps to unmetered
                    _ => AndroidNetworkType.NotRequired!
                };
                constraintsBuilder.SetRequiredNetworkType(androidNetworkType);

                // Other constraints
                constraintsBuilder.SetRequiresCharging(config.RequiresCharging);
                constraintsBuilder.SetRequiresDeviceIdle(config.RequiresDeviceIdle);
                constraintsBuilder.SetRequiresBatteryNotLow(config.RequiresBatteryNotLow);

                Constraints constraints = constraintsBuilder.Build();

                // Create input data with task identifier
                Data inputData = new Data.Builder()
                    .PutString("task_identifier", registration.Identifier)
                    .Build();

                if (config.Type == TaskType.Periodic)
                {
                    // Periodic work - minimum interval is 15 minutes
                    long intervalMinutes = Math.Max(15, (long)config.Interval.TotalMinutes);

                    PeriodicWorkRequest.Builder periodicWorkRequestBuilder = new PeriodicWorkRequest.Builder(
                        typeof(HesoyamWorker),
                        intervalMinutes,
                        Java.Util.Concurrent.TimeUnit.Minutes!)
                        .SetConstraints(constraints)
                        .SetInputData(inputData)
                        .AddTag(registration.Identifier)
                        .AddTag("hesoyam");

                    // Add tags
                    foreach (string tag in config.Tags)
                    {
                        periodicWorkRequestBuilder.AddTag(tag);
                    }

                    if (config.InitialDelay > TimeSpan.Zero)
                    {
                        periodicWorkRequestBuilder.SetInitialDelay((long)config.InitialDelay.TotalMilliseconds, Java.Util.Concurrent.TimeUnit.Milliseconds!);
                    }

                    PeriodicWorkRequest periodicWorkRequest = periodicWorkRequestBuilder.Build();

                    // Enqueue periodic work - use Update policy (replaces deprecated Replace)
                    workManager.EnqueueUniquePeriodicWork(
                        registration.Identifier,
                        ExistingPeriodicWorkPolicy.Update!,
                        periodicWorkRequest);

                    Logger.LogInformation("Successfully registered Android periodic work request for task {Identifier}", registration.Identifier);
                    return Task.FromResult(true);
                }
                else
                {
                    // One-time work
                    OneTimeWorkRequest.Builder oneTimeWorkRequest = new OneTimeWorkRequest.Builder(typeof(HesoyamWorker))
                        .SetConstraints(constraints)
                        .SetInputData(inputData)
                        .AddTag(registration.Identifier)
                        .AddTag("hesoyam");

                    // Add tags
                    foreach (string tag in config.Tags)
                    {
                        oneTimeWorkRequest.AddTag(tag);
                    }

                    if (config.InitialDelay > TimeSpan.Zero)
                    {
                        oneTimeWorkRequest.SetInitialDelay((long)config.InitialDelay.TotalMilliseconds, Java.Util.Concurrent.TimeUnit.Milliseconds!);
                    }

                    // Configure backoff for retries
                    oneTimeWorkRequest.SetBackoffCriteria(
                        BackoffPolicy.Exponential!,
                        (long)config.InitialRetryDelay.TotalMilliseconds,
                        Java.Util.Concurrent.TimeUnit.Milliseconds!);

                    OneTimeWorkRequest builtRequest = oneTimeWorkRequest.Build();

                    // Enqueue one-time work
                    workManager.EnqueueUniqueWork(
                        registration.Identifier,
                        ExistingWorkPolicy.Replace!,
                        builtRequest);

                    Logger.LogInformation("Successfully registered Android one-time work request for task {Identifier}", registration.Identifier);
                    return Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to register Android work request for task {Identifier}", registration.Identifier);
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc />
        protected override Task<bool> UnregisterPlatformTaskAsync(string identifier, CancellationToken cancellationToken)
        {
            try
            {
                WorkManager workManager = WorkManager.GetInstance(Platform.AppContext);
                workManager.CancelUniqueWork(identifier);

                _pendingExecutions.TryRemove(identifier, out _);

                Logger.LogInformation("Successfully unregistered Android work request for task {Identifier}", identifier);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to unregister Android work request for task {Identifier}", identifier);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Gets a pending task registration by identifier.
        /// </summary>
        /// <param name="identifier">The task identifier.</param>
        /// <returns>The task registration if found; otherwise, null.</returns>
        internal static TaskRegistration? GetPendingRegistration(string identifier)
        {
            _pendingExecutions.TryGetValue(identifier, out TaskRegistration? registration);
            return registration;
        }
    }

    /// <summary>
    /// Android Worker implementation that executes Hesoyam background tasks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This worker is invoked by WorkManager and delegates execution to the registered
    /// <see cref="IBackgroundTask"/> implementation through the service provider.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="HesoyamWorker"/> class.
    /// </remarks>
    /// <param name="context">The application context.</param>
    /// <param name="workerParams">The worker parameters.</param>
    public class HesoyamWorker(Context context, WorkerParameters workerParams) : Worker(context, workerParams)
    {
        /// <summary>
        /// Performs the background work.
        /// </summary>
        /// <returns>The result of the work execution.</returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
            Justification = "Task types are registered by the application and preserved through DI registration.")]
        public override Result DoWork()
        {
            string? identifier = InputData?.GetString("task_identifier");
            if (string.IsNullOrEmpty(identifier))
            {
                return Result.InvokeFailure() ?? Result.InvokeRetry()!;
            }

            try
            {
                // Get the registration
                TaskRegistration? registration = AndroidBackgroundService.GetPendingRegistration(identifier);
                if (registration == null)
                {
                    return Result.InvokeFailure() ?? Result.InvokeRetry()!;
                }

                // Get service provider from the MAUI app
                IServiceProvider? serviceProvider = IPlatformApplication.Current?.Services;
                if (serviceProvider == null)
                {
                    return Result.InvokeRetry()!;
                }

                // Execute the task
                TaskExecutor executor = serviceProvider.GetRequiredService<Services.TaskExecutor>();
                bool result = executor.ExecuteAsync(registration, CancellationToken.None).GetAwaiter().GetResult();

                return result ? Result.InvokeSuccess()! : Result.InvokeRetry()!;
            }
            catch (Exception)
            {
                return Result.InvokeRetry()!;
            }
        }
    }
}