using Microsoft.Extensions.Logging;

namespace Hesoyam.Sample.Services
{
    /// <summary>
    /// Implementation of the notification service.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    public class NotificationService(ILogger<NotificationService> logger) : INotificationService
    {
        /// <inheritdoc/>
        public int NotificationsSent { get; private set; }

        /// <inheritdoc/>
        public Task SendNotificationAsync(string title, string message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogInformation("Sending notification: {Title} - {Message}", title, message);

            // In a real app, you would use platform-specific notification APIs
            // For demo purposes, we just log the notification
            NotificationsSent++;

            logger.LogInformation("Notification sent successfully. Total sent: {Count}", NotificationsSent);

            return Task.CompletedTask;
        }
    }
}