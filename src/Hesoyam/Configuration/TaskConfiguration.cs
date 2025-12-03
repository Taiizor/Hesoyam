using Hesoyam.Enums;

namespace Hesoyam.Configuration
{
    /// <summary>
    /// Represents the configuration settings for a background task.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this class to configure all aspects of a background task including scheduling,
    /// constraints, retry behavior, and platform-specific options.
    /// </para>
    /// <para>
    /// <strong>Required Properties:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description><see cref="Identifier"/> - A unique identifier for the task.</description>
    ///     </item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example Usage:</strong>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new TaskConfiguration
    /// {
    ///     Identifier = "daily-sync",
    ///     Type = TaskType.Periodic,
    ///     Interval = TimeSpan.FromHours(24),
    ///     Priority = TaskPriority.Normal,
    ///     NetworkRequirement = NetworkType.Connected,
    ///     RequiresCharging = false,
    ///     PersistAcrossReboots = true
    /// };
    /// 
    /// await scheduler.ScheduleAsync&lt;MySyncTask&gt;(config);
    /// </code>
    /// </example>
    public sealed class TaskConfiguration
    {
        /// <summary>
        /// Gets or sets the unique identifier for this task.
        /// </summary>
        /// <value>
        /// A unique string identifier used to track and manage the task.
        /// </value>
        /// <remarks>
        /// <para>
        /// The identifier must be unique within the application. Using the same identifier
        /// for multiple tasks will cause the previous task to be replaced.
        /// </para>
        /// <para>
        /// Convention: Use a descriptive, lowercase, hyphen-separated name (e.g., "user-data-sync", "cleanup-cache").
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when attempting to set a null value.</exception>
        /// <exception cref="ArgumentException">Thrown when attempting to set an empty or whitespace value.</exception>
        public required string Identifier { get; set; }

        /// <summary>
        /// Gets or sets the type of task scheduling.
        /// </summary>
        /// <value>
        /// The scheduling type. Defaults to <see cref="TaskType.OneShot"/>.
        /// </value>
        /// <remarks>
        /// <para>
        /// The task type determines how the task is scheduled and whether it repeats:
        /// <list type="bullet">
        ///     <item>
        ///         <description><see cref="TaskType.OneShot"/> - Executes once and is removed.</description>
        ///     </item>
        ///     <item>
        ///         <description><see cref="TaskType.Periodic"/> - Repeats at the specified <see cref="Interval"/>.</description>
        ///     </item>
        ///     <item>
        ///         <description><see cref="TaskType.Immediate"/> - Executes as soon as possible.</description>
        ///     </item>
        /// </list>
        /// </para>
        /// </remarks>
        public TaskType Type { get; set; } = TaskType.OneShot;

        /// <summary>
        /// Gets or sets the interval between periodic task executions.
        /// </summary>
        /// <value>
        /// The time interval between executions. Defaults to 15 minutes.
        /// </value>
        /// <remarks>
        /// <para>
        /// This property is only applicable when <see cref="Type"/> is set to <see cref="TaskType.Periodic"/>.
        /// </para>
        /// <para>
        /// <strong>Platform Minimum Intervals:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>Android: 15 minutes (enforced by WorkManager)</description>
        ///     </item>
        ///     <item>
        ///         <description>iOS: System-controlled (typically 15+ minutes)</description>
        ///     </item>
        ///     <item>
        ///         <description>Windows: 15 minutes (enforced by BackgroundTaskBuilder)</description>
        ///     </item>
        /// </list>
        /// </para>
        /// <para>
        /// Setting an interval smaller than the platform minimum will result in the minimum being used.
        /// </para>
        /// </remarks>
        public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Gets or sets the initial delay before the first task execution.
        /// </summary>
        /// <value>
        /// The initial delay. Defaults to <see cref="TimeSpan.Zero"/> (no delay).
        /// </value>
        /// <remarks>
        /// <para>
        /// Use this property to defer the first execution of a task. This is useful when you want
        /// to schedule a task that should not run immediately upon registration.
        /// </para>
        /// <para>
        /// For <see cref="TaskType.Immediate"/> tasks, this property is ignored.
        /// </para>
        /// </remarks>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the priority level for this task.
        /// </summary>
        /// <value>
        /// The task priority. Defaults to <see cref="TaskPriority.Normal"/>.
        /// </value>
        /// <remarks>
        /// <para>
        /// Higher priority tasks are executed before lower priority tasks when multiple
        /// tasks are scheduled to run at the same time.
        /// </para>
        /// <para>
        /// Use <see cref="TaskPriority.Critical"/> sparingly as it may impact battery life.
        /// </para>
        /// </remarks>
        /// <seealso cref="TaskPriority"/>
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        /// <summary>
        /// Gets or sets the network connectivity requirement for this task.
        /// </summary>
        /// <value>
        /// The network requirement. Defaults to <see cref="NetworkType.None"/>.
        /// </value>
        /// <remarks>
        /// <para>
        /// Set this property to ensure the task only runs when appropriate network
        /// conditions are met. This is especially important for tasks that sync data
        /// or download content.
        /// </para>
        /// </remarks>
        /// <seealso cref="NetworkType"/>
        public NetworkType NetworkRequirement { get; set; } = NetworkType.None;

