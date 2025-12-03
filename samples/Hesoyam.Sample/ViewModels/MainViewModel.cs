using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hesoyam.Abstractions;
using Hesoyam.Configuration;
using Hesoyam.Enums;
using Hesoyam.Sample.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Hesoyam.Sample.ViewModels
{
    /// <summary>
    /// ViewModel for the main page demonstrating Hesoyam background task scheduling.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </remarks>
    /// <param name="scheduler">The task scheduler.</param>
    /// <param name="logger">The logger instance.</param>
    public partial class MainViewModel(ITaskScheduler scheduler, ILogger<MainViewModel> logger) : ObservableObject
    {
        /// <summary>
        /// Gets the collection of scheduled task identifiers.
        /// </summary>
        public ObservableCollection<string> ScheduledTasks { get; } = [];

        /// <summary>
        /// Gets the collection of task execution logs.
        /// </summary>
        public ObservableCollection<string> TaskLogs { get; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether a periodic sync is scheduled.
        /// </summary>
        [ObservableProperty]
        private bool _isSyncScheduled;

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = "Ready";

        /// <summary>
        /// Gets or sets the sync interval in minutes.
        /// </summary>
        [ObservableProperty]
        private int _syncIntervalMinutes = 15;

        /// <summary>
        /// Gets or sets a value indicating whether network is required.
        /// </summary>
        [ObservableProperty]
        private bool _requiresNetwork = true;

        /// <summary>
        /// Gets or sets a value indicating whether charging is required.
        /// </summary>
        [ObservableProperty]
        private bool _requiresCharging;

        /// <summary>
        /// Schedules a periodic data sync task.
        /// </summary>
        [RelayCommand]
        private async Task SchedulePeriodicSyncAsync()
        {
            try
            {
                StatusMessage = "Scheduling periodic sync...";
                AddLog("Scheduling periodic sync task...");

                TaskConfiguration config = new()
                {
                    Identifier = "periodic-data-sync",
                    Type = TaskType.Periodic,
                    Interval = TimeSpan.FromMinutes(Math.Max(15, SyncIntervalMinutes)), // Minimum 15 minutes
                    Priority = TaskPriority.Normal,
                    NetworkRequirement = RequiresNetwork ? NetworkType.Connected : NetworkType.None,
                    RequiresCharging = RequiresCharging,
                    PersistAcrossReboots = true,
                    MaxRetries = 3,
                    Tags = ["sync", "periodic"],
                    Metadata = new Dictionary<string, string>
                    {
                        ["scheduledAt"] = DateTime.UtcNow.ToString("O"),
                        ["source"] = "sample-app"
                    }
                };

                bool success = await scheduler.ScheduleAsync<DataSyncTask>(config);

                if (success)
                {
                    IsSyncScheduled = true;
                    StatusMessage = "Periodic sync scheduled!";
                    AddLog($"✓ Periodic sync scheduled (interval: {config.Interval.TotalMinutes} min)");
                    logger.LogInformation("Periodic sync task scheduled successfully");
                }
                else
                {
                    StatusMessage = "Failed to schedule sync";
                    AddLog("✗ Failed to schedule periodic sync");
                    logger.LogWarning("Failed to schedule periodic sync task");
                }

                await RefreshScheduledTasksAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                AddLog($"✗ Error: {ex.Message}");
                logger.LogError(ex, "Error scheduling periodic sync");
            }
        }

        /// <summary>
        /// Runs an immediate data sync task.
        /// </summary>
        [RelayCommand]
        private async Task RunImmediateSyncAsync()
        {
            try
            {
                StatusMessage = "Running immediate sync...";
                AddLog("Running immediate sync task...");

                TaskConfiguration config = new()
                {
                    Identifier = $"immediate-sync-{DateTime.UtcNow.Ticks}",
                    Type = TaskType.Immediate,
                    Priority = TaskPriority.High,
                    Tags = ["sync", "immediate"],
                    Metadata = new Dictionary<string, string>
                    {
                        ["triggeredBy"] = "user"
                    }
                };

                bool success = await scheduler.ScheduleAsync<DataSyncTask>(config);

                if (success)
                {
                    StatusMessage = "Immediate sync started!";
                    AddLog("✓ Immediate sync task started");
                    logger.LogInformation("Immediate sync task started");
                }
                else
                {
                    StatusMessage = "Failed to start sync";
                    AddLog("✗ Failed to start immediate sync");
                }

                await RefreshScheduledTasksAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                AddLog($"✗ Error: {ex.Message}");
                logger.LogError(ex, "Error running immediate sync");
            }
        }

        /// <summary>
        /// Schedules a one-shot cleanup task.
        /// </summary>
        [RelayCommand]
        private async Task ScheduleCleanupAsync()
        {
            try
            {
                StatusMessage = "Scheduling cleanup...";
                AddLog("Scheduling cleanup task...");

                TaskConfiguration config = new()
                {
                    Identifier = "one-shot-cleanup",
                    Type = TaskType.OneShot,
                    InitialDelay = TimeSpan.FromSeconds(5),
                    Priority = TaskPriority.Low,
                    RequiresDeviceIdle = true,
                    RequiresBatteryNotLow = true,
                    Tags = ["cleanup", "maintenance"],
                    Metadata = new Dictionary<string, string>
                    {
                        ["reason"] = "user-requested"
                    }
                };

                bool success = await scheduler.ScheduleAsync<CleanupTask>(config);

                if (success)
                {
                    StatusMessage = "Cleanup scheduled!";
                    AddLog("✓ Cleanup task scheduled (delay: 5s)");
                    logger.LogInformation("Cleanup task scheduled");
                }
                else
                {
                    StatusMessage = "Failed to schedule cleanup";
                    AddLog("✗ Failed to schedule cleanup");
                }

                await RefreshScheduledTasksAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                AddLog($"✗ Error: {ex.Message}");
                logger.LogError(ex, "Error scheduling cleanup");
            }
        }

        /// <summary>
        /// Sends an immediate notification task.
        /// </summary>
        [RelayCommand]
        private async Task SendNotificationAsync()
        {
            try
            {
                StatusMessage = "Sending notification...";
                AddLog("Scheduling notification task...");

                TaskConfiguration config = new()
                {
                    Identifier = $"notification-{DateTime.UtcNow.Ticks}",
                    Type = TaskType.Immediate,
                    Priority = TaskPriority.High,
                    Tags = ["notification"],
                    Metadata = new Dictionary<string, string>
                    {
                        ["title"] = "Hello from Hesoyam!",
                        ["message"] = $"Background task executed at {DateTime.Now:T}"
                    }
                };

                bool success = await scheduler.ScheduleAsync<NotificationTask>(config);

                if (success)
                {
                    StatusMessage = "Notification sent!";
                    AddLog("✓ Notification task executed");
                    logger.LogInformation("Notification task executed");
                }
                else
                {
                    StatusMessage = "Failed to send notification";
                    AddLog("✗ Failed to send notification");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                AddLog($"✗ Error: {ex.Message}");
                logger.LogError(ex, "Error sending notification");
            }
        }

        /// <summary>
        /// Cancels the periodic sync task.
        /// </summary>
        [RelayCommand]
        private async Task CancelPeriodicSyncAsync()
        {
            try
            {
                StatusMessage = "Cancelling periodic sync...";
                AddLog("Cancelling periodic sync task...");

                bool success = await scheduler.CancelAsync("periodic-data-sync");

                if (success)
                {
                    IsSyncScheduled = false;
                    StatusMessage = "Periodic sync cancelled";
                    AddLog("✓ Periodic sync cancelled");
                    logger.LogInformation("Periodic sync cancelled");
                }
                else
                {
                    StatusMessage = "No sync to cancel";
                    AddLog("✗ No periodic sync found to cancel");
                }

                await RefreshScheduledTasksAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                AddLog($"✗ Error: {ex.Message}");
                logger.LogError(ex, "Error cancelling periodic sync");
            }
        }

        /// <summary>
        /// Cancels all scheduled tasks.
        /// </summary>
        [RelayCommand]
        private async Task CancelAllTasksAsync()
        {
            try
            {
                StatusMessage = "Cancelling all tasks...";
                AddLog("Cancelling all tasks...");

                int count = await scheduler.CancelAllAsync();

                IsSyncScheduled = false;
                StatusMessage = $"Cancelled {count} task(s)";
                AddLog($"✓ Cancelled {count} task(s)");
                logger.LogInformation("Cancelled {Count} tasks", count);

                await RefreshScheduledTasksAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                AddLog($"✗ Error: {ex.Message}");
                logger.LogError(ex, "Error cancelling all tasks");
            }
        }

        /// <summary>
        /// Refreshes the list of scheduled tasks.
        /// </summary>
        [RelayCommand]
        private async Task RefreshScheduledTasksAsync()
        {
            try
            {
                IReadOnlyList<string> tasks = await scheduler.GetScheduledTasksAsync();

                ScheduledTasks.Clear();
                foreach (string taskId in tasks)
                {
                    ScheduledTasks.Add(taskId);
                }

                // Check if periodic sync is scheduled
                IsSyncScheduled = await scheduler.IsScheduledAsync("periodic-data-sync");

                StatusMessage = $"Found {tasks.Count} scheduled task(s)";
                logger.LogDebug("Refreshed scheduled tasks: {Count}", tasks.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                logger.LogError(ex, "Error refreshing scheduled tasks");
            }
        }

        /// <summary>
        /// Clears the task logs.
        /// </summary>
        [RelayCommand]
        private void ClearLogs()
        {
            TaskLogs.Clear();
            AddLog("Logs cleared");
        }

        private void AddLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            TaskLogs.Insert(0, $"[{timestamp}] {message}");

            // Keep only last 50 logs
            while (TaskLogs.Count > 50)
            {
                TaskLogs.RemoveAt(TaskLogs.Count - 1);
            }
        }
    }
}