using Moq;
using Windows.Win32.Graphics.Gdi;

namespace ShadowKVM.Tests;

public class MonitorService_LoadDisplayDevicesTests : MonitorServiceFixture
{
    [Fact]
    public void LoadDisplayDevices_EnumDisplayDevicesReturnsFalse()
    {
        List<LoadPhysicalMonitors_Monitor> loadPhysicalMonitorsData = [
            new () { monitorHandle = 12345, device = "dEvIcEnAmE 1",
                physicalMonitors = [
                    new () { physicalHandle = 54321, description = "dEsCrIpTiOn 1" }
                ]
            }
        ];
        SetupLoadPhysicalMonitors(loadPhysicalMonitorsData);

        _windowsAPIMock
            .Setup(m => m.EnumDisplayDevices(It.IsAny<string?>(), It.IsAny<uint>(), ref It.Ref<DISPLAY_DEVICEW>.IsAny, 1))
            .Returns(false);

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
            Assert.Null(monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
            Assert.Equal((nint)54321u, monitor.Handle.DangerousGetHandle());
        });
    }

    [Fact]
    public void LoadDisplayDevices_OneAdapterOneMonitor()
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

        _windowsAPIMock
            .Setup(m => m.SelectAllWMIMonitorIDs())
            .Returns([]);

        var monitorService = new MonitorService(_windowsAPIMock.Object, _loggerApiMock.Object);
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
    public void LoadDisplayDevices_OneAdapterNoMonitors()
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
            new () { deviceName = "dEvIcEnAmE 1", deviceString = "aDaPtEr 1", monitors = [] }
        ];

        SetupLoadDisplayDevices(loadDisplayDevicesData);

        _windowsAPIMock
            .Setup(m => m.SelectAllWMIMonitorIDs())
            .Returns([]);

        var monitorService = new MonitorService(_windowsAPIMock.Object, _loggerApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Collection(monitors, monitor =>
        {
            Assert.Equal("dEvIcEnAmE 1", monitor.Device);
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Null(monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
            Assert.Equal((nint)54321u, monitor.Handle.DangerousGetHandle());
        });
    }

    [Fact]
    public void LoadDisplayDevices_OneAdapterTwoMonitors()
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
                    new () { deviceID = @"\\?\DISPLAY#DELA1CE#5&fc538b4&0&UID4357#{5f310f81-8c58-4028-a7b8-564cb8324c94}" },
                    new () { deviceID = @"\\?\DISPLAY#DELA1CE#5&fc538b4&0&UID4357#{5f310f81-8c58-4028-a7b8-564cb8324c95}" }
                ]
            }
        ];

        SetupLoadDisplayDevices(loadDisplayDevicesData);

        _windowsAPIMock
            .Setup(m => m.SelectAllWMIMonitorIDs())
            .Returns([]);

        var monitorService = new MonitorService(_windowsAPIMock.Object, _loggerApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Collection(monitors, monitor =>
        {
            Assert.Equal("dEvIcEnAmE 1", monitor.Device);
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            // TODO we could still map to the adapter, but not to serial number
            Assert.Null(monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
            Assert.Equal((nint)54321u, monitor.Handle.DangerousGetHandle());
        });

        _loggerApiMock.Verify(m => m.Warning("Multiple monitor devices connected via one adapter device are not supported"));
    }

    [Fact]
    public void LoadDisplayDevices_TwoAdaptersOneMonitor()
    {
        List<LoadPhysicalMonitors_Monitor> loadPhysicalMonitorsData = [
            new () { monitorHandle = 12345, device = "dEvIcEnAmE 1",
                physicalMonitors = [
                    new () { physicalHandle = 54321, description = "dEsCrIpTiOn 1" }
                ]
            },
            new () { monitorHandle = 23456, device = "dEvIcEnAmE 2",
                physicalMonitors = [
                    new () { physicalHandle = 65432, description = "dEsCrIpTiOn 2" },
                ]
            }
        ];
        SetupLoadPhysicalMonitors(loadPhysicalMonitorsData);

        List<LoadDisplayDevices_Adapter> loadDisplayDevicesData = [
            new () { deviceName = "dEvIcEnAmE 1", deviceString = "aDaPtEr 1",
                monitors = [
                    new () { deviceID = @"\\?\DISPLAY#DELA1CE#5&fc538b4&0&UID4357#{5f310f81-8c58-4028-a7b8-564cb8324c94}" }
                ]
            },
            new () { deviceName = "dEvIcEnAmE 2", deviceString = "aDaPtEr 2",
                monitors = [
                    new () { deviceID = @"\\?\DISPLAY#DELA1CE#5&fc538b4&0&UID4357#{5f310f81-8c58-4028-a7b8-564cb8324c95}" }
                ]
            }
        ];

        SetupLoadDisplayDevices(loadDisplayDevicesData);

        _windowsAPIMock
            .Setup(m => m.SelectAllWMIMonitorIDs())
            .Returns([]);

        var monitorService = new MonitorService(_windowsAPIMock.Object, _loggerApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Collection(monitors,
        monitor =>
        {
            Assert.Equal("dEvIcEnAmE 1", monitor.Device);
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Equal("aDaPtEr 1", monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
            Assert.Equal((nint)54321u, monitor.Handle.DangerousGetHandle());
        },
        monitor =>
        {
            Assert.Equal("dEvIcEnAmE 2", monitor.Device);
            Assert.Equal("dEsCrIpTiOn 2", monitor.Description);
            Assert.Equal("aDaPtEr 2", monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
            Assert.Equal((nint)65432u, monitor.Handle.DangerousGetHandle());
        });
    }
}
