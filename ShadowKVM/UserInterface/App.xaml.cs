using System.IO;
using System.Windows;
using H.NotifyIcon;
using Serilog;
using Serilog.Core;

namespace ShadowKVM;

// TODO make testable, write unit tests

public partial class App : Application
{
    public App()
        : this(Services.Instance.Autostart, Services.Instance.BackgroundTask, Services.Instance.ConfigEditor,
            Services.Instance.ConfigGenerator, Services.Instance.ConfigService)
    {
    }

    public App(IAutostart autostart, IBackgroundTask backgroundTask, IConfigEditor configEditor,
        IConfigGenerator configGenerator, IConfigService configService)
    {
        Autostart = autostart;
        BackgroundTask = backgroundTask;
        ConfigEditor = configEditor;
        ConfigGenerator = configGenerator;
        ConfigService = configService;

        Startup += OnStartupAsync;
    }

    async void OnStartupAsync(object sender, StartupEventArgs e)
    {
        // Set up data directory
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var dataDirectory = Path.Combine(appData, "Shadow KVM");
        Directory.CreateDirectory(dataDirectory);

        InitLogger(dataDirectory);

        Log.Information("Initializing, version {FullSemVer} ({CommitDate})",
            GitVersionInformation.FullSemVer, GitVersionInformation.CommitDate);

        // Enable autostart if this is the first time we run for this user
        // Needs to happen before initializing the notify icon
        if (!Autostart.IsConfigured())
        {
            Autostart.SetEnabled(true);
        }

        // Hidden window to listen for WM_CLOSE from installer
        HiddenWindow.Create();

        // Taskbar icon
        NotifyIcon.ForceCreate();

        // Create or load the config file
        await InitConfig(dataDirectory);

        // Debug log can only be enabled after loading config
        Log.Debug("Version: {InformationalVersion}", GitVersionInformation.InformationalVersion);
    }

    void InitLogger(string dataDirectory)
    {
        _logPath = Path.Combine(dataDirectory, "logs", "shadow-kvm-.log");

        // Set up logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(_loggingLevelSwitch)
            .WriteTo.File(_logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Set up exception logging
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    async Task InitConfig(string dataDirectory)
    {
        ConfigService.SetDataDirectory(dataDirectory);

        // Reinitialize whenever the config file is changed
        ConfigService.ConfigChanged += (configService) =>
        {
            // Set up logging level based on config file
            _loggingLevelSwitch.MinimumLevel = configService.Config.LogLevel;

            BackgroundTask.Restart();
        };

        // First load of the config file
        try
        {
            ConfigService.ReloadConfig();
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
            await ConfigEditor.EditConfig();
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
            await ConfigEditor.EditConfig();
        }
    }

    async Task GenerateConfigWithProgress()
    {
        var configGeneratorWindow = new ConfigGeneratorWindow();
        configGeneratorWindow.Show();

        var progress = configGeneratorWindow.ViewModel.Progress;

        await Task.Run(() =>
        {
            var configText = ConfigGenerator.Generate(progress);
            using (var output = new StreamWriter(ConfigService.ConfigPath))
            {
                output.Write(configText);
            }
        });

        configGeneratorWindow.Close();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Shutting down");

        NotifyIcon.Dispose();
        HiddenWindow.Dispose();

        Services.Instance.Dispose();

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

    IAutostart Autostart { get; }
    IBackgroundTask BackgroundTask { get; }
    IConfigEditor ConfigEditor { get; }
    IConfigGenerator ConfigGenerator { get; }
    IConfigService ConfigService { get; }

    TaskbarIcon NotifyIcon => (TaskbarIcon)FindResource("NotifyIcon");
    HiddenWindow HiddenWindow { get; } = new();

    LoggingLevelSwitch _loggingLevelSwitch = new();
    string? _logPath;
}
