using System.Windows;
using H.NotifyIcon;

namespace ShadowKVM;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
        _notifyIcon.ForceCreate();

        _backgroundTask = new BackgroundTask();
        _backgroundTask.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _backgroundTask?.Dispose();
        _notifyIcon?.Dispose();

        base.OnExit(e);
    }

    TaskbarIcon? _notifyIcon;
    BackgroundTask? _backgroundTask;
}
