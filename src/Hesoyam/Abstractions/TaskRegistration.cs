using Hesoyam.Configuration;

namespace Hesoyam.Abstractions
{
    /// <summary>
    /// Represents a task registration containing all information needed to schedule a background task.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class combines the task type with its configuration for registration with
    /// the platform-specific background service. It is used internally by the scheduler
    /// to pass complete task information to platform implementations.
    /// </para>
    /// </remarks>
    /// <seealso cref="TaskConfiguration"/>
    /// <seealso cref="IBackgroundTask"/>
    public sealed class TaskRegistration
    {
        /// <summary>
        /// Gets or sets the type of the background task to execute.
        /// </summary>
        /// <value>
        /// The <see cref="Type"/> that implements <see cref="IBackgroundTask"/>.
        /// </value>
        /// <remarks>
        /// <para>
        /// The task type must be registered in the dependency injection container.
        /// When the task is executed, the scheduler will resolve an instance from
        /// the service provider.
        /// </para>
        /// </remarks>
        public required Type TaskType { get; init; }

        /// <summary>
        /// Gets or sets the configuration for the task.
        /// </summary>
        /// <value>
        /// The <see cref="TaskConfiguration"/> defining scheduling behavior and constraints.
        /// </value>
        public required TaskConfiguration Configuration { get; init; }

        /// <summary>
        /// Gets or sets the timestamp when this registration was created.
        /// </summary>
        /// <value>
        /// A <see cref="DateTimeOffset"/> representing the registration time.
        /// </value>
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets the fully qualified type name of the task.
        /// </summary>
        /// <value>
        /// The assembly-qualified name of the task type.
        /// </value>
        /// <remarks>
        /// <para>
        /// This property is used for serialization and persistence of task registrations.
        /// </para>
        /// </remarks>
        public string TaskTypeName => TaskType.AssemblyQualifiedName ?? TaskType.FullName ?? TaskType.Name;

        /// <summary>
        /// Gets the unique identifier from the configuration.
        /// </summary>
        /// <value>
        /// The task identifier from <see cref="TaskConfiguration.Identifier"/>.
        /// </value>
        public string Identifier => Configuration.Identifier;
    }
}