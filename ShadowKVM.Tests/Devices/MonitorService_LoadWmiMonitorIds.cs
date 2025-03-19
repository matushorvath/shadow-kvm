namespace ShadowKVM.Tests;

public class MonitorService_LoadWmiMonitorIds : MonitorServiceFixture
{
    [Fact]
    public void LoadMonitors_SelectAllWMIMonitorIDsReturnsEmpty()
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

        _monitorApiMock
            .Setup(m => m.SelectAllWMIMonitorIDs())
            .Returns([]);

        var monitorService = new MonitorService(_monitorApiMock.Object, _loggerApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Collection(monitors, monitor =>
        {
            Assert.Equal("dEvIcEnAmE 1", monitor.Device);
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Equal("aDaPtEr 1", monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
            Assert.Equal((nint)54321u, monitor.Handle.DangerousGetHandle());
        });
    }

    [Fact]
    public void LoadMonitors_SerialNumberMissing()
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

        _monitorApiMock
            .Setup(m => m.SelectAllWMIMonitorIDs())
            .Returns([new Dictionary<string, object> {
                    ["InstanceName"] = @"DISPLAY\DELA1CE\5&fc538b4&0&UID4357_0"
            }]);

        var monitorService = new MonitorService(_monitorApiMock.Object, _loggerApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Collection(monitors, monitor =>
        {
            Assert.Equal("dEvIcEnAmE 1", monitor.Device);
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Equal("aDaPtEr 1", monitor.Adapter);
            Assert.Equal(string.Empty, monitor.SerialNumber);
            Assert.Equal((nint)54321u, monitor.Handle.DangerousGetHandle());
        });
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("0", "")]
    [InlineData("\0\0\0\0\0", "")]
    [InlineData("0\0\0\0\0", "")]
    [InlineData("24680", "24680")]
    [InlineData("sErIaL 1", "sErIaL 1")]
    [InlineData("sErIaL 1\0\0\0", "sErIaL 1")]
    public void LoadMonitors_SerialNumberEmpty(string serialInput, string serialExpected)
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
            new () { serialNumber = serialInput, instanceName = @"DISPLAY\DELA1CE\5&fc538b4&0&UID4357_0" }
        ];
        SetupLoadWmiMonitorIds(loadWmiMonitorIdsData);

        var monitorService = new MonitorService(_monitorApiMock.Object, _loggerApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Collection(monitors, monitor =>
        {
            Assert.Equal("dEvIcEnAmE 1", monitor.Device);
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Equal("aDaPtEr 1", monitor.Adapter);
            Assert.Equal(serialExpected, monitor.SerialNumber);
            Assert.Equal((nint)54321u, monitor.Handle.DangerousGetHandle());
        });
    }

    [Fact]
    public void LoadMonitors_InstanceNameMissing()
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

        _monitorApiMock
            .Setup(m => m.SelectAllWMIMonitorIDs())
            .Returns([new Dictionary<string, object> { ["SerialNumber"] = "sErIaL 1" }]);

        var monitorService = new MonitorService(_monitorApiMock.Object, _loggerApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Collection(monitors, monitor =>
        {
            Assert.Equal("dEvIcEnAmE 1", monitor.Device);
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Equal("aDaPtEr 1", monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
            Assert.Equal((nint)54321u, monitor.Handle.DangerousGetHandle());
        });
    }
}
