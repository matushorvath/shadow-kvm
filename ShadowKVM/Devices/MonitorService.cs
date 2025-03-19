using Serilog;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace ShadowKVM;

internal interface IMonitorService
{
    public Monitors LoadMonitors();
}

// Partial class because of GeneratedRegex
internal partial class MonitorService(IMonitorAPI monitorAPI, ILogger logger) : IMonitorService
{
    public Monitors LoadMonitors()
    {
        var physicalMonitors = LoadPhysicalMonitors();
        var displayDevices = LoadDisplayDevices();
        var wmiMonitorIds = LoadWmiMonitorIds();

        return ConnectMonitorData(physicalMonitors, displayDevices, wmiMonitorIds);
    }

    class PhysicalMonitor
    {
        public required string Device { get; set; }
        public required HANDLE Handle { get; set; }
        public required string Description { get; set; }
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

            success = monitorAPI.GetMonitorInfo(hMonitor, ref monitorInfoEx);
            if (!success)
            {
                exception = new Exception("Getting monitor information failed");
                return true;
            }

            uint numberOfPhysicalMonitors;
            success = monitorAPI.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out numberOfPhysicalMonitors);
            if (!success)
            {
                exception = new Exception("Getting physical monitor number failed");
                return true;
            }

            if (numberOfPhysicalMonitors == 0)
            {
                return true;
            }
            if (numberOfPhysicalMonitors > 1)
            {
                // This would make it impossible to match physical monitors to display devices
                logger.Warning("Multiple physical monitors connected via one port are not supported");
                return true;
            }

            var physicalMonitors = new PHYSICAL_MONITOR[numberOfPhysicalMonitors];
            success = monitorAPI.GetPhysicalMonitorsFromHMONITOR(hMonitor, physicalMonitors);
            if (!success)
            {
                exception = new Exception("Getting physical monitor information failed");
                return true;
            }

            foreach (var physicalMonitor in physicalMonitors)
            {
                var monitor = new PhysicalMonitor
                {
                    Device = monitorInfoEx.szDevice.ToString(),
                    Description = physicalMonitor.szPhysicalMonitorDescription.ToString(),
                    Handle = physicalMonitor.hPhysicalMonitor
                };

                monitors.Add(monitor);

                logger.Debug("Physical monitor: {@Monitor}", monitor);
            }