        /// <summary>
        /// Gets or sets a value indicating whether the device must be charging for the task to run.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the device must be charging; otherwise, <see langword="false"/>. 
        /// Defaults to <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// <para>
        /// Setting this to <see langword="true"/> ensures the task only runs when the device is
        /// connected to a power source. This is recommended for resource-intensive tasks
        /// to preserve battery life.
        /// </para>
        /// </remarks>
        public bool RequiresCharging { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the device must be idle for the task to run.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the device must be idle; otherwise, <see langword="false"/>. 
        /// Defaults to <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// <para>
        /// An idle device typically means the screen is off and the device has been inactive
        /// for a period of time. This constraint helps ensure tasks don't impact the user experience.
        /// </para>
        /// <para>
        /// <strong>Platform Support:</strong> Not all platforms support idle detection.
        /// </para>
        /// </remarks>
        public bool RequiresDeviceIdle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the device must have sufficient battery level.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the device must not be low on battery; otherwise, <see langword="false"/>. 
        /// Defaults to <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// <para>
        /// When set to <see langword="true"/>, the task will only run if the device has
        /// sufficient battery level (typically above 15-20%).
        /// </para>
        /// </remarks>
        public bool RequiresBatteryNotLow { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the task registration should persist across device reboots.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the task should be rescheduled after a device reboot; 
        /// otherwise, <see langword="false"/>. Defaults to <see langword="true"/>.
        /// </value>
        /// <remarks>
        /// <para>
        /// When enabled, the task will be automatically rescheduled when the device restarts.
        /// This requires additional permissions on some platforms:
        /// <list type="bullet">
        ///     <item>
        ///         <description>Android: RECEIVE_BOOT_COMPLETED permission</description>
        ///     </item>
        ///     <item>
        ///         <description>iOS: BGTaskSchedulerPermittedIdentifiers in Info.plist</description>
        ///     </item>
        /// </list>
        /// </para>
        /// </remarks>
        public bool PersistAcrossReboots { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts if the task fails.
        /// </summary>
        /// <value>
        /// The maximum number of retries. Defaults to 3.
        /// </value>
        /// <remarks>
        /// <para>
        /// When a task fails (throws an exception), it will be automatically retried
        /// up to this number of times. Set to 0 to disable automatic retries.
        /// </para>
        /// <para>
        /// The retry delay follows an exponential backoff strategy configured by
        /// <see cref="RetryBackoffMultiplier"/>.
        /// </para>
        /// </remarks>
        /// <seealso cref="RetryBackoffMultiplier"/>
        /// <seealso cref="InitialRetryDelay"/>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay before the first retry attempt.
        /// </summary>
        /// <value>
        /// The initial retry delay. Defaults to 30 seconds.
        /// </value>
        /// <remarks>
        /// <para>
        /// This delay is applied before the first retry. Subsequent retries use
        /// exponential backoff with the <see cref="RetryBackoffMultiplier"/>.
        /// </para>
        /// </remarks>
        /// <seealso cref="MaxRetries"/>
        /// <seealso cref="RetryBackoffMultiplier"/>
        public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the backoff multiplier for retry delay calculation.
        /// </summary>
        /// <value>
        /// The backoff multiplier. Defaults to 2.0.
        /// </value>
        /// <remarks>
        /// <para>
        /// Each retry delay is calculated as: <c>InitialRetryDelay * (RetryBackoffMultiplier ^ attemptNumber)</c>
        /// </para>
        /// <para>
        /// For example, with an initial delay of 30 seconds and multiplier of 2.0:
        /// <list type="bullet">
        ///     <item><description>Retry 1: 30 seconds</description></item>
        ///     <item><description>Retry 2: 60 seconds</description></item>
        ///     <item><description>Retry 3: 120 seconds</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <seealso cref="MaxRetries"/>
        /// <seealso cref="InitialRetryDelay"/>
        public double RetryBackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Gets or sets additional metadata associated with this task.
        /// </summary>
        /// <value>
        /// A dictionary of key-value pairs for storing custom data. Defaults to an empty dictionary.
        /// </value>
        /// <remarks>
        /// <para>
        /// Use this property to pass configuration data or context to your task implementation.
        /// The data is serialized and persisted with the task registration.
        /// </para>
        /// <para>
        /// <strong>Note:</strong> Keep metadata lightweight. Large amounts of data may affect
        /// serialization performance and storage.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// config.Metadata["userId"] = "12345";
        /// config.Metadata["syncScope"] = "full";
        /// </code>
        /// </example>
        public Dictionary<string, string> Metadata { get; set; } = [];

        /// <summary>
        /// Gets or sets the maximum execution time allowed for the task.
        /// </summary>
        /// <value>
        /// The maximum execution timeout. Defaults to 10 minutes.
        /// </value>
        /// <remarks>
        /// <para>
        /// If the task does not complete within this time, it will be cancelled.
        /// Set an appropriate timeout based on your task's expected duration.
        /// </para>
        /// <para>
        /// <strong>Platform Limits:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>iOS: Maximum 30 seconds for background tasks.</description>
        ///     </item>
        ///     <item>
        ///         <description>Android: WorkManager has a 10-minute limit per task.</description>
        ///     </item>
        /// </list>
        /// </para>
        /// </remarks>
        public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets or sets the tags associated with this task for categorization.
        /// </summary>
        /// <value>
        /// A collection of tags. Defaults to an empty list.
        /// </value>
        /// <remarks>
        /// <para>
        /// Tags can be used to group related tasks and perform bulk operations
        /// like cancelling all tasks with a specific tag.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// config.Tags.Add("sync");
        /// config.Tags.Add("user-data");
        /// 
        /// // Later, cancel all sync tasks
        /// await scheduler.CancelByTagAsync("sync");
        /// </code>
        /// </example>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>A new <see cref="TaskConfiguration"/> instance with the same values.</returns>
        /// <remarks>
        /// <para>
        /// Use this method to create a copy of a configuration when you need to
        /// create multiple similar tasks with slight variations.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var baseConfig = new TaskConfiguration { Identifier = "base", Priority = TaskPriority.High };
        /// var syncConfig = baseConfig.Clone();
        /// syncConfig.Identifier = "sync-task";
        /// </code>
        /// </example>
        public TaskConfiguration Clone()
        {
            return new TaskConfiguration
            {
                Identifier = Identifier,
                Type = Type,
                Interval = Interval,
                InitialDelay = InitialDelay,
                Priority = Priority,
                NetworkRequirement = NetworkRequirement,
                RequiresCharging = RequiresCharging,
                RequiresDeviceIdle = RequiresDeviceIdle,
                RequiresBatteryNotLow = RequiresBatteryNotLow,
                PersistAcrossReboots = PersistAcrossReboots,
                MaxRetries = MaxRetries,
                InitialRetryDelay = InitialRetryDelay,
                RetryBackoffMultiplier = RetryBackoffMultiplier,
                Metadata = new Dictionary<string, string>(Metadata),
                ExecutionTimeout = ExecutionTimeout,
                Tags = [.. Tags]
            };
        }
    }
}