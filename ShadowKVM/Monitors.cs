using Serilog;
using System.Collections;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace ShadowKVM;

internal class MonitorDevice : IDisposable
{
    public MonitorDevice(string device, SafePhysicalMonitorHandle handle, string description)
    {
        Device = device;
        Handle = handle;
        Description = description;
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
            if (!Handle.IsInvalid)
            {
                Handle.Dispose();
                Handle.SetHandleAsInvalid();
            }
        }
    }

    public string Device { get; }
    public SafePhysicalMonitorHandle Handle { get; } // TODO IDisposable, also on Monitors below
    public string Description { get; }
};

internal class MonitorDevices : IEnumerable<MonitorDevice>, IDisposable
{
    struct LParamData
    {
        public List<MonitorDevice> monitors;
        public Exception exception;
    }

    public unsafe void Refresh()
    {
        foreach (var monitor in _monitors)
        {
            monitor.Dispose();
        }
        _monitors.Clear();

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
            var device = monitorInfoEx.szDevice.ToString();
            var description = physicalMonitor.szPhysicalMonitorDescription.ToString();
            var handle = new SafePhysicalMonitorHandle(physicalMonitor.hPhysicalMonitor, true);
            var monitor = new MonitorDevice(device, handle, description);

            lParamData.monitors.Add(monitor);
            Log.Debug("Discovered physical monitor: description \"{Description}\" device \"{Device}\"", description, device);
        }

        return true;
    }

    public IEnumerator<MonitorDevice> GetEnumerator()
    {
        return _monitors.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
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
            foreach (var monitor in _monitors)
            {
                monitor.Dispose();
            }
            _monitors.Clear();
        }
    }

    List<MonitorDevice> _monitors = new List<MonitorDevice>();
}
