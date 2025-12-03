# Hesoyam

<p align="center">
  <strong>Cross-Platform Background Task & Service Scheduler for .NET MAUI</strong>
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/Hesoyam">
    <img alt="NuGet Version" src="https://img.shields.io/nuget/v/Hesoyam?style=flat-square&logo=nuget">
  </a>
  <a href="https://www.nuget.org/packages/Hesoyam">
    <img alt="NuGet Downloads" src="https://img.shields.io/nuget/dt/Hesoyam?style=flat-square&logo=nuget">
  </a>
  <a href="https://github.com/Taiizor/Hesoyam/blob/main/LICENSE">
    <img alt="License" src="https://img.shields.io/github/license/Taiizor/Hesoyam?style=flat-square">
  </a>
</p>

---

## 📖 Overview

**Hesoyam** is a powerful, cross-platform background task and service scheduler library for .NET MAUI applications. It provides a unified API for scheduling and managing background tasks that can run even when your application is closed.

### ✨ Key Features

- 🔄 **Cross-Platform Support**: Works on Android, iOS, macOS Catalyst, and Windows
- ⏰ **Flexible Scheduling**: One-shot, periodic, and immediate task execution
- 🔌 **Constraint-Based Execution**: Network, charging, battery, and idle state constraints
- 🔁 **Automatic Retries**: Configurable retry policies with exponential backoff
- 💾 **State Persistence**: Task registrations survive app restarts and device reboots
- 📝 **Rich Logging**: Comprehensive logging for debugging and monitoring
- 💉 **DI Integration**: Full dependency injection support with Microsoft.Extensions.DependencyInjection
- 🚀 **AOT/Trimming Compatible**: Full Native AOT and IL Linker support

---

## 📦 Installation

Install via NuGet Package Manager:

```bash
dotnet add package Hesoyam
```

Or via the Package Manager Console:

```powershell
Install-Package Hesoyam
```

---

## 🚀 Quick Start

### 1. Configure Services

Add Hesoyam to your MAUI application in `MauiProgram.cs`:

```csharp
using Hesoyam.Extensions;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        builder
            .UseMauiApp<App>()
            .UseHesoyam();  // Add Hesoyam services
            
        // Register your background tasks
        builder.Services.AddBackgroundTask<DataSyncTask>();
        builder.Services.AddBackgroundTask<CleanupTask>();
        
        return builder.Build();
    }
}
```

### 2. Create a Background Task

Implement the `IBackgroundTask` interface:

```csharp
using Hesoyam.Abstractions;

public class DataSyncTask : IBackgroundTask
{
    private readonly IDataService _dataService;
    private readonly ILogger<DataSyncTask> _logger;

    public DataSyncTask(IDataService dataService, ILogger<DataSyncTask> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public async Task ExecuteAsync(ITaskContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting data sync for task {TaskId}", context.TaskId);
        
        // Access task metadata
        var userId = context.Configuration.Metadata["userId"];
        
        // Resume from previous state if available
        var lastSyncTime = context.GetState<DateTime?>("lastSyncTime") ?? DateTime.MinValue;
        
        try
        {
            // Perform your background work
            await _dataService.SyncAsync(lastSyncTime, cancellationToken);
            
            // Save progress for next execution
            await context.SetStateAsync("lastSyncTime", DateTime.UtcNow);
            
            _logger.LogInformation("Data sync completed successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Data sync was cancelled");
            throw;
        }
    }
}
```

### 3. Schedule the Task

Use the `ITaskScheduler` to schedule your task:

```csharp
using Hesoyam.Abstractions;
using Hesoyam.Configuration;
using Hesoyam.Enums;

public class MainViewModel
{
    private readonly ITaskScheduler _scheduler;

    public MainViewModel(ITaskScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public async Task ScheduleDataSyncAsync()
    {
        var config = new TaskConfiguration
        {
            Identifier = "data-sync",
            Type = TaskType.Periodic,
            Interval = TimeSpan.FromHours(1),
            Priority = TaskPriority.Normal,
            NetworkRequirement = NetworkType.Connected,
            RequiresCharging = false,
            PersistAcrossReboots = true,
            MaxRetries = 3,
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = "12345"
            },
            Tags = ["sync", "user-data"]
        };

        var success = await _scheduler.ScheduleAsync<DataSyncTask>(config);
        
        if (success)
        {
            Console.WriteLine("Task scheduled successfully!");
        }
    }
}
```

---

## ⚙️ Configuration Options

### TaskConfiguration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Identifier` | `string` | Required | Unique identifier for the task |
| `Type` | `TaskType` | `OneShot` | Scheduling type (OneShot, Periodic, Immediate) |
| `Interval` | `TimeSpan` | 15 minutes | Interval for periodic tasks |
| `InitialDelay` | `TimeSpan` | Zero | Delay before first execution |
| `Priority` | `TaskPriority` | `Normal` | Execution priority level |
| `NetworkRequirement` | `NetworkType` | `None` | Required network connectivity |
| `RequiresCharging` | `bool` | `false` | Require device to be charging |
| `RequiresDeviceIdle` | `bool` | `false` | Require device to be idle |
| `RequiresBatteryNotLow` | `bool` | `false` | Require sufficient battery level |
| `PersistAcrossReboots` | `bool` | `true` | Survive device restarts |
| `MaxRetries` | `int` | 3 | Maximum retry attempts on failure |
| `InitialRetryDelay` | `TimeSpan` | 30 seconds | Delay before first retry |
| `RetryBackoffMultiplier` | `double` | 2.0 | Exponential backoff multiplier |
| `ExecutionTimeout` | `TimeSpan` | 10 minutes | Maximum execution time |
| `Metadata` | `Dictionary<string, string>` | Empty | Custom key-value data |
| `Tags` | `List<string>` | Empty | Task categorization tags |

