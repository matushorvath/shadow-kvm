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
    struct LParamData
    {
        public List<Monitor> monitors;
        public Exception exception;
    }

    public unsafe void Refresh()
    {
        // The lParamData variable is fixed, because it's a local variable in unsafe context
        var lParamData = new LParamData { monitors = _monitors };

#pragma warning disable CS8500 // take address of a managed type
        BOOL success = PInvoke.EnumDisplayMonitors(HDC.Null, null, MonitorCallback, (nint)(&lParamData));
#pragma warning restore CS8500

        if (!success)
        {
            throw new Exception("Monitor enumeration failed");
        }

        if (lParamData.exception != null)
        {
            throw lParamData.exception;
        }
    }

    static unsafe BOOL MonitorCallback(HMONITOR hMonitor, HDC hDc, RECT* rect, LPARAM lParam)
    {
#pragma warning disable CS8500 // declares a pointer to a managed type
        var lParamData = *(LParamData*)lParam.Value;
#pragma warning restore CS8500

        BOOL success;

        var monitorInfoEx = new MONITORINFOEXW();
        monitorInfoEx.monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfoEx);

        success = PInvoke.GetMonitorInfo(hMonitor, ref monitorInfoEx.monitorInfo);
        if (!success)
        {
            lParamData.exception = new Exception("Getting monitor information failed");
            return true;
        }

        uint numberOfPhysicalMonitors;
        success = PInvoke.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out numberOfPhysicalMonitors);
        if (!success)
        {
            lParamData.exception = new Exception("Getting physical monitor number failed");
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
            lParamData.exception = new Exception("Getting physical monitor information failed");
            return true;
        }

        foreach (var physicalMonitor in physicalMonitors)
        {
            // TODO also pass physicalMonitor.hPhysicalMonitor out, probably need to duplicate the handle
            var monitor = new Monitor(monitorInfoEx.szDevice.ToString(), physicalMonitor.szPhysicalMonitorDescription.ToString());
            lParamData.monitors.Add(monitor);
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
