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
    public SafePhysicalMonitorHandle Handle { get; }
    public string Description { get; }
};

internal class MonitorDevices : IEnumerable<MonitorDevice>, IDisposable
{
    public unsafe void Refresh()
    {
        foreach (var monitor in _monitors)
        {
            monitor.Dispose();
        }
        _monitors.Clear();

        // This exception is set inside MonitorCallback and checked after EnumDisplayMonitors returns
        Exception? exception = null;

        unsafe BOOL MonitorCallback(HMONITOR hMonitor, HDC hDc, RECT* rect, LPARAM lParam)
        {
            BOOL success;

            var monitorInfoEx = new MONITORINFOEXW();
            monitorInfoEx.monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfoEx);

            success = PInvoke.GetMonitorInfo(hMonitor, ref monitorInfoEx.monitorInfo);
            if (!success)
            {
                exception = new Exception("Getting monitor information failed");
                return true;
            }

            uint numberOfPhysicalMonitors;
            success = PInvoke.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out numberOfPhysicalMonitors);
            if (!success)
            {
                exception = new Exception("Getting physical monitor number failed");
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
                exception = new Exception("Getting physical monitor information failed");
                return true;
            }

            foreach (var physicalMonitor in physicalMonitors)
            {
                var device = monitorInfoEx.szDevice.ToString();
                var description = physicalMonitor.szPhysicalMonitorDescription.ToString();
                var handle = new SafePhysicalMonitorHandle(physicalMonitor.hPhysicalMonitor, true);
                var monitor = new MonitorDevice(device, handle, description);

                _monitors.Add(monitor);
                Log.Debug("Discovered physical monitor: description \"{Description}\" device \"{Device}\"", description, device);
            }

            return true;
        }

        BOOL success = PInvoke.EnumDisplayMonitors(HDC.Null, null, MonitorCallback, 0);

        if (exception != null)
        {
            throw exception;
        }

        if (!success)
        {
            throw new Exception("Monitor enumeration failed");
        }
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
