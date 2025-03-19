using Moq;
using Windows.Win32.Graphics.Gdi;

namespace ShadowKVM.Tests;

public class MonitorService_LoadPhysicalMonitorsTests : MonitorServiceFixture
{
    [Fact]
    public void LoadMonitors_EnumDisplayMonitorsReturnsFalse()
    {
        _monitorApiMock
            .Setup(m => m.EnumDisplayMonitors(HDC.Null, null, It.IsNotNull<MONITORENUMPROC>(), 0))
            .Returns(false);

        var monitorService = new MonitorService(_monitorApiMock.Object);
        var exception = Assert.Throws<Exception>(monitorService.LoadMonitors);

        Assert.Equal("Monitor enumeration failed", exception.Message);
    }

    [Fact]
    public void LoadMonitors_GetMonitorInfoReturnsFalse()
    {
        List<LoadPhysicalMonitors_Monitor> loadPhysicalMonitorsData = [
            new () { monitorHandle = 12345, device = "dEvIcEnAmE 1",
                physicalMonitors = [
                    new () { physicalHandle = 97531, description = "dEsCrIpTiOn 1" }
                ]
            }
        ];
        SetupLoadPhysicalMonitors(loadPhysicalMonitorsData);

        _monitorApiMock
            .Setup(m => m.GetMonitorInfo(It.IsAny<HMONITOR>(), ref It.Ref<MONITORINFOEXW>.IsAny))
            .Returns(false);

        var monitorService = new MonitorService(_monitorApiMock.Object);
        var exception = Assert.Throws<Exception>(monitorService.LoadMonitors);

        Assert.Equal("Getting monitor information failed", exception.Message);
    }
}
