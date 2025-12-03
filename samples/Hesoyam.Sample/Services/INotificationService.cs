namespace Hesoyam.Sample.Services
{
    /// <summary>
    /// Service interface for notification operations.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends a local notification.
        /// </summary>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the notification operation.</returns>
        Task SendNotificationAsync(string title, string message, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the count of notifications sent.
        /// </summary>
        int NotificationsSent { get; }
    }
}