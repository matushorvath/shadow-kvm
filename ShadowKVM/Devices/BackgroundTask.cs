using Serilog;

namespace ShadowKVM;

public interface IBackgroundTask : IDisposable
{
    void Restart();
    bool Enabled { get; set; }
}

public class BackgroundTask(
    IConfigService configService,
    IDeviceNotificationService deviceNotificationService,
    IMonitorService monitorService,
    IWindowsAPI windowsAPI,
    ILogger logger
        ) : IBackgroundTask
{
    public void Restart()
    {
        if (_task != null)
        {
            logger.Debug("Stopping background task");

            _cancellationTokenSource.Cancel();
            _task.Wait();
        }

        logger.Debug("Starting background task");

        _cancellationTokenSource = new CancellationTokenSource();
        _task = Task.Run(() => Execute());
    }

    async Task Execute()
    {
        logger.Debug("Background task started"); // used for synchronization in unit tests

        try
        {
            using (var notification = deviceNotificationService.Register(configService.Config.TriggerDevice))
            {
                await ProcessNotifications(notification);
            }
        }
        catch (OperationCanceledException)
        {
            // Background task was cancelled from outside, just return
            logger.Debug("Background task stopped"); // used for synchronization in unit tests
        }
        catch (Exception exception)
        {
            logger.Warning("Background task failed: {Exception}", exception); // used for synchronization in unit tests
        }
    }

    async Task ProcessNotifications(IDeviceNotification notification)
    {
        IDeviceNotification.Action? lastAction = null;

        var actions = notification.Reader.ReadAllAsync(_cancellationTokenSource.Token);
        await foreach (IDeviceNotification.Action action in actions)
        {
            if (!Enabled)
            {
                logger.Debug("Ignoring device notification while disabled, action {Action}", action); // used for synchronization in unit tests
            }
            else if (lastAction == action)
            {
                logger.Debug("Ignoring duplicate device notification, action {Action}", action); // used for synchronization in unit tests
            }
            else
            {
                ProcessOneNotification(action);
                lastAction = action;
            }
        }
    }

    void ProcessOneNotification(IDeviceNotification.Action action)
    {
        logger.Debug("Received device notification, action {Action}", action);

        using (var monitors = monitorService.LoadMonitors())
        {
            foreach (var monitorConfig in configService.Config.Monitors ?? [])
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
                    windowsAPI.SetVCPFeature(matchingMonitor.Handle, actionConfig.Code, actionConfig.Value);
                    logger.Information("Executed action, code 0x{Code:x} value 0x{Value:x} monitor {@Monitor}",
                        actionConfig.Code.Raw, actionConfig.Value.Raw, matchingMonitor);
                }
            }
        }

        logger.Debug("Device notification processed"); // used for synchronization in unit tests
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
                _task.Wait();
                _task = null;
            }
        }
    }

    public bool Enabled { get; set; } = true;

    public Task? _task; // public for unit tests, don't use
    CancellationTokenSource _cancellationTokenSource = new();
}
