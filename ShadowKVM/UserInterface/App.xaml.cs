using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using Serilog;
using Serilog.Core;

namespace ShadowKVM;

// The Application class is a singleton, which makes it difficult to test
// Therefore we separate the testable parts into a separate AppBehavior class
[ExcludeFromCodeCoverage(Justification = "Non-testable parts of the application instance")]
public partial class App : Application
{
    public App()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dataDirectory = Path.Combine(appData, "Shadow KVM");

        // Set up logger
        var logPath = AppBehavior.FormatLogPath(dataDirectory);
        var loggingLevelSwitch = new LoggingLevelSwitch();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(loggingLevelSwitch)
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Instantiate the testable implementation class
        Behavior = new AppBehavior(dataDirectory, Services.Instance.AppControl, Services.Instance.Autostart,
            Services.Instance.BackgroundTask, Services.Instance.ConfigEditor, Services.Instance.ConfigGenerator,
            Services.Instance.ConfigService, Services.Instance.NativeUserInterface, Log.Logger, loggingLevelSwitch);

        // Set up exception logging
        AppDomain.CurrentDomain.UnhandledException += Behavior.OnUnhandledException;
        TaskScheduler.UnobservedTaskException += Behavior.OnUnobservedTaskException;

        // Set up application startup
        Startup += Behavior.OnStartupAsync;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException -= Behavior.OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= Behavior.OnUnobservedTaskException;

        Behavior.OnExit(e);
        base.OnExit(e);
    }

    AppBehavior Behavior { get; }
}
