using Hesoyam.Abstractions;
using Hesoyam.Configuration;
using Hesoyam.Enums;
using Hesoyam.Services;
using Hesoyam.Shared;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Windows.ApplicationModel.Background;
using IBackgroundTask = Windows.ApplicationModel.Background.IBackgroundTask;

namespace Hesoyam.Platforms.Windows
{
    /// <summary>
    /// Windows-specific implementation of the background service using Windows Background Tasks API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation leverages Windows' Background Tasks API, which provides:
    /// <list type="bullet">
    ///     <item><description>Time-triggered background execution</description></item>
    ///     <item><description>System condition-based triggers (network, power, user presence)</description></item>
    ///     <item><description>Application trigger for app-initiated background work</description></item>
    ///     <item><description>Maintenance and system event triggers</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Requirements:</strong>
    /// <list type="bullet">
    ///     <item><description>Minimum Windows 10 version 1809 (10.0.17763.0)</description></item>
    ///     <item><description>Background Tasks capability in package manifest</description></item>
    ///     <item><description>Registration of background task entry point</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Limitations:</strong>
    /// <list type="bullet">
    ///     <item><description>Minimum time trigger interval is 15 minutes</description></item>
    ///     <item><description>CPU and network resource quotas apply to background tasks</description></item>
    ///     <item><description>Background tasks may be throttled by the system</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="WindowsBackgroundService"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    internal sealed class WindowsBackgroundService(ILogger<WindowsBackgroundService> logger, IServiceProvider serviceProvider) : PlatformBackgroundServiceBase(logger)
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly Dictionary<string, IBackgroundTaskRegistration> _registeredBackgroundTasks = [];
        private bool _isInitialized;

        /// <summary>
        /// The entry point for the background task host.
        /// </summary>
        private const string BackgroundTaskEntryPoint = "Hesoyam.Platforms.Windows.HesoyamBackgroundTask";

        /// <inheritdoc />
        public override bool IsSupported => true;

        /// <inheritdoc />
        public override async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
            {
                return;
            }

            Logger.LogInformation("Initializing Windows background service");

            try
            {
                // Request background execution permission
                BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();

                Logger.LogDebug("Background access status: {Status}", status);

                // Load existing registrations
                foreach (KeyValuePair<Guid, IBackgroundTaskRegistration> task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name.StartsWith("Hesoyam_", StringComparison.Ordinal))
                    {
                        _registeredBackgroundTasks[task.Value.Name] = task.Value;
                    }
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException)
            {
                // Windows Background Task APIs require package identity (MSIX)
                // For unpackaged apps, we log and continue - tasks will be stored but not executed in background
                Logger.LogWarning(ex, "Windows background task APIs not available - app may be running unpackaged");
            }

