namespace ShadowKVM.Tests;

public class MonitorService_LoadWmiMonitorIds : MonitorServiceFixture
{
    [Fact]
    public void LoadMonitors_SelectAllWMIMonitorIDsReturnsEmpty()
    {
        List<LoadPhysicalMonitors_Monitor> loadPhysicalMonitorsData = [
            new () { monitorHandle = 12345, device = "dEvIcEnAmE 1",
                physicalMonitors = [
                    new () { physicalHandle = 97531, description = "dEsCrIpTiOn 1" }
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

        _monitorApiMock
            .Setup(m => m.SelectAllWMIMonitorIDs())
            .Returns([]);

        var monitorService = new MonitorService(_monitorApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Collection(monitors, monitor =>
        {
            Assert.Equal("dEvIcEnAmE 1", monitor.Device);
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Equal("aDaPtEr 1", monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
            Assert.Equal((nint)97531u, monitor.Handle.DangerousGetHandle());
        });
    }
}
