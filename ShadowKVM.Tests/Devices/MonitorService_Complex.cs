using Moq;

namespace ShadowKVM.Tests;

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

        var monitorService = new MonitorService(_windowsAPIMock.Object, _loggerApiMock.Object);
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

    [Theory]
    [InlineData(
        @"\\?\iNvAlId#DELA1CE#5&fc538b4&0&UID4357#{5f310f81-8c58-4028-a7b8-564cb8324c94}",
        @"DISPLAY\DELA1CE\5&fc538b4&0&UID4357_0",
        "Could not parse display device \"{DevId}\"",
        @"\\?\iNvAlId#DELA1CE#5&fc538b4&0&UID4357#{5f310f81-8c58-4028-a7b8-564cb8324c94}")]
    [InlineData(
        @"\\?\DISPLAY#DELA1CE#5&fc538b4&0&UID4357#{5f310f81-8c58-4028-a7b8-564cb8324c94}",
        @"iNvAlId\DELA1CE\5&fc538b4&0&UID4357_0",
        "Could not parse WMI monitor id \"{WmiId}\"",
        @"iNvAlId\DELA1CE\5&fc538b4&0&UID4357_0")]
    [InlineData(
        @"\\?\DISPLAY#DELA1CE#5&fc538b4&0&UID4357#{5f310f81-8c58-4028-a7b8-564cb8324c94}",
        @"DISPLAY\iNvAlId\5&fc538b4&0&UID4357_0",
        null,
        null)]
    [InlineData(
        @"\\?\DISPLAY#DELA1CE#5&fc538b4&0&UID4357#{5f310f81-8c58-4028-a7b8-564cb8324c94}",
        @"DISPLAY\DELA1CE\iNvAlId_0",
        null,
        null)]
    public void LoadMonitors_MismatchedId(string devId, string wmiId, string? warningMessage, string? warningParam)
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
                    new () { deviceID = devId }
                ]
            }
        ];
        SetupLoadDisplayDevices(loadDisplayDevicesData);

        List<LoadWmiMonitorIds_Data> loadWmiMonitorIdsData = [
            new () { serialNumber = "sErIaL 1", instanceName = wmiId }
        ];
        SetupLoadWmiMonitorIds(loadWmiMonitorIdsData);

        var monitorService = new MonitorService(_windowsAPIMock.Object, _loggerApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        if (warningMessage != null)
        {
            _loggerApiMock.Verify(m => m.Warning(warningMessage, warningParam));
        }
        else
        {
            _loggerApiMock.Verify(m => m.Warning(
                It.Is<string>(s => s.StartsWith("Could not parse ")),
                It.IsAny<object>()), Times.Never());
        }

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
