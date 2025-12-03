namespace Hesoyam.Enums
{
    /// <summary>
    /// Defines the priority levels for background task execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Task priority affects the order in which tasks are executed when multiple tasks
    /// are scheduled to run at the same time. Higher priority tasks are executed first.
    /// </para>
    /// <para>
    /// Priority also affects system resource allocation on some platforms.
    /// High priority tasks may receive more CPU time and are less likely to be deferred
    /// by battery optimization systems.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new TaskConfiguration
    /// {
    ///     Priority = TaskPriority.High,
    ///     Identifier = "critical-sync"
    /// };
    /// </code>
    /// </example>
    public enum TaskPriority
    {
        /// <summary>
        /// Lowest priority level. Tasks may be significantly delayed or batched.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this priority for non-urgent background operations that can wait
        /// for optimal conditions (e.g., device charging, Wi-Fi connected).
        /// </para>
        /// <para>
        /// Low priority tasks are most likely to be affected by battery optimization.
        /// </para>
        /// </remarks>
        Low = 0,

        /// <summary>
        /// Default priority level. Standard execution without special treatment.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the default priority for all tasks. Use this for regular
        /// background operations that should run in a reasonable timeframe.
        /// </para>
        /// </remarks>
        Normal = 1,

        /// <summary>
        /// Higher priority level. Tasks are executed before normal priority tasks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this priority for important operations that should not be delayed,
        /// such as user-initiated synchronization or critical updates.
        /// </para>
        /// <para>
        /// High priority tasks receive preferential treatment but may still be
        /// subject to system constraints.
        /// </para>
        /// </remarks>
        High = 2,

        /// <summary>
        /// Highest priority level. Reserved for critical system operations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this priority sparingly for critical operations that must execute
        /// as soon as possible. Overuse of critical priority may lead to
        /// excessive battery consumption and poor user experience.
        /// </para>
        /// <para>
        /// <strong>Warning:</strong> Some platforms may ignore this priority level
        /// or downgrade it to High for non-system applications.
        /// </para>
        /// </remarks>
        Critical = 3
    }
}