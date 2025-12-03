using Hesoyam.Abstractions;
using Hesoyam.Sample.Services;
using Microsoft.Extensions.Logging;

namespace Hesoyam.Sample.Tasks
{
    /// <summary>
    /// Background task that synchronizes data with a remote server.
    /// Demonstrates state persistence and progress reporting.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="DataSyncTask"/> class.
    /// </remarks>
    /// <param name="dataSyncService">The data sync service.</param>
    /// <param name="logger">The logger instance.</param>
    public class DataSyncTask(IDataSyncService dataSyncService, ILogger<DataSyncTask> logger) : IBackgroundTask
    {
        /// <inheritdoc/>
        public async Task ExecuteAsync(ITaskContext context, CancellationToken cancellationToken)
        {
            logger.LogInformation("DataSyncTask started. TaskId: {TaskId}", context.TaskId);

            try
            {
                // Report initial progress
                await context.ReportProgressAsync("Starting data synchronization...");

                // Get the last sync time from persisted state
                DateTime? lastSyncTime = context.GetState<DateTime?>("lastSyncTime");

                logger.LogInformation("Last sync time: {LastSyncTime}",
                    lastSyncTime?.ToString("g") ?? "Never");

                // Perform the sync operation
                await context.ReportProgressAsync("Connecting to server...");
                int syncedItems = await dataSyncService.SyncAsync(lastSyncTime, cancellationToken);

                // Save the new sync time to state
                DateTime newSyncTime = DateTime.UtcNow;
                await context.SetStateAsync("lastSyncTime", newSyncTime);

                // Update cumulative stats
                int totalSynced = context.GetState<int>("totalSyncedItems") + syncedItems;
                await context.SetStateAsync("totalSyncedItems", totalSynced);

                // Report completion
                await context.ReportProgressAsync(100, 100);

                logger.LogInformation(
                    "DataSyncTask completed successfully. Synced {SyncedItems} items. Total: {TotalSynced}",
                    syncedItems, totalSynced);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("DataSyncTask was cancelled");
                await context.ReportProgressAsync("Sync cancelled");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DataSyncTask failed with error");
                await context.ReportProgressAsync($"Error: {ex.Message}");
                throw;
            }
        }
    }
}