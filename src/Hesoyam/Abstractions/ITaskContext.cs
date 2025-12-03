using Hesoyam.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Hesoyam.Abstractions
{
    /// <summary>
    /// Provides context and utilities for a running background task.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The task context is provided to each task execution and contains:
    /// <list type="bullet">
    ///     <item><description>Task identification and metadata</description></item>
    ///     <item><description>Configuration settings</description></item>
    ///     <item><description>Progress reporting capabilities</description></item>
    ///     <item><description>State persistence for resumable tasks</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Use this context to track progress, store intermediate state, and access
    /// task-specific configuration during execution.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public async Task ExecuteAsync(ITaskContext context, CancellationToken cancellationToken)
    /// {
    ///     // Access task metadata
    ///     var userId = context.Configuration.Metadata["userId"];
    ///     
    ///     // Resume from previous state if available
    ///     var lastProcessedIndex = context.GetState&lt;int&gt;("lastIndex");
    ///     
    ///     for (int i = lastProcessedIndex; i &lt; 100; i++)
    ///     {
    ///         cancellationToken.ThrowIfCancellationRequested();
    ///         
    ///         // Do work...
    ///         
    ///         // Save progress periodically
    ///         await context.SetStateAsync("lastIndex", i);
    ///         await context.ReportProgressAsync(i, 100);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IBackgroundTask"/>
    /// <seealso cref="TaskConfiguration"/>
    public interface ITaskContext
    {
        /// <summary>
        /// Gets the unique identifier for this task execution.
        /// </summary>
        /// <value>
        /// A unique string that identifies this specific task registration.
        /// </value>
        /// <remarks>
        /// <para>
        /// This is the same identifier specified in <see cref="TaskConfiguration.Identifier"/>
        /// when the task was scheduled.
        /// </para>
        /// </remarks>
        string TaskId { get; }

        /// <summary>
        /// Gets the unique identifier for this specific execution attempt.
        /// </summary>
        /// <value>
        /// A <see cref="Guid"/> that uniquely identifies this execution run.
        /// </value>
        /// <remarks>
        /// <para>
        /// Each time a task runs (including retries), it receives a new execution ID.
        /// Use this for logging and correlation purposes.
        /// </para>
        /// </remarks>
        Guid ExecutionId { get; }

        /// <summary>
        /// Gets the current attempt number for this execution.
        /// </summary>
        /// <value>
        /// The attempt number, starting from 1 for the first attempt.
        /// </value>
        /// <remarks>
        /// <para>
        /// This value increases with each retry attempt. Use it to implement
        /// custom retry logic or adjust behavior on subsequent attempts.
        /// </para>
        /// </remarks>
        int AttemptNumber { get; }

        /// <summary>
        /// Gets the timestamp when this execution started.
        /// </summary>
        /// <value>
        /// A <see cref="DateTimeOffset"/> representing when the task began execution.
        /// </value>
        DateTimeOffset StartedAt { get; }

        /// <summary>
        /// Gets the configuration used for this task.
        /// </summary>
        /// <value>
        /// The <see cref="TaskConfiguration"/> that was used to schedule this task.
        /// </value>
        /// <remarks>
        /// <para>
        /// This provides read-only access to the task's configuration, including
        /// metadata and all scheduling options.
        /// </para>
        /// </remarks>
        TaskConfiguration Configuration { get; }

        /// <summary>
        /// Reports the progress of the current task execution.
        /// </summary>
        /// <param name="current">The current progress value.</param>
        /// <param name="total">The total value representing 100% completion.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// <para>
        /// Call this method periodically to report progress. Progress information
        /// may be used for UI updates or monitoring purposes.
        /// </para>
        /// <para>
        /// The progress percentage is calculated as: <c>(current / total) * 100</c>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// for (int i = 0; i &lt; items.Count; i++)
        /// {
        ///     await ProcessItemAsync(items[i]);
        ///     await context.ReportProgressAsync(i + 1, items.Count);
        /// }
        /// </code>
        /// </example>
        Task ReportProgressAsync(int current, int total);

        /// <summary>
        /// Reports a progress message for the current task execution.
        /// </summary>
        /// <param name="message">The progress message to report.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// <para>
        /// Use this overload to report textual progress information that cannot
        /// be expressed as a numeric percentage.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// await context.ReportProgressAsync("Connecting to server...");
        /// await context.ReportProgressAsync("Downloading data...");
        /// await context.ReportProgressAsync("Processing complete.");
        /// </code>
        /// </example>
        Task ReportProgressAsync(string message);

        /// <summary>
        /// Retrieves persisted state for this task.
        /// </summary>
        /// <typeparam name="T">The type of the state value.</typeparam>
        /// <param name="key">The key identifying the state value.</param>
        /// <returns>
        /// The stored value if found; otherwise, the default value for type <typeparamref name="T"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// State is persisted across task executions, allowing you to implement
        /// resumable tasks that can continue from where they left off.
        /// </para>
        /// <para>
        /// State is specific to the task identifier and is not shared between different tasks.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var lastSyncTime = context.GetState&lt;DateTime&gt;("lastSyncTime");
        /// var processedCount = context.GetState&lt;int&gt;("processedCount");
        /// </code>
        /// </example>
        T? GetState<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string key);

        /// <summary>
        /// Persists state for this task.
        /// </summary>
        /// <typeparam name="T">The type of the state value.</typeparam>
        /// <param name="key">The key identifying the state value.</param>
        /// <param name="value">The value to store.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// <para>
        /// State is persisted to storage and survives application restarts and device reboots
        /// (if configured). Use this for checkpointing long-running tasks.
        /// </para>
        /// <para>
        /// <strong>Best Practices:</strong>
        /// <list type="bullet">
        ///     <item><description>Keep state data small and serializable.</description></item>
        ///     <item><description>Save state periodically rather than after every operation.</description></item>
        ///     <item><description>Clear obsolete state when it's no longer needed.</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// await context.SetStateAsync("lastSyncTime", DateTime.UtcNow);
        /// await context.SetStateAsync("cursor", nextPageCursor);
        /// </code>
        /// </example>
        Task SetStateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string key, T value);

        /// <summary>
        /// Clears all persisted state for this task.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// <para>
        /// Call this method when a task completes successfully and no longer needs
        /// its persisted state. This helps keep storage clean and prevents stale data
        /// from affecting future executions.
        /// </para>
        /// </remarks>
        Task ClearStateAsync();
    }
}