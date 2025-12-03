using Hesoyam.Abstractions;
using Hesoyam.Sample.Services;
using Microsoft.Extensions.Logging;

namespace Hesoyam.Sample.Tasks
{
    /// <summary>
    /// Background task that sends periodic notifications.
    /// Demonstrates immediate task execution with metadata.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="NotificationTask"/> class.
    /// </remarks>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="logger">The logger instance.</param>
    public class NotificationTask(INotificationService notificationService, ILogger<NotificationTask> logger) : IBackgroundTask
    {
        /// <inheritdoc/>
        public async Task ExecuteAsync(ITaskContext context, CancellationToken cancellationToken)
        {
            logger.LogInformation("NotificationTask started. TaskId: {TaskId}", context.TaskId);

            try
            {
                await context.ReportProgressAsync("Preparing notification...");

                // Get notification content from metadata
                string title = context.Configuration.Metadata.TryGetValue("title", out string? t)
                    ? t
                    : "Hesoyam Notification";

                string message = context.Configuration.Metadata.TryGetValue("message", out string? m)
                    ? m
                    : "Background task executed successfully!";

                // Track notification count in state
                int notificationCount = context.GetState<int>("notificationCount");

                // Send the notification
                await notificationService.SendNotificationAsync(title, message, cancellationToken);

                // Update state
                await context.SetStateAsync("notificationCount", notificationCount + 1);
                await context.SetStateAsync("lastNotificationTime", DateTime.UtcNow);

                await context.ReportProgressAsync("Notification sent!");

                logger.LogInformation(
                    "NotificationTask completed. Notification #{Count} sent: {Title} - {Message}",
                    notificationCount + 1, title, message);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("NotificationTask was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "NotificationTask failed with error");
                throw;
            }
        }
    }
}