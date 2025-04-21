using System.IO;
using System.Windows;
using H.NotifyIcon;
using Serilog;
using Serilog.Core;

namespace ShadowKVM;

// TODO make testable, write unit tests

public interface IAppControl
{
    void Shutdown();
}

public partial class App : Application, IAppControl
{
    public App()
    {
        // Set up data directory
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _dataDirectory = Path.Combine(appData, "Shadow KVM");

        _logPath = Path.Combine(_dataDirectory, "logs", "shadow-kvm-.log");
        _loggingLevelSwitch = new();

        Services = new(_dataDirectory);

        Startup += OnStartupAsync;
    }

    async void OnStartupAsync(object sender, StartupEventArgs e)
    {
        Directory.CreateDirectory(_dataDirectory);

        InitLogger();

        Log.Information("Initializing, version {FullSemVer} ({CommitDate})",
            GitVersionInformation.FullSemVer, GitVersionInformation.CommitDate);

        // Enable autostart if this is the first time we run for this user
        // Needs to happen before initializing the notify icon
        if (!Services.Autostart.IsConfigured())
        {
            Services.Autostart.SetEnabled(true);
        }

        _hiddenWindow = new();
        _hiddenWindow.Create();

        _notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
        _notifyIcon.ForceCreate();

        await InitConfig();

        // Debug log can only be enabled after loading config
        Log.Debug("Version: {InformationalVersion}", GitVersionInformation.InformationalVersion);
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

    async Task InitConfig()
    {
        // Reinitialize whenever the config file is changed
        Services.ConfigService.ConfigChanged += (configService) =>
        {
            // Set up logging level based on config file
            _loggingLevelSwitch.MinimumLevel = configService.Config.LogLevel;

            Services.BackgroundTask.Restart();
        };

        // First load of the config file
        try
        {
            Services.ConfigService.ReloadConfig();
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

            // Create and edit a new config file
            await GenerateConfigWithProgress();
            await Services.ConfigEditor.EditConfig();
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

            // Edit the existing config file
            await Services.ConfigEditor.EditConfig();
        }
    }

    async Task GenerateConfigWithProgress()
    {
        var configGeneratorWindow = new ConfigGeneratorWindow();
        configGeneratorWindow.Show();

        var progress = configGeneratorWindow.ViewModel.Progress;

        await Task.Run(() =>
        {
            var configText = Services.ConfigGenerator.Generate(progress);
            using (var output = new StreamWriter(Services.ConfigService.ConfigPath))
            {
                output.Write(configText);
            }
        });

        configGeneratorWindow.Close();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Shutting down");

        _notifyIcon?.Dispose();
        _hiddenWindow?.Dispose();

        Services.Dispose();

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

    new public static App Current => (App)Application.Current;

    // TODO service discovery should not use App.Services
    public Services Services { get; }

    TaskbarIcon? _notifyIcon;
    HiddenWindow? _hiddenWindow;

    string _dataDirectory;
    string _logPath;

    LoggingLevelSwitch _loggingLevelSwitch;
}
