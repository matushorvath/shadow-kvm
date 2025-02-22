using Windows.Win32;

using (var notification = new DeviceNotification())
{
    notification.Register();

    Console.WriteLine("Registration for device notifications succeeded");

    using (var monitors = new Monitors())
    {
        monitors.Refresh();

        foreach (var monitor in monitors)
        {
            Console.WriteLine($"Monitor: device {monitor.Device} description {monitor.Description}");
        }

        Console.WriteLine("Monitor enumeration succeeded");

        await foreach (DeviceNotification.Action action in notification.Reader.ReadAllAsync())
        {
            Console.WriteLine($"Action: {action}");

            // TODO remove
            PInvoke.SetVCPFeature(monitors.First().Handle, 0x62, 0x42);

            if (action == DeviceNotification.Action.Arrival)
            {
            }
            else if (action == DeviceNotification.Action.Removal)
            {
            }
        }
    }

    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
}
