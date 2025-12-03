namespace Hesoyam.Abstractions
{
    /// <summary>
    /// Defines the contract for platform-specific background service implementations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface is implemented by platform-specific services to handle the actual
    /// scheduling and execution of background tasks using native APIs. Each supported
    /// platform (Android, iOS, macOS, Windows) has its own implementation.
    /// </para>
    /// <para>
    /// <strong>Implementation Details:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///             <strong>Android:</strong> Uses WorkManager/JobScheduler for background execution.
    ///             Supports all constraint types and periodic scheduling with a minimum interval of 15 minutes.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <strong>iOS:</strong> Uses BGTaskScheduler for background app refresh and processing.
    ///             Limited to approximately 30 seconds of background execution time.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <strong>macOS Catalyst:</strong> Uses BGTaskScheduler for background processing.
    ///             More lenient timing compared to iOS.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <strong>Windows:</strong> Uses BackgroundTaskBuilder with system conditions and triggers.
    ///             Supports various trigger types including time and system events.
    ///         </description>
    ///     </item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Note:</strong> This interface is not intended to be implemented by consumers.
    /// It is used internally by the library to abstract platform differences.
    /// </para>
    /// </remarks>
    /// <seealso cref="ITaskScheduler"/>
    /// <seealso cref="IBackgroundTask"/>
    public interface IPlatformBackgroundService
    {
        /// <summary>
        /// Gets a value indicating whether background tasks are supported on this platform.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the platform supports background task execution; 
        /// otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// <para>
        /// Some platforms or device configurations may not support background execution.
        /// Check this property before attempting to schedule tasks.
        /// </para>
        /// </remarks>
        bool IsSupported { get; }

        /// <summary>
        /// Initializes the platform-specific background service.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the initialization.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// <para>
        /// This method is called during application startup to initialize the platform's
        /// background task infrastructure. It may request necessary permissions or
        /// register required system components.
        /// </para>
        /// </remarks>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers a task with the platform's native scheduler.
        /// </summary>
        /// <param name="registration">The task registration information.</param>
        /// <param name="cancellationToken">A token to cancel the registration.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns <see langword="true"/> if registration succeeded; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method translates the generic task registration into platform-specific
        /// API calls. It handles differences in how each platform schedules and manages
        /// background work.
        /// </para>
        /// </remarks>
        Task<bool> RegisterTaskAsync(TaskRegistration registration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unregisters a task from the platform's native scheduler.
        /// </summary>
        /// <param name="identifier">The unique identifier of the task to unregister.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns <see langword="true"/> if the task was found and unregistered; otherwise, <see langword="false"/>.
        /// </returns>
        Task<bool> UnregisterTaskAsync(string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the native platform identifiers for all registered tasks.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns a collection of registered task identifiers.
        /// </returns>
        Task<IEnumerable<string>> GetRegisteredTasksAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Requests the necessary permissions for background task execution.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns <see langword="true"/> if permissions were granted; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Some platforms require explicit user permission for background execution.
        /// This method handles the permission request flow for the current platform.
        /// </para>
        /// </remarks>
        Task<bool> RequestPermissionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether background execution permissions have been granted.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns <see langword="true"/> if permissions are granted; otherwise, <see langword="false"/>.
        /// </returns>
        Task<bool> HasPermissionsAsync(CancellationToken cancellationToken = default);
    }
}