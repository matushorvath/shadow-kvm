using H.NotifyIcon;
using System.IO;
using System.Windows;
using Serilog;
using Serilog.Core;
using System.Diagnostics;

namespace ShadowKVM;

public partial class App : Application
{
    public App()
    {
        // Set up data directory
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _dataDirectory = Path.Combine(appData, "Shadow KVM");

        _configPath = Path.Combine(_dataDirectory, "config.yaml");
        _logPath = Path.Combine(_dataDirectory, "logs", "shadow-kvm-.log");

        _loggingLevelSwitch = new LoggingLevelSwitch();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Directory.CreateDirectory(_dataDirectory);

        InitLogger();

        Log.Information("Initializing");

        // Enable autostart if this is the first time we run for this user
        // Needs to happen before initializing the notify icon
        if (!Autostart.IsConfigured())
        {
            Autostart.SetEnabled(true);
        }

        _hiddenWindow = new HiddenWindow();
        _hiddenWindow.Create();

        _notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
        _notifyIcon.ForceCreate();

        InitConfig();
    }

    void InitLogger()
    {
        // Set up logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(_loggingLevelSwitch)
            .WriteTo.File(_logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Set up exception logging
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    void InitConfig()
    {
        try
        {
            ReloadConfig();
        }
        catch (FileNotFoundException)
        {
            var message = "Configuration file not found, create a new one?";
            var result = MessageBox.Show(message, "Shadow KVM", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                Shutdown();
                return;
            }

            // Create a new config file
            GenerateConfigWithProgress();

            var viewModel = (NotifyIconViewModel)_notifyIcon!.DataContext;
            viewModel.ConfigureCommand.Execute(null);
        }
        catch (ConfigFileException exception)
        {
            var message = $"Configuration file is invalid, edit it manually?\n\n"
                + $"{exception.Message}\n\nSee {Path.GetDirectoryName(_logPath)} for details";
            var result = MessageBox.Show(message, "Shadow KVM", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                Shutdown();
                return;
            }

            var viewModel = (NotifyIconViewModel)_notifyIcon!.DataContext;
            viewModel.ConfigureCommand.Execute(null);
        }
    }

    void GenerateConfigWithProgress()
    {
        ConfigGeneratorWindow.Execute(progress =>
        {
            var configText = ConfigGenerator.Generate(progress);
            using (var output = new StreamWriter(_configPath))
            {
                output.Write(configText);
            }
        });
    }

    public async Task EditConfig()
    {
        // Open notepad to edit the config file and wait for it to close
        var process = Process.Start("notepad.exe", _configPath);
        if (process == null)
        {
            throw new Exception("Failed to start notepad");
        }

        await process.WaitForExitAsync();
    }

    public void ReloadConfig(bool message = false)
    {
        if (_config != null && !_config.HasChanged(_configPath))
        {
            Log.Information("Configuration file has not changed, skipping reload");
            return;
        }

        _config = Config.Load(_configPath);

        if (message)
        {
            MessageBox.Show("Configuration file loaded successfully", "Shadow KVM",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Set up logging level based on config file
        _loggingLevelSwitch.MinimumLevel = _config.LogLevel;

        // Restart the background task
        if (_backgroundTask != null)
        {
            _backgroundTask.Dispose();
            _backgroundTask = null;
        }

        _backgroundTask = new BackgroundTask(_config);
        _backgroundTask.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Shutting down");

        _backgroundTask?.Dispose();
        _notifyIcon?.Dispose();

        _hiddenWindow?.Dispose();

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

    string _dataDirectory;
    string _configPath;
    string _logPath;

    Config? _config;
    LoggingLevelSwitch _loggingLevelSwitch;

    HiddenWindow? _hiddenWindow;
}
