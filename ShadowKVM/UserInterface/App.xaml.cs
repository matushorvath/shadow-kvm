using H.NotifyIcon;
using System.IO;
using System.Windows;
using Serilog;
using Serilog.Core;
using System.Diagnostics;

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
        _logPath = Path.Combine(dataDirectory, "logs", "shadow-kvm-.log");
        var loggingLevelSwitch = new LoggingLevelSwitch();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(loggingLevelSwitch)
            .WriteTo.File(_logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Set up exception logging
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Load the config file
        var configPath = Path.Combine(dataDirectory, "config.yaml");
        var config = LoadConfig(configPath);
        if (config == null)
        {
            return;
        }

        // Set up logging level based on config file
        loggingLevelSwitch.MinimumLevel = config.LogLevel;

        Log.Information("Initializing");

        _notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
        _notifyIcon.ForceCreate();

        _backgroundTask = new BackgroundTask(config);
        _backgroundTask.Start();
    }

    Config? LoadConfig(string configPath)
    {
        try
        {
            return Config.Load(configPath);
        }
        catch (FileNotFoundException)
        {
            var message = "Configuration file not found, create a new one?";
            var result = MessageBox.Show(message, "Shadow KVM", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                Shutdown();
                return null;
            }

            // Create a new config file
            var configText = ConfigGenerator.Generate();
            using (var output = new StreamWriter(configPath))
            {
                output.Write(configText);
            }

            // Edit the config and retry loading
            return EditAndLoadConfig(configPath);
        }
        catch (ConfigFileException exception)
        {
            var message = $"Configuration file is invalid, edit it manually?\n\n"
                + $"{exception.Message}\n\nSee {Path.GetDirectoryName(_logPath)} for details";
            var result = MessageBox.Show(message, "Shadow KVM", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                Shutdown();
                return null;
            }

            // Edit the config and retry loading
            return EditAndLoadConfig(configPath);
        }
    }

    Config EditAndLoadConfig(string configPath)
    {
        // Open notepad to edit the config file and wait for it to close
        var process = Process.Start("notepad.exe", configPath);
        if (process == null)
        {
            throw new Exception("Failed to start notepad");
        }
        process.WaitForExit();

        // Try to load the new config
        var config = Config.Load(configPath);

        MessageBox.Show("Configuration file loaded successfully", "Shadow KVM",
            MessageBoxButton.OK, MessageBoxImage.Information);

        return config;
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
        var error = (args.ExceptionObject as Exception)?.Message ?? args.ExceptionObject.ToString();
        var message = "Shadow KVM encountered an error and needs to close.\n\n"
            + $"{error}\n\nSee {Path.GetDirectoryName(_logPath)} for details";

        MessageBox.Show(message, "Shadow KVM", MessageBoxButton.OK, MessageBoxImage.Error);

        Log.Error("Unhandled exception: {@Exception}", args.ExceptionObject);
    }

    void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        var message = "Shadow KVM encountered an error and needs to close.\n\n"
            + $"{args.Exception.Message}\n\nSee {Path.GetDirectoryName(_logPath)} for details";
        MessageBox.Show(message, "Shadow KVM", MessageBoxButton.OK, MessageBoxImage.Error);

        Log.Error("Unobserved task exception: {@Exception}", args.Exception);
    }

    TaskbarIcon? _notifyIcon;
    BackgroundTask? _backgroundTask;
    string? _logPath;
}
