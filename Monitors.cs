using System.Collections;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

public class Monitor
{
    public Monitor(string device, string description)
    {
        Device = device;
        Description = description;
    }

    public string Device { get; }
    public string Description { get; }
};

class Monitors : IEnumerable<Monitor>
{
    public unsafe void Refresh()
    {
#pragma warning disable CS8500 // take address of a managed type
        fixed (List<Monitor>* monitorsPtr = &_monitors)
        {
            BOOL success = PInvoke.EnumDisplayMonitors(HDC.Null, null, MonitorCallback, (nint)monitorsPtr);
            // TODO also pass errors from MonitorCallback to here (through LPARAM)
            if (!success)
            {
                throw new Exception("Monitor enumeration failed");
            }
        }
#pragma warning restore CS8500
    }

    static unsafe BOOL MonitorCallback(HMONITOR hMonitor, HDC hDc, RECT* rect, LPARAM lparam)
    {
#pragma warning disable CS8500 // declares a pointer to a managed type
        List<Monitor> monitors = *(List<Monitor>*)lparam.Value;
#pragma warning restore CS8500

        BOOL success;

        var monitorInfoEx = new MONITORINFOEXW();
        monitorInfoEx.monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfoEx);

        success = PInvoke.GetMonitorInfo(hMonitor, ref monitorInfoEx.monitorInfo);
        if (!success)
        {
            Console.WriteLine("Getting monitor information failed"); // TODO report error through lparam
            return true;
        }

        uint numberOfPhysicalMonitors;
        success = PInvoke.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out numberOfPhysicalMonitors);
        if (!success)
        {
            Console.WriteLine("Getting physical monitor number failed"); // TODO report error through lparam
            return true;
        }

        if (numberOfPhysicalMonitors == 0) {
            return true;
        }

        var physicalMonitors = new PHYSICAL_MONITOR[numberOfPhysicalMonitors];
        success = PInvoke.GetPhysicalMonitorsFromHMONITOR(hMonitor, physicalMonitors);
        if (!success)
        {
            Console.WriteLine("Getting physical monitor information failed"); // TODO report error through lparam
            return true;
        }

        foreach (var physicalMonitor in physicalMonitors)
        {
            // TODO also pass physicalMonitor.hPhysicalMonitor out, probably need to duplicate the handle
            monitors.Add(new Monitor(monitorInfoEx.szDevice.ToString(), physicalMonitor.szPhysicalMonitorDescription.ToString()));
        }

        return true;
    }

    public IEnumerator<Monitor> GetEnumerator()
    {
        return _monitors.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    List<Monitor> _monitors = new List<Monitor>();
}
