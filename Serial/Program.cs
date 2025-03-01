using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

unsafe BOOL MonitorCallback(HMONITOR hMonitor, HDC hDc, RECT* rect, LPARAM lParam)
{
    BOOL success;

    var monitorInfoEx = new MONITORINFOEXW();
    monitorInfoEx.monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfoEx);

    success = PInvoke.GetMonitorInfo(hMonitor, ref monitorInfoEx.monitorInfo);
    if (!success)
    {
        Console.WriteLine("Getting monitor information failed");
        return true;
    }

    uint numberOfPhysicalMonitors;
    success = PInvoke.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out numberOfPhysicalMonitors);
    if (!success)
    {
        Console.WriteLine("Getting physical monitor number failed");
        return true;
    }

    if (numberOfPhysicalMonitors == 0)
    {
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
        var device = monitorInfoEx.szDevice.ToString();
        var description = physicalMonitor.szPhysicalMonitorDescription.ToString();

        Console.WriteLine($"Discovered physical monitor: description \"{description}\" device \"{device}\"");
    }

    return true;
}

BOOL success;

unsafe
{
    success = PInvoke.EnumDisplayMonitors(HDC.Null, null, MonitorCallback, 0);
    if (!success)
    {
        Console.WriteLine("Monitor enumeration failed");
    }
}

uint adapterIndex = 0;
while (true)
{
    var displayDevice = new DISPLAY_DEVICEW();
    displayDevice.cb = (uint)Marshal.SizeOf(displayDevice);

    success = PInvoke.EnumDisplayDevices(null, adapterIndex++, ref displayDevice, 1 /* EDD_GET_DEVICE_INTERFACE_NAME */);
    if (!success)
    {
        break;
    }

    // Console.WriteLine($"Adapter:");
    // Console.WriteLine($"    id {displayDevice.DeviceID}");
    // Console.WriteLine($"    key {displayDevice.DeviceKey}");
    // Console.WriteLine($"    name {displayDevice.DeviceName}");
    // Console.WriteLine($"    string {displayDevice.DeviceString}");

    uint monitorIndex = 0;
    while (true)
    {
        var deviceName = displayDevice.DeviceName.ToString();
        success = PInvoke.EnumDisplayDevices(deviceName, monitorIndex++, ref displayDevice, 1 /* EDD_GET_DEVICE_INTERFACE_NAME */);
        if (!success)
        {
            break;
        }

        Console.WriteLine($"Monitor:");
        Console.WriteLine($"    id {displayDevice.DeviceID}");
        Console.WriteLine($"    key {displayDevice.DeviceKey}");
        Console.WriteLine($"    name {displayDevice.DeviceName}");
        Console.WriteLine($"    string {displayDevice.DeviceString}");
    }
}

ManagementObjectSearcher wmiSearcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WMIMonitorID");
ManagementObjectCollection monitorIds = wmiSearcher.Get();

foreach (var monitorId in monitorIds)
{
    var serialBytes = (monitorId["SerialNumberID"] as ushort[])?.Select(n => (byte)n).ToArray();
    var serial = serialBytes != null ? Encoding.ASCII.GetString(serialBytes) : string.Empty;

    Console.WriteLine($"WMI Monitor: instance {monitorId["InstanceName"]} serial {serial}");
}
