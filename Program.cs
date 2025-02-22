using Windows.Win32;

var config = Config.Load("C:\\Documents\\kvm\\config.yaml");

DeviceNotification.Action? lastAction = null;

using (var notification = new DeviceNotification())
{
    notification.Register();

    await foreach (DeviceNotification.Action action in notification.Reader.ReadAllAsync())
    {
        if (lastAction != action) {
            Console.WriteLine($"Action: {action}");

            using (var monitors = new Monitors())
            {
                monitors.Refresh();

                foreach (var monitor in monitors)
                {
                    if (action == DeviceNotification.Action.Arrival)
                    {
                        // TODO remove
                        PInvoke.SetVCPFeature(monitor.Handle, 0x60, 0x11);
                    }
                    else if (action == DeviceNotification.Action.Removal)
                    {
                        // TODO remove
                        PInvoke.SetVCPFeature(monitor.Handle, 0x60, 0x12);
                    }
                }
            }

            lastAction = action;
        }
    }
}
