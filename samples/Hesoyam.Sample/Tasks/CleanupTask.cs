using Hesoyam.Abstractions;
using Microsoft.Extensions.Logging;

namespace Hesoyam.Sample.Tasks
{
    /// <summary>
    /// Background task that performs cleanup operations.
    /// Demonstrates one-shot task execution with constraints.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="CleanupTask"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    public class CleanupTask(ILogger<CleanupTask> logger) : IBackgroundTask
    {
        /// <inheritdoc/>
        public async Task ExecuteAsync(ITaskContext context, CancellationToken cancellationToken)
        {
            logger.LogInformation("CleanupTask started. TaskId: {TaskId}", context.TaskId);

            try
            {
                await context.ReportProgressAsync("Starting cleanup...");

                // Get cleanup stats from state
                DateTime? lastCleanupTime = context.GetState<DateTime?>("lastCleanupTime");
                int totalCleanupsPerformed = context.GetState<int>("totalCleanups");

                logger.LogInformation("Last cleanup: {LastCleanup}",
                    lastCleanupTime?.ToString("g") ?? "Never");

                // Simulate cleanup operations
                string[] steps =
                [
                    "Clearing temporary files",
                    "Removing expired cache entries",
                    "Compacting database",
                    "Optimizing storage"
                ];

                for (int i = 0; i < steps.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await context.ReportProgressAsync(steps[i]);
                    await context.ReportProgressAsync(i + 1, steps.Length);

                    // Simulate work
                    await Task.Delay(500, cancellationToken);

                    logger.LogDebug("Cleanup step completed: {Step}", steps[i]);
                }

                // Save cleanup state
                await context.SetStateAsync("lastCleanupTime", DateTime.UtcNow);
                await context.SetStateAsync("totalCleanups", totalCleanupsPerformed + 1);

                await context.ReportProgressAsync("Cleanup completed!");

                logger.LogInformation("CleanupTask completed successfully. Total cleanups: {Count}",
                    totalCleanupsPerformed + 1);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("CleanupTask was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CleanupTask failed with error");
                throw;
            }
        }
    }
}