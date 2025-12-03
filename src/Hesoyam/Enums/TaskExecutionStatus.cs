using Hesoyam.Abstractions;

namespace Hesoyam.Enums
{
    /// <summary>
    /// Defines the execution status of a background task.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This enumeration represents the various states a background task can be in during its lifecycle.
    /// Task status transitions follow a specific flow: <see cref="Pending"/> → <see cref="Running"/> → <see cref="Completed"/>/<see cref="Failed"/>/<see cref="Cancelled"/>.
    /// </para>
    /// <para>
    /// Use this enumeration to track and monitor the progress of scheduled background tasks.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var status = await scheduler.GetTaskStatusAsync(taskId);
    /// if (status == TaskStatus.Completed)
    /// {
    ///     Console.WriteLine("Task completed successfully!");
    /// }
    /// </code>
    /// </example>
    public enum TaskExecutionStatus
    {
        /// <summary>
        /// The task has not yet started and is waiting to be executed.
        /// </summary>
        /// <remarks>
        /// Tasks in this state are queued and will be executed when their scheduled time arrives
        /// or when system resources become available.
        /// </remarks>
        Pending = 0,

        /// <summary>
        /// The task is currently being executed.
        /// </summary>
        /// <remarks>
        /// A task in this state is actively running. Only one instance of a unique task identifier
        /// can be in the Running state at any given time.
        /// </remarks>
        Running = 1,

        /// <summary>
        /// The task has completed successfully.
        /// </summary>
        /// <remarks>
        /// This state indicates that the task's <see cref="IBackgroundTask.ExecuteAsync"/> method
        /// returned without throwing an exception.
        /// </remarks>
        Completed = 2,

        /// <summary>
        /// The task has failed due to an error.
        /// </summary>
        /// <remarks>
        /// This state indicates that the task threw an exception during execution.
        /// Check the task's error details for more information about the failure.
        /// </remarks>
        Failed = 3,

        /// <summary>
        /// The task was cancelled before completion.
        /// </summary>
        /// <remarks>
        /// This state indicates that the task was explicitly cancelled via the
        /// <see cref="CancellationToken"/> or through the scheduler's cancellation methods.
        /// </remarks>
        Cancelled = 4
    }
}