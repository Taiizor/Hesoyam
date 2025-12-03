namespace Hesoyam.Sample.Services
{
    /// <summary>
    /// Service interface for data synchronization operations.
    /// </summary>
    public interface IDataSyncService
    {
        /// <summary>
        /// Synchronizes data with the remote server.
        /// </summary>
        /// <param name="lastSyncTime">The timestamp of the last successful sync.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the sync operation, returning the number of items synced.</returns>
        Task<int> SyncAsync(DateTime? lastSyncTime, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the timestamp of the last successful sync.
        /// </summary>
        /// <returns>The last sync timestamp, or null if never synced.</returns>
        DateTime? GetLastSyncTime();

        /// <summary>
        /// Gets the total number of synced items.
        /// </summary>
        int TotalSyncedItems { get; }
    }
}