namespace Hesoyam.Enums
{
    /// <summary>
    /// Defines the network requirements for background task execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use network constraints to ensure that network-dependent tasks only run
    /// when appropriate network conditions are met. This helps conserve battery
    /// and prevents tasks from failing due to network unavailability.
    /// </para>
    /// <para>
    /// <strong>Platform Notes:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>Android: Fully supported through JobScheduler constraints.</description>
    ///     </item>
    ///     <item>
    ///         <description>iOS: Limited support; tasks may need to check connectivity manually.</description>
    ///     </item>
    ///     <item>
    ///         <description>Windows: Supported through SystemConditionType.</description>
    ///     </item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new TaskConfiguration
    /// {
    ///     NetworkRequirement = NetworkType.Unmetered,
    ///     Identifier = "large-download"
    /// };
    /// </code>
    /// </example>
    public enum NetworkType
    {
        /// <summary>
        /// No network requirement. Task can run without network connectivity.
        /// </summary>
        /// <remarks>
        /// Use this for tasks that do not require network access or can handle
        /// offline scenarios gracefully.
        /// </remarks>
        None = 0,

        /// <summary>
        /// Requires any network connectivity. Task will run on cellular or Wi-Fi.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this for tasks that require network access but can use any type
        /// of connection including cellular data.
        /// </para>
        /// <para>
        /// Be mindful of data usage when using this constraint, as it allows
        /// execution on metered connections.
        /// </para>
        /// </remarks>
        Connected = 1,

        /// <summary>
        /// Requires an unmetered network connection (typically Wi-Fi).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this for tasks that transfer large amounts of data and should not
        /// consume the user's cellular data allowance.
        /// </para>
        /// <para>
        /// This constraint helps prevent unexpected data charges for users.
        /// </para>
        /// </remarks>
        Unmetered = 2,

        /// <summary>
        /// Requires Wi-Fi connectivity specifically.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this for tasks that specifically need Wi-Fi, for example when
        /// a stable, high-bandwidth connection is required.
        /// </para>
        /// <para>
        /// Note that not all platforms distinguish between unmetered and Wi-Fi connections.
        /// </para>
        /// </remarks>
        WiFi = 3
    }
}