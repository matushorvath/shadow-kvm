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

uint index = 0;
while (true)
{
    var displayDevice = new DISPLAY_DEVICEW();
    displayDevice.cb = (uint)Marshal.SizeOf(displayDevice);

    success = PInvoke.EnumDisplayDevices(null, index++, ref displayDevice, 1 /* 1 = EDD_GET_DEVICE_INTERFACE_NAME */);
    if (!success)
    {
        break;
    }

    // Console.WriteLine($"Adapter:");
    // Console.WriteLine($"    id {displayDevice.DeviceID.ToString()}");
    // Console.WriteLine($"    key {displayDevice.DeviceKey.ToString()}");
    // Console.WriteLine($"    name {displayDevice.DeviceName.ToString()}");
    // Console.WriteLine($"    string {displayDevice.DeviceString.ToString()}");

    var deviceName = displayDevice.DeviceName.ToString();
    success = PInvoke.EnumDisplayDevices(deviceName, 0, ref displayDevice, 1 /* 1 = EDD_GET_DEVICE_INTERFACE_NAME */);
    if (!success)
    {
        continue;
    }

    Console.WriteLine($"Monitor:");
    Console.WriteLine($"    id {displayDevice.DeviceID.ToString()}");
    Console.WriteLine($"    key {displayDevice.DeviceKey.ToString()}");
    Console.WriteLine($"    name {displayDevice.DeviceName.ToString()}");
    Console.WriteLine($"    string {displayDevice.DeviceString.ToString()}");
}

ManagementObjectSearcher wmiSearcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WMIMonitorID");
ManagementObjectCollection monitorIds = wmiSearcher.Get();

foreach (var monitorId in monitorIds)
{
    var serialBytes = (monitorId["SerialNumberID"] as ushort[])?.Select(n => (byte)n).ToArray();
    var serial = serialBytes != null ? Encoding.ASCII.GetString(serialBytes) : string.Empty;

    Console.WriteLine($"WMI Monitor: instance {monitorId["InstanceName"]} serial {serial}");
}


//     Msgbox (A_Clipboard:=MonitorGetName(A_Index))

// While EnumDisplayDevices(A_Index-1, &DISPLAY_DEVICEA)    {
//     if !DISPLAY_DEVICEA["StateFlags"]
//         continue
//     tp:=""
//     For k,v in DISPLAY_DEVICEA
//         tp.=k " : " v "`n"
//     Msgbox (A_Clipboard:=tp)
// }

// WmiMonitorInfos:=GetWmiMonitorInfos()
// Loop WmiMonitorInfos.Count    {
//     tp:=""
//     For k,v in WmiMonitorInfos[A_Index]
//         tp.=k " : " v "`n"
//     Msgbox (A_Clipboard:=tp)
// }
// ExitApp



// /*
// EnumDisplayDevicesW function (winuser.h)
//     https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaydevicesw
// DISPLAY_DEVICEA structure (wingdi.h)
//     https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-display_devicea
// Get display name that matches that found in display settings
//     https://stackoverflow.com/questions/7486485/get-display-name-that-matches-that-found-in-display-settings
// Secondary Monitor
//     https://www.autohotkey.com/board/topic/20084-secondary-monitor/
// */
// EnumDisplayDevices(iDevNum, &DISPLAY_DEVICEA)    {
//     Static   EDD_GET_DEVICE_INTERFACE_NAME := 0x00000001
//             ,byteCount              := 4+4+((32+128+128+128)*2)
//             ,offset_cb              := 0
//             ,offset_DeviceName      := 4                            ,length_DeviceName      := 32
//             ,offset_DeviceString    := 4+(32*2)                     ,length_DeviceString    := 128
//             ,offset_StateFlags      := 4+((32+128)*2)
//             ,offset_DeviceID        := 4+4+((32+128)*2)             ,length_DeviceID        := 128
//             ,offset_DeviceKey       := 4+4+((32+128+128)*2)         ,length_DeviceKey       := 128


//     DISPLAY_DEVICEA:=Map("cb",0,"DeviceName","","DisplayAdapter","","DisplayMonitor","","StateFlags",0,"DeviceID","","DeviceKey","")
//     lpDisplayDevice:=Buffer(byteCount,0)
//     Numput("UInt",byteCount,lpDisplayDevice,offset_cb)
//     if !DllCall("EnumDisplayDevices", "Ptr",0, "UInt",iDevNum, "Ptr",lpDisplayDevice.Ptr, "UInt",0)
//         return false
//     For k in DISPLAY_DEVICEA    {
//         Switch k
//         {
//             case "cb","StateFlags":         DISPLAY_DEVICEA[k]:=NumGet(lpDisplayDevice, offset_%k%,"UInt")
//             case "DisplayAdapter":          DISPLAY_DEVICEA[k]:=StrGet(lpDisplayDevice.Ptr+offset_DeviceString,length_DeviceString)
//             case "DisplayMonitor":          continue
//             default:                        DISPLAY_DEVICEA[k]:=StrGet(lpDisplayDevice.Ptr+offset_%k%,length_%k%)
//         }
//     }
//     lpDisplayDevice:=Buffer(byteCount,0)
//     Numput("UInt",byteCount,lpDisplayDevice,offset_cb)
//     lpDevice:=Buffer(length_DeviceString*2,0)
//     StrPut(DISPLAY_DEVICEA["DeviceName"],lpDevice,length_DeviceString)
//     DllCall("EnumDisplayDevices", "Ptr",lpDevice.Ptr, "UInt",0, "Ptr",lpDisplayDevice.Ptr, "UInt",EDD_GET_DEVICE_INTERFACE_NAME)
//     DISPLAY_DEVICEA["DisplayMonitor"]:=StrGet(lpDisplayDevice.Ptr+offset_DeviceString,length_DeviceString)
//     return true
// }