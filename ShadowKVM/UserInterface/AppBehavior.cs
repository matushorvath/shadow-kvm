using System.IO;
using System.Windows;
using H.NotifyIcon;
using Serilog;
using Serilog.Core;

namespace ShadowKVM;

// TODO write unit tests

// This contains testable parts of the App class
public class AppBehavior(string dataDirectory, IAppControl appControl, IAutostart autostart, IBackgroundTask backgroundTask,
        IConfigEditor configEditor, IConfigGenerator configGenerator, IConfigService configService,
        INativeUserInterface nativeUserInterface, ILogger logger, LoggingLevelSwitch loggingLevelSwitch)
{
    public async void OnStartupAsync(object sender, StartupEventArgs e)
    {
        logger.Information("Initializing, version {FullSemVer} ({CommitDate})",
            GitVersionInformation.FullSemVer, GitVersionInformation.CommitDate);

        Directory.CreateDirectory(dataDirectory); // TODO use mockable filesystem

        // Set up config file location
        configService.SetDataDirectory(dataDirectory);

        // Enable autostart if this is the first time we run for this user
        // Needs to happen before initializing the notify icon
        if (!autostart.IsConfigured())
        {
            autostart.SetEnabled(true);
        }

        // Hidden window to listen for WM_CLOSE from installer
        HiddenWindow.Create();

        // Taskbar icon
        NotifyIcon.ForceCreate();

        // Create or load the config file
        await InitConfig();

        // Debug log can only be enabled after loading config
        logger.Debug("Version: {InformationalVersion}", GitVersionInformation.InformationalVersion);
    }

    async Task InitConfig()
    {
        // Reinitialize whenever the config file is changed
        configService.ConfigChanged += (configService) =>
        {
            // Set up logging level based on config file
            loggingLevelSwitch.MinimumLevel = configService.Config.LogLevel;

            backgroundTask.Restart();
        };

        // First load of the config file
        try
        {
            configService.ReloadConfig();
        }
        catch (FileNotFoundException)
        {
            var result = nativeUserInterface.QuestionBox("Configuration file not found, create a new one?", "Shadow KVM");
            if (!result)
            {
                appControl.Shutdown();
                return;
            }

            // Create and edit a new config file
            await GenerateConfigWithProgress();
            await configEditor.EditConfig();
        }
        catch (ConfigFileException exception)
        {
            var message = $"Configuration file is invalid, edit it manually?\n\n"
                + $"{exception.Message}\n\nSee {Path.GetDirectoryName(LogPath)} for details";

            var result = nativeUserInterface.QuestionBox(message, "Shadow KVM");
            if (!result)
            {
                appControl.Shutdown();
                return;
            }

            // Edit the existing config file
            await configEditor.EditConfig();
        }
    }

    async Task GenerateConfigWithProgress()
    {
        var configGeneratorWindow = new ConfigGeneratorWindow();
        nativeUserInterface.ShowWindow(configGeneratorWindow);

        var progress = configGeneratorWindow.ViewModel.Progress;

        await Task.Run(() =>
        {
            var configText = configGenerator.Generate(progress);
            using (var output = new StreamWriter(configService.ConfigPath))
            {
                output.Write(configText);
            }
        });

        configGeneratorWindow.Close();
    }

    public void OnExit(ExitEventArgs e)
    {
        logger.Information("Shutting down");

        NotifyIcon.Dispose();
        HiddenWindow.Dispose();

        Services.Instance.Dispose();
    }

    public void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        var error = (args.ExceptionObject as Exception)?.Message ?? args.ExceptionObject.ToString();
        var message = "Shadow KVM encountered an error and needs to close.\n\n"
            + $"{error}\n\nSee {Path.GetDirectoryName(LogPath)} for details.";

        nativeUserInterface.ErrorBox(message, "Shadow KVM");

        logger.Error("Unhandled exception: {@Exception}", args.ExceptionObject);
    }

    public void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        var message = "Shadow KVM encountered an error and needs to close.\n\n"
            + $"{args.Exception.Message}\n\nSee {Path.GetDirectoryName(LogPath)} for details";

        nativeUserInterface.ErrorBox(message, "Shadow KVM");

        logger.Error("Unobserved task exception: {@Exception}", args.Exception);
    }

    public static string FormatLogPath(string dataDirectory)
    {
        return Path.Combine(dataDirectory, "logs", "shadow-kvm-.log");
    }

    TaskbarIcon NotifyIcon => (TaskbarIcon)Application.Current.FindResource("NotifyIcon");
    HiddenWindow HiddenWindow { get; } = new();

    string LogPath => FormatLogPath(dataDirectory);
}
