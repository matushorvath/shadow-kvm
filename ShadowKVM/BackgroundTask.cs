﻿using Windows.Win32;

internal class BackgroundTask(Config config) : IDisposable
{
    public void Start()
    {
        _task = Task.Run(ProcessNotifications);
    }

    async void ProcessNotifications()
    {
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
                // TODO log stopping the background task
            }
        }
    }

    void ProcessNotification(DeviceNotification.Action action)
    {
        // TODO log $"Action: {action}"

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

                // TODO log the device found, or if no such device found
                if (monitorDevice == null)
                {
                    continue;
                }

                // Execute the action for this monitor
                PInvoke.SetVCPFeature(monitorDevice.Handle, actionConfig.Code, actionConfig.Value);
            }
        }
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
