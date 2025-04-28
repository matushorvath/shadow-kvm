using System.IO;
using System.IO.Abstractions;
using Serilog;
using Serilog.Core;

namespace ShadowKVM;

// TODO write unit tests

// This contains testable parts of the App class
public class AppBehavior(string dataDirectory, IAppControl appControl, IAutostart autostart, IBackgroundTask backgroundTask,
        IConfigEditor configEditor, IConfigGenerator configGenerator, IConfigService configService, IFileSystem fileSystem,
        INativeUserInterface nativeUserInterface, ILogger logger, LoggingLevelSwitch loggingLevelSwitch)
{
    public async Task OnStartupAsync(object sender, EventArgs e)
    {
        logger.Information("Initializing, version {FullSemVer} ({CommitDate})",
            GitVersionInformation.FullSemVer, GitVersionInformation.CommitDate);

        fileSystem.Directory.CreateDirectory(dataDirectory);

        // Enable autostart if this is the first time we run for this user
        // Needs to happen before initializing the notify icon
        if (!autostart.IsConfigured())
        {
            autostart.SetEnabled(true);
        }

        // Create or load the config file
        await InitConfig(dataDirectory);

        // Debug log can only be enabled after loading config
        logger.Debug("Version: {InformationalVersion}", GitVersionInformation.InformationalVersion);
    }

    async Task InitConfig(string dataDirectory)
    {
        // Set up config file location
        configService.SetDataDirectory(dataDirectory);

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
            var message = $"""
                Configuration file is invalid, edit it manually?

                {exception.Message}

                See {Path.GetDirectoryName(LogPath)} for details
                """;

            var result = nativeUserInterface.QuestionBox(message.ReplaceLineEndings(), "Shadow KVM");
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
        var configGeneratorViewModel = new ConfigGeneratorViewModel();
        nativeUserInterface.ShowWindow((ConfigGeneratorWindow configGeneratorWindow) =>
        {
            configGeneratorWindow.DataContext = configGeneratorViewModel;
        });

        await Task.Run(() =>
        {
            var progress = configGeneratorViewModel.Progress;
            var configText = configGenerator.Generate(progress);

            using (var stream = fileSystem.File.Create(configService.ConfigPath))
            using (var output = new StreamWriter(stream))
            {
                output.Write(configText);
            }
        });
    }

    public void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        var error = (args.ExceptionObject as Exception)?.Message ?? args.ExceptionObject.ToString();
        var message = $"""
            Shadow KVM encountered an error and needs to close.

            {error}

            See {Path.GetDirectoryName(LogPath)} for details.
            """;

        nativeUserInterface.ErrorBox(message.ReplaceLineEndings(), "Shadow KVM");

        logger.Error("Unhandled exception: {@Exception}", args.ExceptionObject);
    }

    public void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        var message = $"""
            Shadow KVM encountered an error and needs to close.

            {args.Exception.Message}

            See {Path.GetDirectoryName(LogPath)} for details.
            """;

        nativeUserInterface.ErrorBox(message.ReplaceLineEndings(), "Shadow KVM");

        logger.Error("Unobserved task exception: {@Exception}", args.Exception);
    }

    public static string FormatLogPath(string dataDirectory)
    {
        return Path.Combine(dataDirectory, "logs", "shadow-kvm-.log");
    }

    string LogPath => FormatLogPath(dataDirectory);
}
