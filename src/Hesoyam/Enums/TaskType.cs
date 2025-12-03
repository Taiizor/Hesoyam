using Hesoyam.Configuration;

namespace Hesoyam.Enums
{
    /// <summary>
    /// Defines the scheduling type for background tasks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This enumeration specifies how a task should be scheduled and executed.
    /// Different types affect when and how often the task will run.
    /// </para>
    /// <para>
    /// Note: Platform-specific limitations may affect the actual behavior of each type.
    /// For example, iOS has strict limitations on background execution time and frequency.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new TaskConfiguration
    /// {
    ///     Type = TaskType.Periodic,
    ///     Interval = TimeSpan.FromHours(1)
    /// };
    /// </code>
    /// </example>
    public enum TaskType
    {
        /// <summary>
        /// A task that executes only once at the scheduled time.
        /// </summary>
        /// <remarks>
        /// <para>
        /// One-shot tasks are ideal for deferred operations that need to run once,
        /// such as scheduled notifications or delayed data synchronization.
        /// </para>
        /// <para>
        /// After completion, the task is automatically removed from the scheduler.
        /// </para>
        /// </remarks>
        OneShot = 0,

        /// <summary>
        /// A task that executes repeatedly at specified intervals.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Periodic tasks continue to run at regular intervals until explicitly cancelled.
        /// Use the <see cref="TaskConfiguration.Interval"/> property to specify the repeat interval.
        /// </para>
        /// <para>
        /// <strong>Platform Notes:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>Android: Minimum interval is 15 minutes for JobScheduler/WorkManager.</description>
        ///     </item>
        ///     <item>
        ///         <description>iOS: Background refresh intervals are controlled by the system.</description>
        ///     </item>
        ///     <item>
        ///         <description>Windows: Uses BackgroundTaskBuilder with time triggers.</description>
        ///     </item>
        /// </list>
        /// </para>
        /// </remarks>
        Periodic = 1,

        /// <summary>
        /// A task that executes immediately when conditions are met.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Immediate tasks bypass scheduled timing and execute as soon as possible,
        /// subject to system constraints and battery optimization settings.
        /// </para>
        /// <para>
        /// Use this type for time-sensitive operations that need to run without delay.
        /// </para>
        /// </remarks>
        Immediate = 2
    }
}