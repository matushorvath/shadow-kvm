using System.Text;
using Moq;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace ShadowKVM.Tests;

public class MonitorServiceFixture
{
    internal Mock<IMonitorAPI> _monitorApiMock = new();

    protected class LoadPhysicalMonitors_Monitor
    {
        public required nint monitorHandle;
        public required string device;
        public required IList<LoadPhysicalMonitors_PhysicalMonitor> physicalMonitors;
    }

    protected class LoadPhysicalMonitors_PhysicalMonitor
    {
        public required string description;
        public required nint physicalHandle;
    }

    protected class LoadDisplayDevices_Adapter
    {
        public required string deviceName;
        public required string deviceString;
        public required IList<LoadDisplayDevices_Monitor> monitors;
    }

    protected class LoadDisplayDevices_Monitor
    {
        public required string deviceID;
    }

    protected class LoadWmiMonitorIds_Data
    {
        public required string serialNumber;
        public required string instanceName;
    }

    protected unsafe void SetupLoadPhysicalMonitors(IList<LoadPhysicalMonitors_Monitor> results)
    {
        _monitorApiMock
            .Setup(m => m.EnumDisplayMonitors(HDC.Null, null, It.IsNotNull<MONITORENUMPROC>(), 0))
            .Returns(
                (HDC hdc, RECT? lprcClip, MONITORENUMPROC lpfnEnum, LPARAM dwData) =>
                {
                    foreach (var result in results)
                    {
                        Assert.True(lpfnEnum((HMONITOR)result.monitorHandle, HDC.Null, null, 0));
                    }

                    return (BOOL)true;
                });

        var currentResultIndex = -1;

        var getMonitorInfoInvocation = 0;
        _monitorApiMock
            .Setup(m => m.GetMonitorInfo(It.IsAny<HMONITOR>(), ref It.Ref<MONITORINFOEXW>.IsAny))
            .Returns(
                (HMONITOR hMonitor, ref MONITORINFOEXW lpmi) =>
                {
                    currentResultIndex++;
                    Assert.InRange(currentResultIndex, 0, results.Count - 1);

                    Assert.Equal(104u, lpmi.monitorInfo.cbSize); // 104 is size of MONITORINFOEXW
                    Assert.Equal(results[currentResultIndex].monitorHandle, hMonitor);

                    lpmi.szDevice = results[getMonitorInfoInvocation].device;

                    getMonitorInfoInvocation++;
                    return (BOOL)true;
                });


        uint pdwNumberOfPhysicalMonitors;
        _monitorApiMock
            .Setup(m => m.GetNumberOfPhysicalMonitorsFromHMONITOR(It.IsAny<HMONITOR>(), out pdwNumberOfPhysicalMonitors))
            .Returns(
                (HMONITOR hMonitor, out uint pdwNumberOfPhysicalMonitors) =>
                {
                    Assert.Equal(results[currentResultIndex].monitorHandle, hMonitor);
                    pdwNumberOfPhysicalMonitors = (uint)results[currentResultIndex].physicalMonitors.Count;
                    return (BOOL)true;
                }
            );


        _monitorApiMock
            .Setup(m => m.GetPhysicalMonitorsFromHMONITOR(It.IsAny<HMONITOR>(), It.IsAny<PHYSICAL_MONITOR[]>()))
            .Returns(
                 (HMONITOR hMonitor, PHYSICAL_MONITOR[] pPhysicalMonitorArray) =>
                 {
                    Assert.Equal(results[currentResultIndex].monitorHandle, hMonitor);
                    Assert.Equal(results[currentResultIndex].physicalMonitors.Count, pPhysicalMonitorArray.Length);

                    for (var physicalMonitorIndex = 0; physicalMonitorIndex < results[currentResultIndex].physicalMonitors.Count; physicalMonitorIndex++)
                    {
                        var physicalMonitor = results[currentResultIndex].physicalMonitors[physicalMonitorIndex];
                        pPhysicalMonitorArray[physicalMonitorIndex].szPhysicalMonitorDescription = physicalMonitor.description;
                        pPhysicalMonitorArray[physicalMonitorIndex].hPhysicalMonitor = (HANDLE)physicalMonitor.physicalHandle;
                    }

                    return (BOOL)true;
                 });
    }

    protected void SetupLoadDisplayDevices(List<LoadDisplayDevices_Adapter> adapters)
    {
        _monitorApiMock
            .Setup(m => m.EnumDisplayDevices(It.IsAny<string?>(), It.IsAny<uint>(), ref It.Ref<DISPLAY_DEVICEW>.IsAny, 1))
            .Returns(
                (string? lpDevice, uint iDevNum, ref DISPLAY_DEVICEW lpDisplayDevice, uint dwFlags) =>
                {
                    Assert.Equal(840u, lpDisplayDevice.cb); // 840 is size of DISPLAY_DEVICEW

                    if (lpDevice == null)
                    {
                        // Enum adapters
                        Assert.InRange((int)iDevNum, 0, adapters.Count);

                        if (iDevNum >= adapters.Count)
                        {
                            return (BOOL)false;
                        }

                        lpDisplayDevice.DeviceName = adapters[(int)iDevNum].deviceName;
                        lpDisplayDevice.DeviceString = adapters[(int)iDevNum].deviceString;

                        return (BOOL)true;
                    }
                    else
                    {
                        // Enum monitors
                        var monitors = (from a in adapters where a.deviceName == lpDevice select a).Single().monitors;

                        Assert.InRange((int)iDevNum, 0, monitors.Count);

                        if (iDevNum >= monitors.Count)
                        {
                            return (BOOL)false;
                        }

                        lpDisplayDevice.DeviceID = monitors[(int)iDevNum].deviceID;

                        return (BOOL)true;

                    }
                });
    }

    protected void SetupLoadWmiMonitorIds(List<LoadWmiMonitorIds_Data> ids)
    {
        _monitorApiMock
            .Setup(m => m.SelectAllWMIMonitorIDs())
            .Returns(
                from id in ids
                select new Dictionary<string, object> {
                    ["SerialNumberID"] = Encoding.ASCII.GetBytes(id.serialNumber).Select(b => (ushort)b).ToArray(),
                    ["InstanceName"] = id.instanceName
                });
    }
}
