using Windows.Win32;

var config = Config.Load("C:\\Documents\\kvm\\config.yaml");

DeviceNotification.Action? lastAction = null;

using (var notification = new DeviceNotification())
{
    notification.Register();

    await foreach (DeviceNotification.Action action in notification.Reader.ReadAllAsync())
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
