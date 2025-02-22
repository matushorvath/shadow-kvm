using Windows.Win32;

var config = Config.Load("C:\\Documents\\kvm\\config.yaml");

DeviceNotification.Action? lastAction = null;

using (var notification = new DeviceNotification())
using (var keyPressCanceller = new KeyPressCanceller())
{
    notification.Register();

    await foreach (DeviceNotification.Action action in notification.Reader.ReadAllAsync(keyPressCanceller.Token))
    {
        ProcessNotification(action);
    }
}

void ProcessNotification(DeviceNotification.Action action)
{
    if (lastAction == action)
    {
        // Ignore repeated actions
        return;
    }
    lastAction = action;

    // TODO log
    Console.WriteLine($"Action: {action}");

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

internal class KeyPressCanceller : IDisposable
{
    public KeyPressCanceller()
    {
        _tokenSource = new CancellationTokenSource();

        var waitFunction = () =>
        {
            Console.ReadKey();
            _tokenSource.Cancel();
        };

        _task = Task.Run(waitFunction);
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
                _task?.Dispose();
                _task = null;
            }
        }
    }

    public CancellationToken Token => _tokenSource.Token;

    CancellationTokenSource _tokenSource;
    Task? _task;
}
