using Hesoyam.Configuration;
using Hesoyam.Enums;

namespace Hesoyam.Abstractions
{
    /// <summary>
    /// Defines the contract for scheduling and managing background tasks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The task scheduler is the main entry point for registering, managing, and
    /// monitoring background tasks. It handles platform-specific implementations
    /// while providing a unified API.
    /// </para>
    /// <para>
    /// <strong>Key Features:</strong>
    /// <list type="bullet">
    ///     <item><description>Schedule one-shot and periodic tasks</description></item>
    ///     <item><description>Configure execution constraints (network, charging, etc.)</description></item>
    ///     <item><description>Monitor task status and execution history</description></item>
    ///     <item><description>Cancel tasks individually or by tag</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong>
    /// All methods on this interface are thread-safe and can be called from any thread.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register the scheduler in your MAUI app
    /// builder.Services.AddHesoyam();
    /// 
    /// // Schedule a task
    /// var scheduler = serviceProvider.GetRequiredService&lt;ITaskScheduler&gt;();
    /// 
    /// var config = new TaskConfiguration
    /// {
    ///     Identifier = "data-sync",
    ///     Type = TaskType.Periodic,
    ///     Interval = TimeSpan.FromHours(1),
    ///     NetworkRequirement = NetworkType.Connected
    /// };
    /// 
    /// await scheduler.ScheduleAsync&lt;DataSyncTask&gt;(config);
    /// </code>
    /// </example>
    /// <seealso cref="IBackgroundTask"/>
    /// <seealso cref="TaskConfiguration"/>
    public interface ITaskScheduler
    {
        /// <summary>
        /// Schedules a background task for execution.
        /// </summary>
        /// <typeparam name="TTask">
        /// The type of the background task to schedule. Must implement <see cref="IBackgroundTask"/>.
        /// </typeparam>
        /// <param name="configuration">The configuration defining how the task should be scheduled.</param>
        /// <param name="cancellationToken">A token to cancel the scheduling operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous scheduling operation.
        /// Returns <see langword="true"/> if the task was successfully scheduled; 
        /// <see langword="false"/> if scheduling failed.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If a task with the same <see cref="TaskConfiguration.Identifier"/> already exists,
        /// the existing task will be replaced with the new configuration.
        /// </para>
        /// <para>
        /// The task type <typeparamref name="TTask"/> must be registered in the dependency injection
        /// container. The scheduler will resolve the task from the container when execution time arrives.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="configuration"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <see cref="TaskConfiguration.Identifier"/> is null, empty, or whitespace.
        /// </exception>
        /// <example>
        /// <code>
        /// var config = new TaskConfiguration
        /// {
        ///     Identifier = "cleanup-task",
        ///     Type = TaskType.Periodic,
        ///     Interval = TimeSpan.FromDays(1)
        /// };
        /// 
        /// var success = await scheduler.ScheduleAsync&lt;CleanupTask&gt;(config);
        /// if (success)
        /// {
        ///     Console.WriteLine("Task scheduled successfully!");
        /// }
        /// </code>
        /// </example>
        Task<bool> ScheduleAsync<TTask>(TaskConfiguration configuration, CancellationToken cancellationToken = default)
            where TTask : class, IBackgroundTask;

