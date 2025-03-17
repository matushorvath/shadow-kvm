using Moq;
using System.IO.Abstractions.TestingHelpers;
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
        var hMonitorValue = 12345;

        _monitorApiMock
            .Setup(m => m.EnumDisplayMonitors(HDC.Null, null, It.IsAny<MONITORENUMPROC>(), 0))
            .Returns(
                (HDC hdc, RECT? lprcClip, MONITORENUMPROC lpfnEnum, LPARAM dwData) =>
                    lpfnEnum(new HMONITOR(hMonitorValue), HDC.Null, null, 0));

        _monitorApiMock
            .Setup(m => m.GetMonitorInfo((HMONITOR)hMonitorValue, ref It.Ref<MONITORINFOEXW>.IsAny))
            .Returns(
                (HMONITOR hMonitor, ref MONITORINFOEXW lpmi) =>
                {
                    Assert.Equal(104u, lpmi.monitorInfo.cbSize); // 104 is size of MONITORINFOEXW
                    lpmi.monitorInfo.cbSize = 42;
                    lpmi.szDevice = "dEvIcE 1";

                    return (BOOL)true;
                });

        uint pdwNumberOfPhysicalMonitors = 1;
        _monitorApiMock
            .Setup(m => m.GetNumberOfPhysicalMonitorsFromHMONITOR((HMONITOR)hMonitorValue, out pdwNumberOfPhysicalMonitors))
            .Returns(true);

        _monitorApiMock
            .Setup(m => m.GetPhysicalMonitorsFromHMONITOR((HMONITOR)hMonitorValue, It.IsAny<PHYSICAL_MONITOR[]>()))
            .Returns(
                 (HMONITOR hMonitor, PHYSICAL_MONITOR[] pPhysicalMonitorArray) =>
                 {
                    Assert.Single(pPhysicalMonitorArray);
                    pPhysicalMonitorArray[0].szPhysicalMonitorDescription = "dEsCrIpTiOn 1";
                    pPhysicalMonitorArray[0].hPhysicalMonitor = (HANDLE)(nint)0x97531;
                    return (BOOL)true;
                 });

        var monitorService = new MonitorService(_monitorApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Collection(monitors, monitor =>
        {
            Assert.Equal("dEvIcE 1", monitor.Device);
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Equal((nint)0x97531u, monitor.Handle.DangerousGetHandle());
        });
    }
}