            _isInitialized = true;
        }

        /// <inheritdoc />
        public override async Task<bool> RequestPermissionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
                return status is not BackgroundAccessStatus.DeniedByUser and
                       not BackgroundAccessStatus.DeniedBySystemPolicy;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException)
            {
                // Windows Background Task APIs require package identity (MSIX)
                Logger.LogWarning(ex, "Background permissions not available - app may be running unpackaged");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to request background execution permission");
                return false;
            }
        }

        /// <inheritdoc />
        public override async Task<bool> HasPermissionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
                return status is not BackgroundAccessStatus.DeniedByUser and
                       not BackgroundAccessStatus.DeniedBySystemPolicy;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException)
            {
                // Windows Background Task APIs require package identity (MSIX)
                Logger.LogWarning(ex, "Background permissions not available - app may be running unpackaged");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to check background execution permission");
                return false;
            }
        }

        /// <inheritdoc />
        protected override Task<bool> RegisterPlatformTaskAsync(TaskRegistration registration, CancellationToken cancellationToken)
        {
            // Always store task data first (works in both packaged and unpackaged scenarios)
            StoreTaskData(registration);

            try
            {
                TaskConfiguration config = registration.Configuration;
                string taskName = GetTaskName(registration.Identifier);

                // Unregister existing task with the same name
                UnregisterExistingTask(taskName);

                // Create the task builder
                BackgroundTaskBuilder builder = new()
                {
                    Name = taskName,
                    TaskEntryPoint = BackgroundTaskEntryPoint
                };

                // Configure trigger based on task type
                if (config.Type == TaskType.Periodic)
                {
                    // Time trigger - minimum is 15 minutes
                    uint intervalMinutes = (uint)Math.Max(15, (int)config.Interval.TotalMinutes);
                    TimeTrigger timeTrigger = new(intervalMinutes, oneShot: false);
                    builder.SetTrigger(timeTrigger);
                }
                else if (config.Type == TaskType.Immediate)
                {
                    // Application trigger for immediate execution
                    ApplicationTrigger appTrigger = new();
                    builder.SetTrigger(appTrigger);

                    // Store the trigger for later invocation
                    StoreAppTrigger(registration.Identifier, appTrigger);
                }
                else
                {
                    // One-shot with delay using maintenance trigger
                    uint intervalMinutes = (uint)Math.Max(15, (int)config.InitialDelay.TotalMinutes);
                    if (intervalMinutes == 0)
                    {
                        intervalMinutes = 15;
                    }

                    TimeTrigger timeTrigger = new(intervalMinutes, oneShot: true);
                    builder.SetTrigger(timeTrigger);
                }

                // Add conditions based on configuration
                if (config.NetworkRequirement != NetworkType.None)
                {
                    SystemCondition networkCondition = config.NetworkRequirement switch
                    {
                        NetworkType.Unmetered => new SystemCondition(SystemConditionType.FreeNetworkAvailable),
                        NetworkType.WiFi => new SystemCondition(SystemConditionType.FreeNetworkAvailable),
                        _ => new SystemCondition(SystemConditionType.InternetAvailable)
                    };
                    builder.AddCondition(networkCondition);
                }

                if (config.RequiresCharging)
                {
                    builder.AddCondition(new SystemCondition(SystemConditionType.BackgroundWorkCostNotHigh));
                }

                if (config.RequiresDeviceIdle)
                {
                    builder.AddCondition(new SystemCondition(SystemConditionType.UserNotPresent));
                }

                // Register the task
                BackgroundTaskRegistration backgroundTask = builder.Register();
                _registeredBackgroundTasks[taskName] = backgroundTask;

                // Handle completion
                backgroundTask.Completed += (sender, args) =>
                {
                    Logger.LogDebug("Windows background task {TaskName} completed", taskName);
                };

                Logger.LogInformation("Successfully registered Windows background task {Identifier}", registration.Identifier);
                return Task.FromResult(true);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException)
            {
                // Windows Background Task APIs require package identity (MSIX)
                // Task data has already been stored, so the task will be available when app is running
                Logger.LogWarning(ex, "Windows background task registration not available for {Identifier} - app may be running unpackaged. Task data has been stored.", registration.Identifier);
                return Task.FromResult(true); // Return true because the task data was stored successfully
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to register Windows background task for {Identifier}", registration.Identifier);
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc />
        protected override Task<bool> UnregisterPlatformTaskAsync(string identifier, CancellationToken cancellationToken)
        {
            try
            {
                string taskName = GetTaskName(identifier);
                bool taskFound = UnregisterExistingTask(taskName);

                // Clear stored data regardless of whether the task was found
                ClearTaskData(identifier);

                if (taskFound)
                {
                    Logger.LogInformation("Successfully unregistered Windows background task {Identifier}", identifier);
                }
                else
                {
                    Logger.LogDebug("Windows background task {Identifier} was not registered, skipping unregister", identifier);
                }

                // Return true even if the task wasn't found - the end result is the same (task is not registered)
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to unregister Windows background task for {Identifier}", identifier);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Unregisters an existing background task by name.
        /// </summary>
        /// <param name="taskName">The name of the task to unregister.</param>
        /// <returns><c>true</c> if the task was found and unregistered; otherwise, <c>false</c>.</returns>
        private bool UnregisterExistingTask(string taskName)
        {
            try
            {
                foreach (KeyValuePair<Guid, IBackgroundTaskRegistration> task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == taskName)
                    {
                        try
                        {
                            task.Value.Unregister(true);
                        }
                        catch (System.Runtime.InteropServices.COMException ex)
                        {
                            // 0x80070490 = ERROR_NOT_FOUND - The task was already unregistered
                            Logger.LogDebug(ex, "Task {TaskName} was already unregistered or not found", taskName);
                        }
                        _registeredBackgroundTasks.Remove(taskName);
                        return true;
                    }
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException)
            {
                // Windows Background Task APIs require package identity (MSIX)
                Logger.LogDebug(ex, "Background task APIs not available for {TaskName} - app may be running unpackaged", taskName);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Error while iterating background tasks for {TaskName}", taskName);
            }

            // Also remove from our local cache even if not found in system
            _registeredBackgroundTasks.Remove(taskName);
            return false;
        }

        /// <summary>
        /// Gets the Windows-specific task name.
        /// </summary>
        /// <param name="identifier">The task identifier.</param>
        /// <returns>The Windows task name.</returns>
        private static string GetTaskName(string identifier)
        {
            return $"Hesoyam_{identifier}";
        }

        /// <summary>
        /// Stores task registration data in local settings.
        /// </summary>
        /// <param name="registration">The task registration.</param>
        private void StoreTaskData(TaskRegistration registration)
        {
            try
            {
                IPreferencesProvider preferencesProvider = _serviceProvider.GetRequiredService<IPreferencesProvider>();
                string key = GetTaskDataKey(registration.Identifier);
                WindowsTaskData taskData = new()
                {
                    TaskTypeName = registration.TaskTypeName,
                    Identifier = registration.Identifier
                };
                string json = JsonSerializer.Serialize(taskData, HesoyamJsonContext.Default.WindowsTaskData);
                preferencesProvider.Set(key, json);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Error storing task data for {Identifier}", registration.Identifier);
            }
        }

        /// <summary>
        /// Clears stored task data by setting the value to an empty string.
        /// </summary>
        /// <param name="identifier">The task identifier.</param>
        /// <remarks>
        /// Since <see cref="IPreferencesProvider"/> does not expose a Remove method,
        /// clearing is implemented by setting the value to an empty string.
        /// The task data retrieval logic treats empty strings as missing data.
        /// </remarks>
        private void ClearTaskData(string identifier)
        {
            try
            {
                IPreferencesProvider preferencesProvider = _serviceProvider.GetRequiredService<IPreferencesProvider>();
                string key = GetTaskDataKey(identifier);
                preferencesProvider.Set(key, string.Empty);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Error clearing task data for {Identifier}", identifier);
            }
        }

        /// <summary>
        /// Gets the preferences key for storing task data.
        /// </summary>
        /// <param name="identifier">The task identifier.</param>
        /// <returns>The preferences key.</returns>
        internal static string GetTaskDataKey(string identifier)
        {
            return $"hesoyam_windows_task_{identifier}";
        }

        /// <summary>
        /// Stores an application trigger for later invocation.
        /// </summary>
        /// <param name="identifier">The task identifier.</param>
        /// <param name="trigger">The application trigger.</param>
        private static void StoreAppTrigger(string identifier, ApplicationTrigger trigger)
        {
            // Store in a static dictionary for access by the background task
            _appTriggers[identifier] = trigger;
        }

        private static readonly Dictionary<string, ApplicationTrigger> _appTriggers = [];

        /// <summary>
        /// Triggers an immediate background task execution.
        /// </summary>
        /// <param name="identifier">The task identifier.</param>
        /// <returns>A task representing the async operation.</returns>
        public static async Task TriggerImmediateAsync(string identifier)
        {
            if (_appTriggers.TryGetValue(identifier, out ApplicationTrigger? trigger))
            {
                await trigger.RequestAsync();
            }
        }
    }

    /// <summary>
    /// Windows background task implementation that executes Hesoyam tasks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is the entry point for Windows background task execution.
    /// It must be registered in the application manifest and match the entry point
    /// specified in <see cref="WindowsBackgroundService"/>.
    /// </para>
    /// </remarks>
    public sealed partial class HesoyamBackgroundTask : IBackgroundTask
    {
        private CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// Runs the background task.
        /// </summary>
        /// <param name="taskInstance">The background task instance.</param>
        [UnconditionalSuppressMessage("Trimming", "IL2057:Unrecognized value passed to the parameter 'typeName' of method 'System.Type.GetType(String)'.",
            Justification = "Type names are stored by the application itself and are known at compile time.")]
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            _cancellationTokenSource = new CancellationTokenSource();

            // Register for cancellation
            taskInstance.Canceled += OnTaskCanceled;

            try
            {
                string taskName = taskInstance.Task.Name;
                string identifier = taskName.Replace("Hesoyam_", string.Empty);

                // Get service provider from the app - break down the chain for clarity
                Application? application = Microsoft.Maui.Controls.Application.Current;
                if (application == null)
                {
                    return;
                }

                IElementHandler handler = application.Handler;
                if (handler == null)
                {
                    return;
                }

                IMauiContext? mauiContext = handler.MauiContext;
                if (mauiContext == null)
                {
                    return;
                }

                IServiceProvider serviceProvider = mauiContext.Services;
                if (serviceProvider == null)
                {
                    return;
                }

                // Get task data from preferences provider
                IPreferencesProvider preferencesProvider = serviceProvider.GetRequiredService<IPreferencesProvider>();
                string key = WindowsBackgroundService.GetTaskDataKey(identifier);
                string json = preferencesProvider.Get(key, string.Empty);

                if (string.IsNullOrEmpty(json))
                {
                    return;
                }

                WindowsTaskData? taskData = JsonSerializer.Deserialize(json, HesoyamJsonContext.Default.WindowsTaskData);
                if (taskData == null || string.IsNullOrEmpty(taskData.TaskTypeName))
                {
                    return;
                }

                // Resolve and execute the task
                Type? taskType = Type.GetType(taskData.TaskTypeName);
                if (taskType == null)
                {
                    return;
                }

                ITaskStorageService storageService = serviceProvider.GetRequiredService<ITaskStorageService>();
                TaskRegistration? registration = storageService.GetRegistrationAsync(identifier).GetAwaiter().GetResult();

                if (registration != null)
                {
                    TaskExecutor executor = serviceProvider.GetRequiredService<TaskExecutor>();
                    executor.ExecuteAsync(registration, _cancellationTokenSource.Token).GetAwaiter().GetResult();
                }
            }
            finally
            {
                taskInstance.Canceled -= OnTaskCanceled;
                _cancellationTokenSource?.Dispose();
                deferral.Complete();
            }
        }

        /// <summary>
        /// Handles the task cancellation event.
        /// </summary>
        /// <param name="sender">The background task instance.</param>
        /// <param name="reason">The cancellation reason.</param>
        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}