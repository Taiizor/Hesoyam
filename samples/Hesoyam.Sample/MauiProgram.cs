using Hesoyam.Extensions;
using Hesoyam.Sample.Services;
using Hesoyam.Sample.Tasks;
using Hesoyam.Sample.ViewModels;
using Microsoft.Extensions.Logging;

namespace Hesoyam.Sample
{
    /// <summary>
    /// The main entry point for the MAUI application.
    /// </summary>
    public static class MauiProgram
    {
        /// <summary>
        /// Creates and configures the MAUI application.
        /// </summary>
        /// <returns>The configured <see cref="MauiApp"/> instance.</returns>
        public static MauiApp CreateMauiApp()
        {
            MauiAppBuilder builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseHesoyam(options =>
                {
                    // Configure Hesoyam options
                    options.EnableLogging = true;
                    options.DefaultRetryCount = 3;
                    options.AutoRestoreTasks = true;
                    options.DefaultRetryDelay = TimeSpan.FromSeconds(30);
                    options.DefaultExecutionTimeout = TimeSpan.FromMinutes(5);
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register application services
            builder.Services.AddSingleton<INotificationService, NotificationService>();
            builder.Services.AddSingleton<IDataSyncService, DataSyncService>();

            // Register background tasks
            builder.Services.AddBackgroundTask<NotificationTask>();
            builder.Services.AddBackgroundTask<DataSyncTask>();
            builder.Services.AddBackgroundTask<CleanupTask>();

            // Register ViewModels
            builder.Services.AddTransient<MainViewModel>();

            // Register Pages
            builder.Services.AddTransient<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}