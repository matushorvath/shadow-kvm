using Serilog;
using System.Windows;
using Windows.Win32;

namespace ShadowKVM;

internal class BackgroundTask(Config config) : IDisposable
{
    public void Start()
    {
        Log.Debug("Starting background task");
        _task = Task.Run(ProcessNotifications);
    }

    async void ProcessNotifications()
    {
        Log.Debug("Background task started");

        DeviceNotification.Action? lastAction = null;

        using (var notification = new DeviceNotification())
        {
            notification.Register(config.DeviceClassGuid);

            try
            {
                var actions = notification.Reader.ReadAllAsync(_cancellationTokenSource.Token);
                await foreach (DeviceNotification.Action action in actions)
                {
                    if (lastAction != action)
                    {
                        ProcessNotification(action);
                        lastAction = action;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Background task was cancelled from outside, just return
                Log.Debug("Background task stopped");
            }
        }

        Application.Current.Shutdown();
    }

    void ProcessNotification(DeviceNotification.Action action)
    {
        Log.Debug("Received device notification, action {Action}", action);

        using (var monitorDevices = new MonitorDevices())
        {
            monitorDevices.Refresh();

            foreach (var monitorConfig in config.Monitors)
            {
                // Find the action config for this device action
                var actionConfig = action == DeviceNotification.Action.Arrival ? monitorConfig.Attach : monitorConfig.Detach;

                if (actionConfig == null)
                {
                    continue;
                }

                // Find a device to match this config item
                var monitorDevice = (
                        from device in monitorDevices
                        where device.Description == monitorConfig.Description
                            && device.Device == monitorConfig.Device
                        select device
                    ).SingleOrDefault();

                if (monitorDevice == null)
                {
                    Log.Warning("Did not find monitor for description \"{ConfigDescription}\" device \"{ConfigDevice}\"",
                        monitorConfig.Description, monitorConfig.Device);
                    var existingMonitors = from md in monitorDevices
                        select new { Description = md.Description, Device = md.Device };
                    Log.Debug("Following monitors exist: {@Monitors}", existingMonitors.ToArray());

                    continue;
                }

                Log.Debug("Found monitor for description \"{ConfigDescription}\" device \"{ConfigDevice}\"",
                    monitorConfig.Description, monitorConfig.Device);

                // Execute the action for this monitor
                PInvoke.SetVCPFeature(monitorDevice.Handle, actionConfig.Code, actionConfig.Value);

                Log.Debug("Executed action, code 0x{Code:X} value 0x{Value:X}", actionConfig.Code, actionConfig.Value);
            }
        }

        Log.Debug("Device notification processed");
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

    CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
}
