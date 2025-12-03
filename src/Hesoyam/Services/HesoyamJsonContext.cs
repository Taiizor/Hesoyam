using Hesoyam.Configuration;
using System.Text.Json.Serialization;

namespace Hesoyam.Services
{
    /// <summary>
    /// JSON source generator context for AOT-compatible serialization in Hesoyam.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This context provides compile-time generated serialization code for all types
    /// used in Hesoyam's persistence layer, enabling native AOT compilation and
    /// trimming compatibility.
    /// </para>
    /// <para>
    /// <strong>Included Types:</strong>
    /// <list type="bullet">
    ///     <item><description><see cref="StoredRegistration"/> - Task registration persistence</description></item>
    ///     <item><description><see cref="TaskConfiguration"/> - Task configuration data</description></item>
    ///     <item><description>Dictionary types for registrations and state storage</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(StoredRegistration))]
    [JsonSerializable(typeof(TaskConfiguration))]
    [JsonSerializable(typeof(Dictionary<string, StoredRegistration>))]
    [JsonSerializable(typeof(Dictionary<string, Dictionary<string, string>>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(DateTime))]
    [JsonSerializable(typeof(DateTimeOffset))]
    [JsonSerializable(typeof(TimeSpan))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(WindowsTaskData))]
    internal sealed partial class HesoyamJsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// Represents a serializable task registration for persistent storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is used internally by <see cref="DefaultTaskStorageService"/> to
    /// persist task registrations to platform storage in an AOT-compatible manner.
    /// </para>
    /// </remarks>
    internal sealed class StoredRegistration
    {
        /// <summary>
        /// Gets or sets the assembly-qualified name of the task type.
        /// </summary>
        /// <value>
        /// The full type name including assembly information.
        /// </value>
        public required string TaskTypeName { get; init; }

        /// <summary>
        /// Gets or sets the task configuration.
        /// </summary>
        /// <value>
        /// The complete task configuration including scheduling and constraints.
        /// </value>
        public required TaskConfiguration Configuration { get; init; }

        /// <summary>
        /// Gets or sets the timestamp when the registration was created.
        /// </summary>
        /// <value>
        /// The UTC timestamp of registration creation.
        /// </value>
        public DateTimeOffset CreatedAt { get; init; }
    }

    /// <summary>
    /// Represents task data stored for Windows background task execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is used to persist task registration data in a platform-agnostic way,
    /// allowing background tasks to retrieve task information during execution.
    /// </para>
    /// </remarks>
    internal sealed class WindowsTaskData
    {
        /// <summary>
        /// Gets or sets the assembly-qualified name of the task type.
        /// </summary>
        /// <value>
        /// The full type name including assembly information.
        /// </value>
        public required string TaskTypeName { get; init; }

        /// <summary>
        /// Gets or sets the task identifier.
        /// </summary>
        /// <value>
        /// The unique identifier for the task.
        /// </value>
        public required string Identifier { get; init; }
    }
}