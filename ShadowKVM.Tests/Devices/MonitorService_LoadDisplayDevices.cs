using Moq;
using Windows.Win32.Graphics.Gdi;

namespace ShadowKVM.Tests;

public class MonitorService_LoadDisplayDevicesTests : MonitorServiceFixture
{
    [Fact]
    public void LoadMonitors_EnumDisplayDevicesReturnsFalse()
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
            .Setup(m => m.EnumDisplayDevices(It.IsAny<string?>(), It.IsAny<uint>(), ref It.Ref<DISPLAY_DEVICEW>.IsAny, 1))
            .Returns(false);

        List<LoadWmiMonitorIds_Data> loadWmiMonitorIdsData = [
            new () { serialNumber = "sErIaL 1", instanceName = @"DISPLAY\DELA1CE\5&fc538b4&0&UID4357_0" }
        ];

        SetupLoadWmiMonitorIds(loadWmiMonitorIdsData);

        var monitorService = new MonitorService(_monitorApiMock.Object, _loggerApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Collection(monitors, monitor =>
        {
            Assert.Equal("dEvIcEnAmE 1", monitor.Device);
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Null(monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
            Assert.Equal((nint)97531u, monitor.Handle.DangerousGetHandle());
        });
    }
}
