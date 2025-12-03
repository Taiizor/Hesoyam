# Changelog

All notable changes to Hesoyam will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.1] - 2025-12-03

### Added

- **Cross-Platform Background Task Scheduling**: Unified API for scheduling background tasks across Android, iOS, macOS Catalyst, and Windows platforms
- **Platform-Specific Implementations**:
  - **Android**: WorkManager integration with periodic/one-time work, constraints, and exponential backoff
  - **iOS**: BGTaskScheduler integration with app refresh and background processing tasks
  - **macOS Catalyst**: BGTaskScheduler integration with QoS mapping
  - **Windows**: BackgroundTaskBuilder integration with time triggers and system conditions
- **Task Types**: Support for OneShot, Periodic, and Immediate task execution
- **Constraint-Based Execution**: Configure tasks with network, charging, battery level, and device idle requirements
- **Automatic Retry Logic**: Configurable retry policies with exponential backoff (MaxRetries, InitialRetryDelay, RetryBackoffMultiplier)
- **State Persistence**: Task registrations and state survive app restarts and device reboots
- **Task Context API**: Access task metadata, save/restore state, and report progress during execution
- **Dependency Injection Integration**: Full support for Microsoft.Extensions.DependencyInjection
- **AOT/Trimming Compatibility**: Full Native AOT and IL Linker support with source-generated JSON serialization
- **Comprehensive XML Documentation**: All public APIs are fully documented

### Platform Support

- .NET 9.0: Android 35.0, iOS 18.0, macOS Catalyst 18.0, Windows 10.0.19041.0
- .NET 10.0: Android 36.0, iOS 26.0, macOS Catalyst 26.0, Windows 10.0.19041.0

### API

- `IBackgroundTask`: Interface for implementing background task logic
- `ITaskScheduler`: Schedule, cancel, and monitor background tasks
- `ITaskContext`: Execution context with state persistence and progress reporting
- `TaskConfiguration`: Configure task scheduling, constraints, retry policies, and metadata
- `AddHesoyam()`: Register Hesoyam services with IServiceCollection
- `UseHesoyam()`: Configure Hesoyam with MauiAppBuilder
- `AddBackgroundTask<T>()`: Register background task implementations

[1.0.1]: https://github.com/Taiizor/Hesoyam/releases/tag/v1.0.1