---

## 📱 Platform-Specific Notes

### Android

- Uses **WorkManager** for background execution
- Minimum interval for periodic tasks: **15 minutes**
- Requires `RECEIVE_BOOT_COMPLETED` permission for reboot persistence
- All constraints (network, charging, idle, battery) are fully supported

### iOS

- Uses **BGTaskScheduler** (Background App Refresh & Processing)
- Limited to approximately **30 seconds** of background execution time
- Requires `BGTaskSchedulerPermittedIdentifiers` in `Info.plist`
- Requires Background Modes capability (Background fetch, Background processing)
- Scheduling is at the discretion of the system

### macOS Catalyst

- Uses **BGTaskScheduler**
- More lenient timing compared to iOS
- Requires Background Modes capability
- Power-aware execution respects battery state

### Windows

- Uses **Windows Background Tasks API**
- Minimum interval for periodic tasks: **15 minutes**
- Requires Background Tasks capability in package manifest
- Supports time triggers and system conditions

---

## 🔧 Advanced Usage

### Task State Management

```csharp
public async Task ExecuteAsync(ITaskContext context, CancellationToken cancellationToken)
{
    // Retrieve previous state
    var lastProcessedId = context.GetState<int>("lastProcessedId");
    var totalProcessed = context.GetState<int>("totalProcessed");
    
    // Process items
    foreach (var item in items.Skip(lastProcessedId))
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        await ProcessItemAsync(item);
        
        // Save checkpoint periodically
        await context.SetStateAsync("lastProcessedId", item.Id);
        await context.SetStateAsync("totalProcessed", ++totalProcessed);
        
        // Report progress
        await context.ReportProgressAsync(totalProcessed, items.Count);
    }
    
    // Clear state on completion
    await context.ClearStateAsync();
}
```

### Cancelling Tasks

```csharp
// Cancel a specific task
await scheduler.CancelAsync("data-sync");

// Cancel all tasks with a specific tag
await scheduler.CancelByTagAsync("sync");

// Cancel all tasks
await scheduler.CancelAllAsync();
```

### Checking Task Status

```csharp
// Check if a task is scheduled
var isScheduled = await scheduler.IsScheduledAsync("data-sync");

// Get task status
var status = await scheduler.GetStatusAsync("data-sync");

// Get task configuration
var config = await scheduler.GetConfigurationAsync("data-sync");

// List all scheduled tasks
var tasks = await scheduler.GetScheduledTasksAsync();
```

### Custom Options

```csharp
builder.Services.AddHesoyam(options =>
{
    options.EnableLogging = true;
    options.DefaultRetryCount = 5;
    options.DefaultRetryDelay = TimeSpan.FromMinutes(1);
    options.DefaultExecutionTimeout = TimeSpan.FromMinutes(5);
    options.AutoRestoreTasks = true;
});
```

---

## 📋 Requirements

- .NET 9.0 or .NET 10.0
- .NET MAUI
- Platform-specific requirements:
  - Android: API 21+ (Android 5.0)
  - iOS: 15.0+
  - macOS Catalyst: 15.0+
  - Windows: 10.0.17763.0+

---

## 🏗️ Project Structure

```
src/Hesoyam/
├── Abstractions/           # Interfaces and contracts
│   ├── IBackgroundTask.cs
│   ├── ITaskScheduler.cs
│   ├── ITaskContext.cs
│   ├── IPlatformBackgroundService.cs
│   ├── ITaskStorageService.cs
│   └── TaskRegistration.cs
├── Configuration/          # Task configuration
│   └── TaskConfiguration.cs
├── Enums/                  # Enumerations
│   ├── TaskType.cs
│   ├── TaskPriority.cs
│   ├── NetworkType.cs
│   └── TaskExecutionStatus.cs
├── Services/               # Core service implementations
│   ├── DefaultTaskScheduler.cs
│   ├── DefaultTaskContext.cs
│   ├── DefaultTaskStorageService.cs
│   ├── TaskExecutor.cs
│   └── HesoyamJsonContext.cs
├── Platforms/              # Platform-specific implementations
│   ├── Android/
│   ├── iOS/
│   ├── MacCatalyst/
│   ├── Windows/
│   └── Shared/
└── Extensions/             # DI and configuration extensions
    ├── HesoyamServiceCollectionExtensions.cs
    ├── HesoyamMauiExtensions.cs
    └── HesoyamOptions.cs
```

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/Taiizor/Hesoyam/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Hesoyam/discussions)

---

<p align="center">
  Made with ❤️ by <a href="https://github.com/Taiizor">Taiizor</a>
</p>