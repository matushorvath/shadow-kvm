using System.Management;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using System.Diagnostics.CodeAnalysis;

namespace ShadowKVM;

public interface IWindowsAPI
{
    // For MonitorService
    public BOOL GetMonitorInfo(HMONITOR hMonitor, ref MONITORINFOEXW lpmi);

    public BOOL GetNumberOfPhysicalMonitorsFromHMONITOR(HMONITOR hMonitor, out uint pdwNumberOfPhysicalMonitors);
    public BOOL GetPhysicalMonitorsFromHMONITOR(HMONITOR hMonitor, PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    public BOOL EnumDisplayMonitors(HDC hdc, RECT? lprcClip, MONITORENUMPROC lpfnEnum, LPARAM dwData);
    public BOOL EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICEW lpDisplayDevice, uint dwFlags);

    public IEnumerable<IDictionary<string, object>> SelectAllWMIMonitorIDs();

    // For SafePhysicalMonitorHandle
    public BOOL DestroyPhysicalMonitor(HANDLE hMonitor);

    // For MonitorInputService
    public int GetCapabilitiesStringLength(SafeHandle hMonitor, out uint pdwCapabilitiesStringLengthInCharacters);

    public int CapabilitiesRequestAndCapabilitiesReply(
        SafeHandle hMonitor, PSTR pszASCIICapabilitiesString, uint dwCapabilitiesStringLengthInCharacters);

    public int GetVCPFeatureAndVCPFeatureReply(
        SafeHandle hMonitor, byte bVCPCode, ref MC_VCP_CODE_TYPE vct, out uint pdwCurrentValue, out uint pdwMaximumValue);

    // For DeviceNotificationService
    public CONFIGRET CM_Register_Notification(CM_NOTIFY_FILTER pFilter, nuint pContext,
        PCM_NOTIFY_CALLBACK pCallback, out CM_Unregister_NotificationSafeHandle pNotifyContext);

    // For BackgroundTask
    public int SetVCPFeature(SafeHandle hMonitor, byte bVCPCode, uint dwNewValue);
}

[ExcludeFromCodeCoverage(Justification = "Productive implementation of the Windows API interface")] 
public class WindowsAPI : IWindowsAPI
{
    // For MonitorService
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

    // For SafePhysicalMonitorHandle
    public BOOL DestroyPhysicalMonitor(HANDLE hMonitor)
    {
        return PInvoke.DestroyPhysicalMonitor(hMonitor);
    }

    // For MonitorInputService
    public int GetCapabilitiesStringLength(SafeHandle hMonitor, out uint pdwCapabilitiesStringLengthInCharacters)
    {
        return PInvoke.GetCapabilitiesStringLength(hMonitor, out pdwCapabilitiesStringLengthInCharacters);
    }

    public int CapabilitiesRequestAndCapabilitiesReply(
        SafeHandle hMonitor, PSTR pszASCIICapabilitiesString, uint dwCapabilitiesStringLengthInCharacters)
    {
        return PInvoke.CapabilitiesRequestAndCapabilitiesReply(
            hMonitor, pszASCIICapabilitiesString, dwCapabilitiesStringLengthInCharacters);
    }

    public unsafe int GetVCPFeatureAndVCPFeatureReply(
        SafeHandle hMonitor, byte bVCPCode, ref MC_VCP_CODE_TYPE vct, out uint pdwCurrentValue, out uint dwMaximumValue)
    {
        fixed (MC_VCP_CODE_TYPE* pvct = &vct)
        fixed (uint* pdwMaximumValue = &dwMaximumValue)
        {
            return PInvoke.GetVCPFeatureAndVCPFeatureReply(hMonitor, bVCPCode, pvct, out pdwCurrentValue, pdwMaximumValue);
        }
    }

    // For DeviceNotificationService
    public unsafe CONFIGRET CM_Register_Notification(CM_NOTIFY_FILTER pFilter, nuint pContext,
        PCM_NOTIFY_CALLBACK pCallback, out CM_Unregister_NotificationSafeHandle pNotifyContext)
    {
        return PInvoke.CM_Register_Notification(pFilter, (void*)pContext, pCallback, out pNotifyContext);
    }

    // For BackgroundTask
    public int SetVCPFeature(SafeHandle hMonitor, byte bVCPCode, uint dwNewValue)
    {
        return PInvoke.SetVCPFeature(hMonitor, bVCPCode, dwNewValue);
    }
}
