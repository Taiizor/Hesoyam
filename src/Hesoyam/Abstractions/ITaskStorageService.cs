using System.Diagnostics.CodeAnalysis;

namespace Hesoyam.Abstractions
{
    /// <summary>
    /// Defines the contract for persisting task registrations and state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The task storage service handles persistence of:
    /// <list type="bullet">
    ///     <item><description>Task registrations for reboot persistence</description></item>
    ///     <item><description>Task execution state for resumable tasks</description></item>
    ///     <item><description>Task status and history</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The default implementation uses platform-specific secure storage mechanisms.
    /// Custom implementations can be provided for specialized storage requirements.
    /// </para>
    /// </remarks>
    /// <seealso cref="TaskRegistration"/>
    public interface ITaskStorageService
    {
        /// <summary>
        /// Saves a task registration to persistent storage.
        /// </summary>
        /// <param name="registration">The task registration to save.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// <para>
        /// If a registration with the same identifier already exists, it will be replaced.
        /// </para>
        /// </remarks>
        Task SaveRegistrationAsync(TaskRegistration registration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a task registration from storage.
        /// </summary>
        /// <param name="identifier">The unique identifier of the task.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns the <see cref="TaskRegistration"/> if found; otherwise, <see langword="null"/>.
        /// </returns>
        Task<TaskRegistration?> GetRegistrationAsync(string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all task registrations from storage.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns a collection of all stored task registrations.
        /// </returns>
        Task<IEnumerable<TaskRegistration>> GetAllRegistrationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a task registration from storage.
        /// </summary>
        /// <param name="identifier">The unique identifier of the task to remove.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns <see langword="true"/> if the registration was found and removed; otherwise, <see langword="false"/>.
        /// </returns>
        Task<bool> RemoveRegistrationAsync(string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves task state to persistent storage.
        /// </summary>
        /// <typeparam name="T">The type of the state value.</typeparam>
        /// <param name="taskIdentifier">The identifier of the task.</param>
        /// <param name="key">The key identifying the state value.</param>
        /// <param name="value">The value to store.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SaveStateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string taskIdentifier, string key, T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves task state from storage.
        /// </summary>
        /// <typeparam name="T">The type of the state value.</typeparam>
        /// <param name="taskIdentifier">The identifier of the task.</param>
        /// <param name="key">The key identifying the state value.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// Returns the stored value if found; otherwise, the default value for <typeparamref name="T"/>.
        /// </returns>
        Task<T?> GetStateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string taskIdentifier, string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all state for a specific task.
        /// </summary>
        /// <param name="taskIdentifier">The identifier of the task.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ClearStateAsync(string taskIdentifier, CancellationToken cancellationToken = default);
    }
}