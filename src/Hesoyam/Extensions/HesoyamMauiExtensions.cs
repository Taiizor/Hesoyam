using Hesoyam.Abstractions;
using Hesoyam.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

#if ANDROID
using Hesoyam.Platforms.Android;
#elif IOS
using Hesoyam.Platforms.iOS;
#elif MACCATALYST
using Hesoyam.Platforms.MacCatalyst;
#elif WINDOWS
using Hesoyam.Platforms.Windows;
#endif

namespace Hesoyam.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring Hesoyam with <see cref="MauiAppBuilder"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extension methods provide a fluent API for integrating Hesoyam into your MAUI application.
    /// </para>
    /// </remarks>
    public static class HesoyamMauiAppBuilderExtensions
    {
        /// <summary>
        /// Configures the MAUI application to use Hesoyam background services.
        /// </summary>
        /// <param name="builder">The <see cref="MauiAppBuilder"/> to configure.</param>
        /// <returns>The <see cref="MauiAppBuilder"/> so that additional calls can be chained.</returns>
        /// <remarks>
        /// <para>
        /// This method registers all Hesoyam services and performs any necessary platform-specific initialization.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var builder = MauiApp.CreateBuilder();
        /// builder
        ///     .UseMauiApp&lt;App&gt;()
        ///     .UseHesoyam();
        /// </code>
        /// </example>
        public static MauiAppBuilder UseHesoyam(this MauiAppBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            // Add preferences provider - use file-based provider for Windows to support unpackaged apps
#if WINDOWS
            builder.Services.TryAddSingleton<IPreferencesProvider, WindowsFilePreferencesProvider>();
#else
            builder.Services.TryAddSingleton<IPreferencesProvider, MauiPreferencesProvider>();
#endif

            // Add core services with MAUI preferences
            builder.Services.TryAddSingleton<ITaskStorageService>(sp =>
                new DefaultTaskStorageService(
                    sp.GetRequiredService<ILogger<DefaultTaskStorageService>>(),
                    sp.GetRequiredService<IPreferencesProvider>()));

            builder.Services.TryAddSingleton<ITaskScheduler, DefaultTaskScheduler>();
            builder.Services.TryAddSingleton<TaskExecutor>();

            // Register platform-specific service
            builder.Services.TryAddSingleton<IPlatformBackgroundService>(sp =>
            {
#if ANDROID
                return new AndroidBackgroundService(
                    sp.GetRequiredService<ILogger<AndroidBackgroundService>>(),
                    sp);
#elif IOS
                return new iOSBackgroundService(
                    sp.GetRequiredService<ILogger<iOSBackgroundService>>(),
                    sp);
#elif MACCATALYST
                return new MacCatalystBackgroundService(
                    sp.GetRequiredService<ILogger<MacCatalystBackgroundService>>(),
                    sp);
#elif WINDOWS
                return new WindowsBackgroundService(
                    sp.GetRequiredService<ILogger<WindowsBackgroundService>>(),
                    sp);
#else
                var logger = sp.GetRequiredService<ILogger<DefaultPlatformBackgroundService>>();
                return new DefaultPlatformBackgroundService(logger, sp);
#endif
            });

            // Configure lifecycle events for initialization
            builder.ConfigureLifecycleEvents(lifecycle =>
            {
#if ANDROID
                lifecycle.AddAndroid(android =>
                {
                    android.OnCreate((activity, bundle) =>
                    {
                        // Initialize is handled automatically by WorkManager
                    });
                });
#elif IOS || MACCATALYST
                lifecycle.AddiOS(ios =>
                {
                    ios.FinishedLaunching((app, options) =>
                    {
                        // BGTaskScheduler registration should be done here
                        return true;
                    });
                });
#elif WINDOWS
                lifecycle.AddWindows(windows =>
                {
                    windows.OnLaunched((app, args) =>
                    {
                        // Initialize background task registration
                    });
                });
#endif
            });

            return builder;
        }

        /// <summary>
        /// Configures the MAUI application to use Hesoyam with custom options.
        /// </summary>
        /// <param name="builder">The <see cref="MauiAppBuilder"/> to configure.</param>
        /// <param name="configure">An action to configure Hesoyam options.</param>
        /// <returns>The <see cref="MauiAppBuilder"/> so that additional calls can be chained.</returns>
        /// <example>
        /// <code>
        /// builder.UseHesoyam(options =>
        /// {
        ///     options.EnableLogging = true;
        /// });
        /// </code>
        /// </example>
        public static MauiAppBuilder UseHesoyam(this MauiAppBuilder builder, Action<HesoyamOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configure);

            // Configure options
            HesoyamOptions options = new();
            configure(options);
            builder.Services.AddSingleton(options);

            // Add Hesoyam services
            builder.UseHesoyam();

            return builder;
        }
    }

    /// <summary>
    /// MAUI Preferences provider that uses the platform Preferences API.
    /// </summary>
    internal sealed class MauiPreferencesProvider : IPreferencesProvider
    {
        /// <inheritdoc />
        public string Get(string key, string defaultValue)
        {
            return Preferences.Get(key, defaultValue);
        }

        /// <inheritdoc />
        public void Set(string key, string value)
        {
            Preferences.Set(key, value);
        }
    }
}