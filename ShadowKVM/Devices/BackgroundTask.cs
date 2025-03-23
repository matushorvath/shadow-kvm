using System.Windows;
using Serilog;
using Windows.Win32;

namespace ShadowKVM;

public class BackgroundTask(
    Config config,
    IDeviceNotificationService deviceNotificationService,
    IMonitorService monitorService,
    ILogger logger
        ) : IDisposable
{
    public void Start()
    {
        logger.Debug("Starting background task");
        _task = Task.Run(ProcessNotifications);
    }

    async void ProcessNotifications()
    {
        logger.Debug("Background task started");

        IDeviceNotification.Action? lastAction = null;

        using (var notification = deviceNotificationService.Register(config.TriggerDevice))
        {
            try
            {
                var actions = notification.Reader.ReadAllAsync(_cancellationTokenSource.Token);
                await foreach (IDeviceNotification.Action action in actions)
                {
                    if (App.IsEnabled && lastAction != action)
                    {
                        ProcessNotification(action);
                        lastAction = action;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Background task was cancelled from outside, just return
                logger.Debug("Background task stopped");
            }
        }
    }

    void ProcessNotification(IDeviceNotification.Action action)
    {
        logger.Debug("Received device notification, action {Action}", action);

        using (var monitors = monitorService.LoadMonitors())
        {
            foreach (var monitorConfig in config.Monitors ?? [])
            {
                // Find the action config for this device action
                var actionConfig = action == IDeviceNotification.Action.Arrival ? monitorConfig.Attach : monitorConfig.Detach;
                if (actionConfig == null)
                {
                    continue;
                }

                logger.Debug("Processing config {@MonitorConfig}", monitorConfig);

                // Execute the action for matching monitors
                var matchingMonitors =
                    from device in monitors
                    where (monitorConfig.Adapter == null || monitorConfig.Adapter == device.Adapter)
                        && (monitorConfig.Description == null || monitorConfig.Description == device.Description)
                        && (monitorConfig.SerialNumber == null || monitorConfig.SerialNumber == device.SerialNumber)
                    select device;

                if (matchingMonitors.Count() == 0)
                {
                    logger.Warning("Did not find any monitors for config {@MonitorConfig}", monitorConfig);
                    logger.Information("Following monitors exist: {@Monitors}", monitors);

                    continue;
                }

                foreach (var matchingMonitor in matchingMonitors)
                {
                    PInvoke.SetVCPFeature(matchingMonitor.Handle, actionConfig.Code, actionConfig.Value);
                    logger.Information("Executed action, code 0x{Code:x} value 0x{Value:x} monitor {@Monitor}",
                        actionConfig.Code.Raw, actionConfig.Value.Raw, matchingMonitor);
                }
            }
        }

        logger.Debug("Device notification processed");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_task != null)
            {
                // Cancel the task, wait up to five seconds for it to finish
                _cancellationTokenSource.Cancel();
                _task.Wait(TimeSpan.FromSeconds(5));
                _task = null;
            }
        }
    }

    Task? _task;

    CancellationTokenSource _cancellationTokenSource = new();

    App App => (App)Application.Current;
}