            return true;
        }

        BOOL success = monitorAPI.EnumDisplayMonitors(HDC.Null, null, MonitorCallback, 0);

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

    class DisplayDevice
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Adapter { get; set; }
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
            success = monitorAPI.EnumDisplayDevices(null, adapterIndex++, ref adapterDevice, 1 /* EDD_GET_DEVICE_INTERFACE_NAME */);
            if (!success)
            {
                break;
            }

            // More than one monitor connected to the same adapter makes matching monitors impossible, so we don't support it
            var deviceName = adapterDevice.DeviceName.ToString();
            success = monitorAPI.EnumDisplayDevices(deviceName, 0, ref monitorDevice, 1 /* EDD_GET_DEVICE_INTERFACE_NAME */);
            if (!success)
            {
                continue;
            }

            var device = new DisplayDevice
            {
                Id = monitorDevice.DeviceID.ToString(),
                Name = adapterDevice.DeviceName.ToString(),
                Adapter = adapterDevice.DeviceString.ToString()
            };

            // Try to get a second monitor to see if there's more than one
            success = monitorAPI.EnumDisplayDevices(deviceName, 1, ref monitorDevice, 1 /* EDD_GET_DEVICE_INTERFACE_NAME */);
            if (success)
            {
                logger.Warning("Multiple monitor devices connected via one adapter device are not supported");
                continue;
            }

            devices.Add(device);

            logger.Debug("Display device: {@Device}", device);
        }

        return devices;
    }

    class WmiMonitorId
    {
        public required string InstanceName { get; set; }
        public required string SerialNumber { get; set; }
    }

    List<WmiMonitorId> LoadWmiMonitorIds()
    {
        var wmiMonitorIds = new List<WmiMonitorId>();

        var monitorIds = monitorAPI.SelectAllWMIMonitorIDs();
        foreach (IDictionary<string, object> monitorId in monitorIds)
        {
            var serialNumberBytes =
                from ch in (monitorId["SerialNumberID"] as ushort[]) ?? []
                where ch != 0
                select (byte)ch;

            var serialNumber = Encoding.ASCII.GetString(serialNumberBytes.ToArray());
            if (serialNumber == "0")
            {
                serialNumber = string.Empty;
            }

            var wmiMonitorId = new WmiMonitorId
            {
                InstanceName = monitorId["InstanceName"]?.ToString() ?? string.Empty,
                SerialNumber = serialNumber
            };

            wmiMonitorIds.Add(wmiMonitorId);

            logger.Debug("WMI monitor id: {@WmiMonitorId}", wmiMonitorId);
        }

        return wmiMonitorIds;
    }

    Monitors ConnectMonitorData(List<PhysicalMonitor> physicalMonitors, List<DisplayDevice> displayDevices, List<WmiMonitorId> wmiMonitorIds)
    {
        var monitors = new Monitors();

        foreach (var physicalMonitor in physicalMonitors)
        {
            // Include each physical monitor
            var monitor = new Monitor
            {
                Device = physicalMonitor.Device,
                Description = physicalMonitor.Description,
                Handle = new SafePhysicalMonitorHandle(physicalMonitor.Handle, true)
            };

            // Find display device for this physical monitor, if available
            var displayDevice = (
                from dd in displayDevices
                where dd.Name == physicalMonitor.Device
                select dd
            ).SingleOrDefault();

            if (displayDevice == null)
            {
                logger.Debug("Could not find display device for \"{Device}\"", physicalMonitor.Device);
            }
            else
            {
                // Load additional information from the display device
                monitor.Adapter = displayDevice.Adapter;

                // Find WMI monitor id for this display device, if available
                var wmiMonitorId = (
                    from wdi in wmiMonitorIds
                    where MatchDeviceId(displayDevice.Id, wdi.InstanceName)
                    select wdi
                ).SingleOrDefault();

                if (wmiMonitorId == null)
                {
                    logger.Debug("Could not find WMI monitor id for \"{Id}\"", displayDevice.Id);
                }
                else
                {
                    // Load additional information from the display device
                    monitor.SerialNumber = wmiMonitorId.SerialNumber;
                }
            }

            logger.Debug("Monitor: {@Monitor}", monitor);

            monitors.Add(monitor);
        }

        return monitors;
    }

    [GeneratedRegex(@"^\\\\\?\\DISPLAY#([^#]+)#([^#]+)#{[-0-9a-f]+}$", RegexOptions.IgnoreCase)]
    private static partial Regex DevIdRegex();

    [GeneratedRegex(@"^DISPLAY\\([^\\]+)\\([^_]+)_\d+$", RegexOptions.IgnoreCase)]
    private static partial Regex WmiIdRegex();

    bool MatchDeviceId(string devId, string wmiId)
    {
        // The DisplayDevice object and the WMI Monitor ID object contain the same id,
        // but it's formatted slightly differently:
        // devId: "\\?\DISPLAY#DELA1CE#5&fc538b4&0&UID4354#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}"
        // wmiId: "DISPLAY\DELA1CE\5&fc538b4&0&UID4354_0"

        var devMatch = DevIdRegex().Match(devId);
        if (!devMatch.Success)
        {
            logger.Warning("Could not parse display device \"{DevId}\"", devId);
            return false;
        }

        var wmiMatch = WmiIdRegex().Match(wmiId);
        if (!devMatch.Success)
        {
            logger.Warning("Could not parse WMI monitor id \"{WmiId}\"", wmiId);
            return false;
        }

        return devMatch.Groups[1].Value == wmiMatch.Groups[1].Value
            && devMatch.Groups[2].Value == wmiMatch.Groups[2].Value;
    }
}
