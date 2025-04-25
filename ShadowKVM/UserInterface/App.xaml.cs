using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using H.NotifyIcon;
using Serilog;
using Serilog.Core;

namespace ShadowKVM;

// TODO write unit tests
// TODO use NativeUserInterface for configGeneratorWindow.Show()

public partial class App : Application
{
    public App()
    {
        // Use the real app data directory
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        DataDirectory = Path.Combine(appData, "Shadow KVM");

        // Set up logger
        Logger = Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(LoggingLevelSwitch)
            .WriteTo.File(LogPath, rollingInterval: RollingInterval.Day) // LogPath depends on DataDirectory
            .CreateLogger();

        Construct(Services.Instance.Autostart, Services.Instance.BackgroundTask, Services.Instance.ConfigEditor,
            Services.Instance.ConfigGenerator, Services.Instance.ConfigService, Services.Instance.NativeUserInterface);
    }

    public App(string dataDirectory, IAutostart autostart, IBackgroundTask backgroundTask, IConfigEditor configEditor,
        IConfigGenerator configGenerator, IConfigService configService, INativeUserInterface nativeUserInterface, ILogger logger)
    {
        DataDirectory = dataDirectory;
        Logger = logger;

        Construct(autostart, backgroundTask, configEditor, configGenerator, configService, nativeUserInterface);
    }

    [MemberNotNull(nameof(Autostart), nameof(BackgroundTask), nameof(ConfigEditor),
        nameof(ConfigGenerator), nameof(ConfigService), nameof(NativeUserInterface))]
    void Construct(IAutostart autostart, IBackgroundTask backgroundTask, IConfigEditor configEditor,
        IConfigGenerator configGenerator, IConfigService configService, INativeUserInterface nativeUserInterface)
    {
        Autostart = autostart;
        BackgroundTask = backgroundTask;
        ConfigEditor = configEditor;
        ConfigGenerator = configGenerator;
        ConfigService = configService;
        NativeUserInterface = nativeUserInterface;

        // Set up exception logging
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        Startup += OnStartupAsync;
    }

    async void OnStartupAsync(object sender, StartupEventArgs e)
    {
        Logger.Information("Initializing, version {FullSemVer} ({CommitDate})",
            GitVersionInformation.FullSemVer, GitVersionInformation.CommitDate);

        Directory.CreateDirectory(DataDirectory);

        // Set up config file location
        ConfigService.SetDataDirectory(DataDirectory);

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
        await InitConfig();

        // Debug log can only be enabled after loading config
        Logger.Debug("Version: {InformationalVersion}", GitVersionInformation.InformationalVersion);
    }

    async Task InitConfig()
    {
        // Reinitialize whenever the config file is changed
        ConfigService.ConfigChanged += (configService) =>
        {
            // Set up logging level based on config file
            LoggingLevelSwitch.MinimumLevel = configService.Config.LogLevel;

            BackgroundTask.Restart();
        };

        // First load of the config file
        try
        {
            ConfigService.ReloadConfig();
        }
        catch (FileNotFoundException)
        {
            var result = NativeUserInterface.QuestionBox("Configuration file not found, create a new one?", "Shadow KVM");
            if (!result)
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
                + $"{exception.Message}\n\nSee {Path.GetDirectoryName(LogPath)} for details";

            var result = NativeUserInterface.QuestionBox(message, "Shadow KVM");
            if (!result)
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
        configGeneratorWindow.Show(); // TODO not testable

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
        Logger.Information("Shutting down");

        NotifyIcon.Dispose();
        HiddenWindow.Dispose();

        Services.Instance.Dispose();

        base.OnExit(e);
    }

    void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        var error = (args.ExceptionObject as Exception)?.Message ?? args.ExceptionObject.ToString();
        var message = "Shadow KVM encountered an error and needs to close.\n\n"
            + $"{error}\n\nSee {Path.GetDirectoryName(LogPath)} for details";

        NativeUserInterface.ErrorBox(message, "Shadow KVM");

        Logger.Error("Unhandled exception: {@Exception}", args.ExceptionObject);
    }

    void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        var message = "Shadow KVM encountered an error and needs to close.\n\n"
            + $"{args.Exception.Message}\n\nSee {Path.GetDirectoryName(LogPath)} for details";

        NativeUserInterface.ErrorBox(message, "Shadow KVM");

        Logger.Error("Unobserved task exception: {@Exception}", args.Exception);
    }

    IAutostart Autostart { get; set; }
    IBackgroundTask BackgroundTask { get; set; }
    IConfigEditor ConfigEditor { get; set; }
    IConfigGenerator ConfigGenerator { get; set; }
    IConfigService ConfigService { get; set; }
    ILogger Logger { get; set; }
    INativeUserInterface NativeUserInterface { get; set; }

    TaskbarIcon NotifyIcon => (TaskbarIcon)FindResource("NotifyIcon");
    HiddenWindow HiddenWindow { get; } = new();

    string DataDirectory { get; }
    string LogPath => Path.Combine(DataDirectory, "logs", "shadow-kvm-.log");

    LoggingLevelSwitch LoggingLevelSwitch = new();
}
