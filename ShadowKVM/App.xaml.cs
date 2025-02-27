using H.NotifyIcon;
using System.IO;
using System.Windows;
using Serilog;
using Serilog.Core;

namespace ShadowKVM;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Set up data directory
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dataDirectory = Path.Combine(appData, "Shadow KVM");
        Directory.CreateDirectory(dataDirectory);

        // Set up logger
        var logPath = Path.Combine(dataDirectory, "logs", "shadow-kvm-.log");
        var loggingLevelSwitch = new LoggingLevelSwitch();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(loggingLevelSwitch)
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Set up exception logging
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Load config and possibly adjust the log level
        var config = Config.Load(dataDirectory);
        loggingLevelSwitch.MinimumLevel = config.LogLevel;

        Log.Information("Initializing");

        _notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
        _notifyIcon.ForceCreate();

        _backgroundTask = new BackgroundTask(config);
        _backgroundTask.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Shutting down");

        _backgroundTask?.Dispose();
        _notifyIcon?.Dispose();

        base.OnExit(e);
    }

    void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        Log.Error("Unhandled exception: {@Exception}", args.ExceptionObject);
    }

    void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        Log.Error("Unobserved task exception: {@Exception}", args.Exception);
    }

    TaskbarIcon? _notifyIcon;
    BackgroundTask? _backgroundTask;
}
