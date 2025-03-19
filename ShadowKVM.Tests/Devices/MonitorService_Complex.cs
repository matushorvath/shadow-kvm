namespace ShadowKVM.Tests;

// TODO test with serialNumber = "sErIaL\0\0\0"
// TODO test matching, with not found and found
// TODO test with zero serial
// TODO fully load multiple monitors

public class MonitorService_ComplexTests : MonitorServiceFixture
{
    [Fact]
    public void LoadMonitors_FullyLoadsOneMonitor()
    {
        List<LoadPhysicalMonitors_Monitor> loadPhysicalMonitorsData = [
            new () { monitorHandle = 12345, device = "dEvIcEnAmE 1",
                physicalMonitors = [
                    new () { physicalHandle = 54321, description = "dEsCrIpTiOn 1" }
                ]
            }
        ];
        SetupLoadPhysicalMonitors(loadPhysicalMonitorsData);

        List<LoadDisplayDevices_Adapter> loadDisplayDevicesData = [
            new () { deviceName = "dEvIcEnAmE 1", deviceString = "aDaPtEr 1",
                monitors = [
                    new () { deviceID = @"\\?\DISPLAY#DELA1CE#5&fc538b4&0&UID4357#{5f310f81-8c58-4028-a7b8-564cb8324c94}" }
                ]
            }
        ];
        SetupLoadDisplayDevices(loadDisplayDevicesData);

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
            Assert.Equal("aDaPtEr 1", monitor.Adapter);
            Assert.Equal("sErIaL 1", monitor.SerialNumber);
            Assert.Equal((nint)54321u, monitor.Handle.DangerousGetHandle());
        });
    }
}
