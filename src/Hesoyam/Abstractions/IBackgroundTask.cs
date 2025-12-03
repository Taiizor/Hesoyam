namespace Hesoyam.Abstractions
{
    /// <summary>
    /// Defines the contract for a background task that can be executed by the scheduler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement this interface to create custom background tasks. Each implementation
    /// should represent a single, well-defined unit of work that can be executed
    /// independently in the background.
    /// </para>
    /// <para>
    /// <strong>Implementation Guidelines:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>Keep tasks focused and idempotent where possible.</description>
    ///     </item>
    ///     <item>
    ///         <description>Handle cancellation by checking the <see cref="CancellationToken"/> frequently.</description>
    ///     </item>
    ///     <item>
    ///         <description>Store and restore state using the <see cref="ITaskContext"/> for resumable tasks.</description>
    ///     </item>
    ///     <item>
    ///         <description>Use dependency injection for external services.</description>
    ///     </item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Platform Considerations:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>iOS: Tasks are limited to approximately 30 seconds of background execution time.</description>
    ///     </item>
    ///     <item>
    ///         <description>Android: WorkManager imposes a 10-minute limit per task execution.</description>
    ///     </item>
    ///     <item>
    ///         <description>Windows: Background tasks have varying time limits based on trigger type.</description>
    ///     </item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class DataSyncTask : IBackgroundTask
    /// {
    ///     private readonly IDataService _dataService;
    ///     private readonly ILogger&lt;DataSyncTask&gt; _logger;
    /// 
    ///     public DataSyncTask(IDataService dataService, ILogger&lt;DataSyncTask&gt; logger)
    ///     {
    ///         _dataService = dataService;
    ///         _logger = logger;
    ///     }
    /// 
    ///     public async Task ExecuteAsync(ITaskContext context, CancellationToken cancellationToken)
    ///     {
    ///         _logger.LogInformation("Starting data sync for task {TaskId}", context.TaskId);
    ///         
    ///         try
    ///         {
    ///             await _dataService.SyncAsync(cancellationToken);
    ///             _logger.LogInformation("Data sync completed successfully");
    ///         }
    ///         catch (OperationCanceledException)
    ///         {
    ///             _logger.LogWarning("Data sync was cancelled");
    ///             throw;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ITaskContext"/>
    /// <seealso cref="ITaskScheduler"/>
    public interface IBackgroundTask
    {
        /// <summary>
        /// Executes the background task logic.
        /// </summary>
        /// <param name="context">
        /// The execution context providing task metadata, configuration, and progress reporting capabilities.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that indicates when the task should stop execution.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is called by the scheduler when it's time to execute the task.
        /// Implementations should:
        /// </para>
        /// <list type="number">
        ///     <item>
        ///         <description>Check the <paramref name="cancellationToken"/> regularly and stop gracefully when cancelled.</description>
        ///     </item>
        ///     <item>
        ///         <description>Report progress using <see cref="ITaskContext.ReportProgressAsync(int, int)"/> for long-running operations.</description>
        ///     </item>
        ///     <item>
        ///         <description>Throw <see cref="OperationCanceledException"/> when cancellation is requested.</description>
        ///     </item>
        ///     <item>
        ///         <description>Handle transient failures appropriately or let them propagate for automatic retry.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
        /// </exception>
        Task ExecuteAsync(ITaskContext context, CancellationToken cancellationToken);
    }
}