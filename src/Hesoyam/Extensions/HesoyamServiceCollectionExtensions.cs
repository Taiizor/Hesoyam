using Hesoyam.Abstractions;
using Hesoyam.Services;
using Hesoyam.Shared;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Hesoyam.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring Hesoyam background services in the dependency injection container.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use these extension methods in your MAUI application's <c>MauiProgram.cs</c> to register
    /// and configure the Hesoyam background task scheduler.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public static class MauiProgram
    /// {
    ///     public static MauiApp CreateMauiApp()
    ///     {
    ///         var builder = MauiApp.CreateBuilder();
    ///         builder
    ///             .UseMauiApp&lt;App&gt;()
    ///             .UseHesoyam();
    ///             
    ///         // Register your background tasks
    ///         builder.Services.AddTransient&lt;DataSyncTask&gt;();
    ///         builder.Services.AddTransient&lt;CleanupTask&gt;();
    ///         
    ///         return builder.Build();
    ///     }
    /// }
    /// </code>
    /// </example>
    public static class HesoyamServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Hesoyam background task services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the following services:
        /// <list type="bullet">
        ///     <item><description><see cref="ITaskScheduler"/> - The main task scheduler interface</description></item>
        ///     <item><description><see cref="ITaskStorageService"/> - Task registration and state storage</description></item>
        ///     <item><description><see cref="IPlatformBackgroundService"/> - Platform-specific implementation</description></item>
        ///     <item><description><see cref="TaskExecutor"/> - Internal task execution service</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// builder.Services.AddHesoyam();
        /// </code>
        /// </example>
        public static IServiceCollection AddHesoyam(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            // Register core services
            services.TryAddSingleton<ITaskStorageService, DefaultTaskStorageService>();
            services.TryAddSingleton<ITaskScheduler, DefaultTaskScheduler>();
            services.TryAddSingleton<TaskExecutor>();

            // Register platform-specific service
            services.TryAddSingleton<IPlatformBackgroundService>(sp =>
            {
                ILogger<DefaultPlatformBackgroundService> logger = sp.GetRequiredService<ILogger<DefaultPlatformBackgroundService>>();
                return new DefaultPlatformBackgroundService(logger, sp);
            });

            return services;
        }

        /// <summary>
        /// Adds Hesoyam background task services with custom configuration.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configure">An action to configure Hesoyam options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// <para>
        /// Use this overload to provide custom configuration for the background task scheduler.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// builder.Services.AddHesoyam(options =>
        /// {
        ///     options.EnableLogging = true;
        ///     options.DefaultRetryCount = 5;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddHesoyam(this IServiceCollection services, Action<HesoyamOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            // Configure options
            HesoyamOptions options = new();
            configure(options);
            services.AddSingleton(options);

            // Add core services
            services.AddHesoyam();

            return services;
        }

        /// <summary>
        /// Adds a background task to the service collection.
        /// </summary>
        /// <typeparam name="TTask">The type of the background task.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// <para>
        /// Registers the background task with a transient lifetime. Each execution
        /// will receive a fresh instance of the task.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// builder.Services.AddBackgroundTask&lt;DataSyncTask&gt;();
        /// </code>
        /// </example>
        public static IServiceCollection AddBackgroundTask<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTask>(this IServiceCollection services)
            where TTask : class, IBackgroundTask
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddTransient<TTask>();
            services.AddTransient(typeof(IBackgroundTask), typeof(TTask));

            return services;
        }
    }
}