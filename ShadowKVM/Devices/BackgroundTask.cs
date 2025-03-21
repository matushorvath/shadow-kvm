﻿using System.Windows;
using Serilog;
using Windows.Win32;

namespace ShadowKVM;

internal class BackgroundTask(Config config, IMonitorService monitorService) : IDisposable
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
            notification.Register(config.TriggerDevice);

            try
            {
                var actions = notification.Reader.ReadAllAsync(_cancellationTokenSource.Token);
                await foreach (DeviceNotification.Action action in actions)
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
                Log.Debug("Background task stopped");
            }
        }
    }

    void ProcessNotification(DeviceNotification.Action action)
    {
        Log.Debug("Received device notification, action {Action}", action);

        using (var monitors = monitorService.LoadMonitors())
        {
            foreach (var monitorConfig in config.Monitors ?? [])
            {
                // Find the action config for this device action
                var actionConfig = action == DeviceNotification.Action.Arrival ? monitorConfig.Attach : monitorConfig.Detach;
                if (actionConfig == null)
                {
                    continue;
                }

                Log.Debug("Processing config {@MonitorConfig}", monitorConfig);

                // Execute the action for matching monitors
                var matchingMonitors =
                    from device in monitors
                    where (monitorConfig.Adapter == null || monitorConfig.Adapter == device.Adapter)
                        && (monitorConfig.Description == null || monitorConfig.Description == device.Description)
                        && (monitorConfig.SerialNumber == null || monitorConfig.SerialNumber == device.SerialNumber)
                    select device;

                if (matchingMonitors.Count() == 0)
                {
                    Log.Warning("Did not find any monitors for config {@MonitorConfig}", monitorConfig);
                    Log.Information("Following monitors exist: {@Monitors}", monitors);

                    continue;
                }

                foreach (var matchingMonitor in matchingMonitors)
                {
                    PInvoke.SetVCPFeature(matchingMonitor.Handle, actionConfig.Code, actionConfig.Value);
                    Log.Information("Executed action, code 0x{Code:x} value 0x{Value:x} monitor {@Monitor}",
                        actionConfig.Code.Raw, actionConfig.Value.Raw, matchingMonitor);
                }
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

    App App => (App)Application.Current;
}
