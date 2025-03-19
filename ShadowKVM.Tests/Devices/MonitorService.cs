using Moq;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace ShadowKVM.Tests;

public class MonitorServiceTests
{
    Mock<IMonitorAPI> _monitorApiMock = new Mock<IMonitorAPI>();

    [Fact]
    public unsafe void LoadMonitors_FullyLoadsOneMonitor()
    {
        _monitorApiMock
            .Setup(m => m.EnumDisplayMonitors(HDC.Null, null, It.IsAny<MONITORENUMPROC>(), 0))
            .Returns(
                (HDC hdc, RECT? lprcClip, MONITORENUMPROC lpfnEnum, LPARAM dwData) =>
                    lpfnEnum(new HMONITOR(12345), HDC.Null, null, 0));

        _monitorApiMock
            .Setup(m => m.GetMonitorInfo((HMONITOR)12345u, ref It.Ref<MONITORINFOEXW>.IsAny))
            .Returns(
                (HMONITOR hMonitor, ref MONITORINFOEXW lpmi) =>
                {
                    Assert.Equal(104u, lpmi.monitorInfo.cbSize); // 104 is size of MONITORINFOEXW
                    lpmi.szDevice = "dEvIcEnAmE 1";
                    return (BOOL)true;
                });

        uint pdwNumberOfPhysicalMonitors = 1;
        _monitorApiMock
            .Setup(m => m.GetNumberOfPhysicalMonitorsFromHMONITOR((HMONITOR)12345u, out pdwNumberOfPhysicalMonitors))
            .Returns(true);

        _monitorApiMock
            .Setup(m => m.GetPhysicalMonitorsFromHMONITOR((HMONITOR)12345u, It.IsAny<PHYSICAL_MONITOR[]>()))
            .Returns(
                 (HMONITOR hMonitor, PHYSICAL_MONITOR[] pPhysicalMonitorArray) =>
                 {
                    Assert.Single(pPhysicalMonitorArray);
                    pPhysicalMonitorArray[0].szPhysicalMonitorDescription = "dEsCrIpTiOn 1";
                    pPhysicalMonitorArray[0].hPhysicalMonitor = (HANDLE)(nint)0x97531;
                    return (BOOL)true;
                 });

        int enumDisplayDevicesInvocation = 0;

        _monitorApiMock
            .Setup(m => m.EnumDisplayDevices(It.IsAny<string?>(), It.IsAny<uint>(), ref It.Ref<DISPLAY_DEVICEW>.IsAny, 1))
            .Returns(
                (string? lpDevice, uint iDevNum, ref DISPLAY_DEVICEW lpDisplayDevice, uint dwFlags) =>
                {
                    Assert.Equal(840u, lpDisplayDevice.cb); // 840 is size of DISPLAY_DEVICEW

                    switch (enumDisplayDevicesInvocation++)
                    {
                    case 0:
                        Assert.Null(lpDevice);
                        Assert.Equal(0u, iDevNum);
                        lpDisplayDevice.DeviceName = "dEvIcEnAmE 1";
                        lpDisplayDevice.DeviceString = "aDaPtEr 1";
                        return (BOOL)true;
                    case 1:
                        Assert.Equal("dEvIcEnAmE 1", lpDevice);
                        Assert.Equal(0u, iDevNum);
                        lpDisplayDevice.DeviceID = "dEvIcEiD 1";
                        return (BOOL)true;
                    case 2:
                        Assert.Equal("dEvIcEnAmE 1", lpDevice);
                        Assert.Equal(1u, iDevNum);
                        return (BOOL)false;
                    case 3:
                        Assert.Null(lpDevice);
                        Assert.Equal(1u, iDevNum);
                        return (BOOL)false;
                    default:
                        Assert.Fail("Too many calls to EnumDisplayDevices");
                        throw new Exception("Too many calls to EnumDisplayDevices");
                    }
                });

        var monitorService = new MonitorService(_monitorApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Collection(monitors, monitor =>
        {
            Assert.Equal("dEvIcEnAmE 1", monitor.Device);
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Equal("aDaPtEr 1", monitor.Adapter);
            Assert.Equal((nint)0x97531u, monitor.Handle.DangerousGetHandle());
        });
    }
}
