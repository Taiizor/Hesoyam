using Microsoft.Extensions.Logging;

namespace Hesoyam.Sample.Services
{
    /// <summary>
    /// Implementation of the data synchronization service.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="DataSyncService"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    public class DataSyncService(ILogger<DataSyncService> logger) : IDataSyncService
    {
        private DateTime? _lastSyncTime;

        /// <inheritdoc/>
        public int TotalSyncedItems { get; private set; }

        /// <inheritdoc/>
        public async Task<int> SyncAsync(DateTime? lastSyncTime, CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting data sync from {LastSyncTime}", lastSyncTime ?? DateTime.MinValue);

            // Simulate network delay and data sync
            int itemCount = Random.Shared.Next(5, 25);

            for (int i = 0; i < itemCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Simulate processing time per item
                await Task.Delay(100, cancellationToken);

                logger.LogDebug("Synced item {Current} of {Total}", i + 1, itemCount);
            }

            _lastSyncTime = DateTime.UtcNow;
            TotalSyncedItems += itemCount;

            logger.LogInformation("Data sync completed. Synced {ItemCount} items. Total: {Total}", itemCount, TotalSyncedItems);

            return itemCount;
        }

        /// <inheritdoc/>
        public DateTime? GetLastSyncTime()
        {
            return _lastSyncTime;
        }
    }
}