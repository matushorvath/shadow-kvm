using Serilog;
using System.Collections;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace ShadowKVM;

internal class Monitor : IDisposable
{
    public Monitor(string device, SafePhysicalMonitorHandle handle, string description)
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

internal class Monitors : IEnumerable<Monitor>, IDisposable
{
    public void Refresh()
    {
        foreach (var monitor in _monitors)
        {
            monitor.Dispose();
        }
        _monitors.Clear();

        var physicalMonitors = LoadPhysicalMonitors();
        var displayDevices = LoadDisplayDevices();
        var wmiMonitorIds = LoadWmiMonitorIds();

        
    }

    struct PhysicalMonitor
    {
        public string device;
        public HANDLE handle;
        public string description;
    }

    unsafe List<PhysicalMonitor> LoadPhysicalMonitors()
    {
        var monitors = new List<PhysicalMonitor>();

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
                var monitor = new PhysicalMonitor
                {
                    device = monitorInfoEx.szDevice.ToString(),
                    description = physicalMonitor.szPhysicalMonitorDescription.ToString(),
                    handle = physicalMonitor.hPhysicalMonitor, 
                };

                monitors.Add(monitor);

                Log.Debug("Physical monitor: description \"{Description}\" device \"{Device}\"",
                    monitor.description, monitor.device);
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

        return monitors;
    }

    struct DisplayDevice
    {
        public string id;
        public string name;
        public string adapterString;
        public string monitorString;
    }

    List<DisplayDevice> LoadDisplayDevices()
    {
        var devices = new List<DisplayDevice>();

        BOOL success;

        var adapterDevice = new DISPLAY_DEVICEW();
        adapterDevice.cb = (uint)Marshal.SizeOf(adapterDevice);

        var monitorDevice = new DISPLAY_DEVICEW();
        monitorDevice.cb = (uint)Marshal.SizeOf(monitorDevice);

        uint adapterIndex = 0;
        while (true)
        {
            success = PInvoke.EnumDisplayDevices(null, adapterIndex++, ref adapterDevice, 1 /* EDD_GET_DEVICE_INTERFACE_NAME */);
            if (!success)
            {
                break;
            }

            uint monitorIndex = 0;
            while (true)
            {
                var deviceName = adapterDevice.DeviceName.ToString();
                success = PInvoke.EnumDisplayDevices(deviceName, monitorIndex++, ref monitorDevice, 1 /* EDD_GET_DEVICE_INTERFACE_NAME */);
                if (!success)
                {
                    break;
                }

                var device = new DisplayDevice
                {
                    id = monitorDevice.DeviceID.ToString(),
                    name = adapterDevice.DeviceName.ToString(),
                    adapterString = adapterDevice.DeviceString.ToString(),
                    monitorString = monitorDevice.DeviceString.ToString()
                };

                devices.Add(device);

                Log.Debug("Display device: adapter \"{AdapterString}\" monitor \"{MonitorString}\" name \"{Name}\" id \"{Id}\"",
                    device.adapterString, device.monitorString, device.name, device.id);
            }
        }

        return devices;
    }

    struct WmiMonitorId
    {
        public string instanceName;
        public string serialNumber;
    }

    List<WmiMonitorId> LoadWmiMonitorIds()
    {
        var wmiMonitorIds = new List<WmiMonitorId>();

        ManagementObjectSearcher wmiSearcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WMIMonitorID");
        ManagementObjectCollection monitorIds = wmiSearcher.Get();

        foreach (var monitorId in monitorIds)
        {
            var serialNumberBytes =
                from ch in (monitorId["SerialNumberID"] as ushort[]) ?? []
                where ch != 0
                select (byte)ch;

            var wmiMonitorId = new WmiMonitorId
            {
                instanceName = monitorId["InstanceName"]?.ToString() ?? string.Empty,
                serialNumber = Encoding.ASCII.GetString(serialNumberBytes.ToArray())
            };

            wmiMonitorIds.Add(wmiMonitorId);

            Log.Debug("WMI monitor id: instance \"{InstanceName}\" serial number \"{SerialNumber}\"",
                wmiMonitorId.instanceName, wmiMonitorId.serialNumber);
        }

        return wmiMonitorIds;
    }

    public IEnumerator<Monitor> GetEnumerator()
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

    List<Monitor> _monitors = new List<Monitor>();
}
