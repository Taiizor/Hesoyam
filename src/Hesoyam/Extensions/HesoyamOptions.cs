using Hesoyam.Configuration;

namespace Hesoyam.Extensions
{
    /// <summary>
    /// Configuration options for the Hesoyam background task scheduler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this class to customize the behavior of the background task scheduler
    /// when registering it with the dependency injection container.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddHesoyam(options =>
    /// {
    ///     options.EnableLogging = true;
    ///     options.DefaultRetryCount = 5;
    ///     options.DefaultRetryDelay = TimeSpan.FromMinutes(1);
    /// });
    /// </code>
    /// </example>
    public sealed class HesoyamOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether detailed logging is enabled.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to enable detailed logging; otherwise, <see langword="false"/>.
        /// Defaults to <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// <para>
        /// When enabled, additional debug information will be logged during task
        /// scheduling and execution. Useful for troubleshooting issues.
        /// </para>
        /// </remarks>
        public bool EnableLogging { get; set; }

        /// <summary>
        /// Gets or sets the default number of retry attempts for failed tasks.
        /// </summary>
        /// <value>
        /// The default number of retries. Defaults to 3.
        /// </value>
        /// <remarks>
        /// <para>
        /// This value is used when <see cref="TaskConfiguration.MaxRetries"/>
        /// is not explicitly set.
        /// </para>
        /// </remarks>
        public int DefaultRetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets the default delay between retry attempts.
        /// </summary>
        /// <value>
        /// The default retry delay. Defaults to 30 seconds.
        /// </value>
        /// <remarks>
        /// <para>
        /// This value is used when <see cref="TaskConfiguration.InitialRetryDelay"/>
        /// is not explicitly set.
        /// </para>
        /// </remarks>
        public TimeSpan DefaultRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the default execution timeout for tasks.
        /// </summary>
        /// <value>
        /// The default execution timeout. Defaults to 10 minutes.
        /// </value>
        /// <remarks>
        /// <para>
        /// This value is used when <see cref="TaskConfiguration.ExecutionTimeout"/>
        /// is not explicitly set.
        /// </para>
        /// <para>
        /// <strong>Note:</strong> Platform-specific limits may override this value.
        /// </para>
        /// </remarks>
        public TimeSpan DefaultExecutionTimeout { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets or sets a value indicating whether to automatically reschedule tasks after app startup.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to automatically reschedule persisted tasks; otherwise, <see langword="false"/>.
        /// Defaults to <see langword="true"/>.
        /// </value>
        /// <remarks>
        /// <para>
        /// When enabled, tasks that were scheduled before the app was closed will be
        /// automatically restored and rescheduled when the app starts.
        /// </para>
        /// </remarks>
        public bool AutoRestoreTasks { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to request battery optimization exemption.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to request battery optimization exemption; otherwise, <see langword="false"/>.
        /// Defaults to <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// <para>
        /// On some platforms (notably Android), requesting battery optimization exemption
        /// allows background tasks to run more reliably. However, this may require
        /// additional user consent and could affect app store approval.
        /// </para>
        /// </remarks>
        public bool RequestBatteryOptimizationExemption { get; set; }
    }
}