# Hesoyam

A cross-platform background task scheduler for .NET MAUI applications. Hesoyam enables you to schedule and execute background tasks that run even when your application is closed.

## Features

- **Cross-Platform Support**: Works on Android, iOS, macOS Catalyst, and Windows
- **Persistent Tasks**: Tasks survive app restarts and device reboots
- **Flexible Scheduling**: One-shot, periodic, and immediate task execution
- **Constraints**: Network, charging, idle, and battery level requirements
- **Retry Logic**: Automatic retry with exponential backoff
- **State Persistence**: Save and restore task state across executions
- **Progress Reporting**: Track task progress with numeric or message-based updates
- **AOT/Trimming Compatible**: Full Native AOT and IL Linker support

## Platform Implementations

| Platform | Technology | Minimum Version |
|----------|------------|-----------------|
| Android | WorkManager | API 21 (Android 5.0) |
| iOS | BGTaskScheduler | iOS 15.0 |
| macOS Catalyst | BGTaskScheduler | macOS 15.0 |
| Windows | BackgroundTaskBuilder | Windows 10 1809 |

## Installation

```bash
dotnet add package Hesoyam
```

## Quick Start

### 1. Register Services

```csharp
// MauiProgram.cs
var builder = MauiApp.CreateBuilder();
builder
    .UseMauiApp<App>()
    .UseHesoyam();

// Register your background tasks
builder.Services.AddBackgroundTask<DataSyncTask>();
```

### 2. Create a Background Task

```csharp
public class DataSyncTask : IBackgroundTask
{
    private readonly IDataService _dataService;
    
    public DataSyncTask(IDataService dataService)
    {
        _dataService = dataService;
    }
    
    public async Task ExecuteAsync(ITaskContext context, CancellationToken cancellationToken)
    {
        // Report progress
        await context.ReportProgressAsync("Starting sync...");
        
        // Do your work
        await _dataService.SyncAsync(cancellationToken);
        
        // Save state for resumable tasks
        await context.SetStateAsync("lastSync", DateTime.UtcNow);
    }
}
```

### 3. Schedule the Task

```csharp
var scheduler = services.GetRequiredService<ITaskScheduler>();

var config = new TaskConfiguration
{
    Identifier = "data-sync",
    Type = TaskType.Periodic,
    Interval = TimeSpan.FromHours(1),
    NetworkRequirement = NetworkType.Connected,
    PersistAcrossReboots = true
};

await scheduler.ScheduleAsync<DataSyncTask>(config);
```

## Task Configuration

```csharp
var config = new TaskConfiguration
{
    // Required: Unique task identifier
    Identifier = "my-task",
    
    // Scheduling
    Type = TaskType.Periodic,           // OneShot, Periodic, Immediate
    Interval = TimeSpan.FromHours(1),   // For periodic tasks
    InitialDelay = TimeSpan.FromMinutes(5),
    
    // Constraints
    NetworkRequirement = NetworkType.Connected,  // None, Connected, Unmetered, WiFi
    RequiresCharging = false,
    RequiresDeviceIdle = false,
    RequiresBatteryNotLow = false,
    
    // Retry Policy
    MaxRetries = 3,
    InitialRetryDelay = TimeSpan.FromSeconds(30),
    RetryBackoffMultiplier = 2.0,
    
    // Other
    ExecutionTimeout = TimeSpan.FromMinutes(10),
    PersistAcrossReboots = true,
    Tags = ["sync", "user-data"],
    Metadata = new() { ["userId"] = "12345" }
};
```

## Managing Tasks

```csharp
// Check if scheduled
bool isScheduled = await scheduler.IsScheduledAsync("data-sync");

// Get status
TaskExecutionStatus? status = await scheduler.GetStatusAsync("data-sync");

// Cancel by identifier
await scheduler.CancelAsync("data-sync");

// Cancel by tag
int cancelled = await scheduler.CancelByTagAsync("sync");

// Cancel all
int total = await scheduler.CancelAllAsync();

// Get all scheduled tasks
IReadOnlyList<string> tasks = await scheduler.GetScheduledTasksAsync();
```

## Platform-Specific Notes

### Android
- Minimum interval for periodic tasks is 15 minutes (WorkManager limitation)
- Add `RECEIVE_BOOT_COMPLETED` permission for reboot persistence
- WorkManager handles constraints automatically

### iOS
- Background tasks limited to ~30 seconds execution time
- Add task identifiers to `BGTaskSchedulerPermittedIdentifiers` in Info.plist
- Enable Background Modes capability (Background fetch, Background processing)

### macOS Catalyst
- Uses BGTaskScheduler API
- Enable Background Modes capability
- Power-aware execution respects battery state

### Windows
- Minimum interval for time triggers is 15 minutes
- Add Background Tasks capability to package manifest
- CPU and network quotas apply

## Requirements

- .NET 9.0 or .NET 10.0
- .NET MAUI
- Platform-specific:
  - Android: API 21+
  - iOS: 15.0+
  - macOS Catalyst: 15.0+
  - Windows: 10.0.17763.0+

## License

MIT License - see LICENSE file for details.
