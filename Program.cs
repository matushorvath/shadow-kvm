using System.Runtime.InteropServices;
using System.Threading.Channels;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

// Disposing this will unregister the device notification
CM_Unregister_NotificationSafeHandle notification;

var channelOptions = new BoundedChannelOptions(16);
channelOptions.FullMode = BoundedChannelFullMode.DropOldest;
channelOptions.SingleReader = true;
channelOptions.SingleWriter = true;

var channel = Channel.CreateUnbounded<CM_NOTIFY_ACTION>();

unsafe
{
    uint DeviceCallback(HCMNOTIFICATION notification, [Optional] void* Context,
        CM_NOTIFY_ACTION action, CM_NOTIFY_EVENT_DATA* evt, uint eventDataSize)
    {
        if (evt->FilterType != CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE)
        {
            return (uint)WIN32_ERROR.ERROR_SUCCESS;
        }

        if (action != CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL
            && action != CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEINTERFACEREMOVAL)
        {
            return (uint)WIN32_ERROR.ERROR_SUCCESS;
        }

        channel.Writer.TryWrite(action);    // always succeeds
        return (uint)WIN32_ERROR.ERROR_SUCCESS;
    }

    CM_NOTIFY_FILTER filter = new CM_NOTIFY_FILTER();
    filter.cbSize = (uint)Marshal.SizeOf(filter);
    filter.FilterType = CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE;
    filter.u.DeviceInterface.ClassGuid = PInvoke.GUID_DEVINTERFACE_KEYBOARD;

    CONFIGRET res = PInvoke.CM_Register_Notification(filter, null, DeviceCallback, out notification);
    if (res != CONFIGRET.CR_SUCCESS)
    {
        Console.WriteLine($"Registration for device notifications failed, result {res}");
        // TODO return error
    }
}

Console.WriteLine("Registration for device notifications succeeded");

unsafe
{
    BOOL MonitorCallback(HMONITOR hMonitor, HDC hDc, RECT* rect, LPARAM lparam)
    {
        Console.WriteLine($"Monitor: {hMonitor}");

        BOOL success;

        uint numberOfPhysicalMonitors;
        success = PInvoke.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out numberOfPhysicalMonitors);
        if (!success)
        {
            Console.WriteLine("Getting physical monitor number failed");
            return true;
        }

        var physicalMonitors = new PHYSICAL_MONITOR[numberOfPhysicalMonitors];
        success = PInvoke.GetPhysicalMonitorsFromHMONITOR(hMonitor, physicalMonitors);
        if (!success)
        {
            Console.WriteLine("Getting physical monitor information failed");
            return true;
        }

        foreach (var physicalMonitor in physicalMonitors)
        {
            Console.WriteLine($"Monitor device {physicalMonitor.szPhysicalMonitorDescription}");
        }

        return true;
    }

    BOOL success = PInvoke.EnumDisplayMonitors(HDC.Null, null, MonitorCallback, 0);
    if (!success)
    {
        Console.WriteLine("Monitor enumeration failed");
        // TODO return error
    }
}

await foreach (CM_NOTIFY_ACTION action in channel.Reader.ReadAllAsync())
{
    Console.WriteLine($"Action: {action}");
}

Console.WriteLine("Press any key to continue...");
Console.ReadKey();
