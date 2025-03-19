using System.Management;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace ShadowKVM;

internal interface IMonitorAPI
{
    public BOOL GetMonitorInfo(HMONITOR hMonitor, ref MONITORINFOEXW lpmi);

    public BOOL GetNumberOfPhysicalMonitorsFromHMONITOR(HMONITOR hMonitor, out uint pdwNumberOfPhysicalMonitors);
    public BOOL GetPhysicalMonitorsFromHMONITOR(HMONITOR hMonitor, PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    public BOOL EnumDisplayMonitors(HDC hdc, RECT? lprcClip, MONITORENUMPROC lpfnEnum, LPARAM dwData);
    public BOOL EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICEW lpDisplayDevice, uint dwFlags);

    public IEnumerable<IDictionary<string, object>> SelectAllWMIMonitorIDs();
}

internal class MonitorAPI : IMonitorAPI
{
    public BOOL GetMonitorInfo(HMONITOR hMonitor, ref MONITORINFOEXW lpmi)
    {
        return PInvoke.GetMonitorInfo(hMonitor, ref lpmi.monitorInfo);
    }

    public BOOL GetNumberOfPhysicalMonitorsFromHMONITOR(HMONITOR hMonitor, out uint pdwNumberOfPhysicalMonitors)
    {
        return PInvoke.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out pdwNumberOfPhysicalMonitors);
    }

    public BOOL GetPhysicalMonitorsFromHMONITOR(HMONITOR hMonitor, PHYSICAL_MONITOR[] pPhysicalMonitorArray)
    {
        return PInvoke.GetPhysicalMonitorsFromHMONITOR(hMonitor, pPhysicalMonitorArray);
    }

    public BOOL EnumDisplayMonitors(HDC hdc, RECT? lprcClip, MONITORENUMPROC lpfnEnum, LPARAM dwData)
    {
        return PInvoke.EnumDisplayMonitors(hdc, lprcClip, lpfnEnum, dwData);
    }

    public BOOL EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICEW lpDisplayDevice, uint dwFlags)
    {
        return PInvoke.EnumDisplayDevices(lpDevice, iDevNum, ref lpDisplayDevice, dwFlags);
    }

    public IEnumerable<IDictionary<string, object>> SelectAllWMIMonitorIDs()
    {
        var wmiMonitorIds = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WMIMonitorID").Get();

        // Convert from a WMI collection to a list of dictionaries, for mocking purposes
        return
            from ManagementBaseObject wmiMonitorId in wmiMonitorIds
            select wmiMonitorId.Properties.Cast<PropertyData>()
                .ToDictionary(property => property.Name, property => property.Value);
    }
}
