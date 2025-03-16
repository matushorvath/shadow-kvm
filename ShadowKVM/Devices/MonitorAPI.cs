using System.Collections;
using System.Management;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace ShadowKVM;

internal interface IMonitorAPI
{
    public BOOL GetMonitorInfo(HMONITOR hMonitor, ref MONITORINFO lpmi);

    public BOOL GetNumberOfPhysicalMonitorsFromHMONITOR(HMONITOR hMonitor, out uint pdwNumberOfPhysicalMonitors);
    public BOOL GetPhysicalMonitorsFromHMONITOR(HMONITOR hMonitor, Span<PHYSICAL_MONITOR> pPhysicalMonitorArray);

    public BOOL EnumDisplayMonitors(HDC hdc, RECT? lprcClip, MONITORENUMPROC lpfnEnum, LPARAM dwData);
    public BOOL EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICEW lpDisplayDevice, uint dwFlags);

    public IEnumerable SelectAllWMIMonitorIDs();
}

internal class MonitorAPI : IMonitorAPI
{
    public BOOL GetMonitorInfo(HMONITOR hMonitor, ref MONITORINFO lpmi)
    {
        return PInvoke.GetMonitorInfo(hMonitor, ref lpmi);
    }

    public BOOL GetNumberOfPhysicalMonitorsFromHMONITOR(HMONITOR hMonitor, out uint pdwNumberOfPhysicalMonitors)
    {
        return PInvoke.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out pdwNumberOfPhysicalMonitors);
    }

    public BOOL GetPhysicalMonitorsFromHMONITOR(HMONITOR hMonitor, Span<PHYSICAL_MONITOR> pPhysicalMonitorArray)
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

    public IEnumerable SelectAllWMIMonitorIDs()
    {
        return new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WMIMonitorID").Get();
    }
}