        /// <summary>
        /// Schedules a background task using its runtime type.
        /// </summary>
        /// <param name="taskType">The type of the background task to schedule.</param>
        /// <param name="configuration">The configuration defining how the task should be scheduled.</param>
        /// <param name="cancellationToken">A token to cancel the scheduling operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous scheduling operation.
        /// Returns <see langword="true"/> if the task was successfully scheduled; 
        /// <see langword="false"/> if scheduling failed.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Use this overload when the task type is only known at runtime. For compile-time
        /// type safety, prefer <see cref="ScheduleAsync{TTask}"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="taskType"/> or <paramref name="configuration"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="taskType"/> does not implement <see cref="IBackgroundTask"/>.
        /// </exception>
        Task<bool> ScheduleAsync(Type taskType, TaskConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels a scheduled task by its identifier.
        /// </summary>
        /// <param name="identifier">The unique identifier of the task to cancel.</param>
        /// <param name="cancellationToken">A token to cancel the cancellation operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns <see langword="true"/> if the task was found and cancelled; 
        /// <see langword="false"/> if no task with the specified identifier exists.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If the task is currently executing, it will receive a cancellation request
        /// through its <see cref="CancellationToken"/>. The task should handle this
        /// gracefully and terminate as soon as possible.
        /// </para>
        /// <para>
        /// Cancelling a task removes it from the scheduler. To temporarily pause a task,
        /// consider using a custom mechanism through task metadata.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var cancelled = await scheduler.CancelAsync("data-sync");
        /// if (cancelled)
        /// {
        ///     Console.WriteLine("Task cancelled successfully!");
        /// }
        /// </code>
        /// </example>
        Task<bool> CancelAsync(string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels all scheduled tasks that have the specified tag.
        /// </summary>
        /// <param name="tag">The tag identifying the tasks to cancel.</param>
        /// <param name="cancellationToken">A token to cancel the cancellation operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns the number of tasks that were cancelled.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Use tags to group related tasks and cancel them together. This is useful for
        /// scenarios like cancelling all sync operations or all tasks for a specific user.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Cancel all sync-related tasks
        /// var count = await scheduler.CancelByTagAsync("sync");
        /// Console.WriteLine($"Cancelled {count} tasks");
        /// </code>
        /// </example>
        Task<int> CancelByTagAsync(string tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels all scheduled tasks.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the cancellation operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns the number of tasks that were cancelled.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <strong>Warning:</strong> This operation cannot be undone. All tasks will need
        /// to be rescheduled after calling this method.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Cancel all tasks (e.g., on user logout)
        /// var count = await scheduler.CancelAllAsync();
        /// Console.WriteLine($"Cancelled {count} tasks");
        /// </code>
        /// </example>
        Task<int> CancelAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current status of a scheduled task.
        /// </summary>
        /// <param name="identifier">The unique identifier of the task.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns the <see cref="TaskExecutionStatus"/> if the task is found; 
        /// otherwise, <see langword="null"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The status reflects the current state of the task:
        /// <list type="bullet">
        ///     <item><description><see cref="TaskExecutionStatus.Pending"/> - Task is scheduled but not yet running</description></item>
        ///     <item><description><see cref="TaskExecutionStatus.Running"/> - Task is currently executing</description></item>
        ///     <item><description><see cref="TaskExecutionStatus.Completed"/> - Last execution completed successfully</description></item>
        ///     <item><description><see cref="TaskExecutionStatus.Failed"/> - Last execution failed</description></item>
        ///     <item><description><see cref="TaskExecutionStatus.Cancelled"/> - Task was cancelled</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var status = await scheduler.GetStatusAsync("data-sync");
        /// if (status == TaskExecutionStatus.Running)
        /// {
        ///     Console.WriteLine("Sync is in progress...");
        /// }
        /// </code>
        /// </example>
        Task<TaskExecutionStatus?> GetStatusAsync(string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of all scheduled task identifiers.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns a read-only collection of task identifiers.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method returns identifiers for all registered tasks, regardless of their
        /// current status. Use <see cref="GetStatusAsync"/> to check individual task states.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var tasks = await scheduler.GetScheduledTasksAsync();
        /// foreach (var taskId in tasks)
        /// {
        ///     var status = await scheduler.GetStatusAsync(taskId);
        ///     Console.WriteLine($"{taskId}: {status}");
        /// }
        /// </code>
        /// </example>
        Task<IReadOnlyList<string>> GetScheduledTasksAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a task with the specified identifier is scheduled.
        /// </summary>
        /// <param name="identifier">The unique identifier of the task.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns <see langword="true"/> if the task is scheduled; otherwise, <see langword="false"/>.
        /// </returns>
        /// <example>
        /// <code>
        /// if (!await scheduler.IsScheduledAsync("data-sync"))
        /// {
        ///     await scheduler.ScheduleAsync&lt;DataSyncTask&gt;(config);
        /// }
        /// </code>
        /// </example>
        Task<bool> IsScheduledAsync(string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the configuration for a scheduled task.
        /// </summary>
        /// <param name="identifier">The unique identifier of the task.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns the <see cref="TaskConfiguration"/> if found; otherwise, <see langword="null"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The returned configuration is a copy; modifications will not affect the scheduled task.
        /// To update a task's configuration, cancel it and reschedule with new settings.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var config = await scheduler.GetConfigurationAsync("data-sync");
        /// if (config != null)
        /// {
        ///     Console.WriteLine($"Task interval: {config.Interval}");
        /// }
        /// </code>
        /// </example>
        Task<TaskConfiguration?> GetConfigurationAsync(string identifier, CancellationToken cancellationToken = default);
    }
}