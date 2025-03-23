using Moq;
using Windows.Win32.Devices.Display;
using Windows.Win32.Graphics.Gdi;

namespace ShadowKVM.Tests;

public class MonitorService_LoadPhysicalMonitorsTests : MonitorServiceFixture
{
    [Fact]
    public void LoadPhysicalMonitors_EnumDisplayMonitorsReturnsFalse()
    {
        _windowsAPIMock
            .Setup(m => m.EnumDisplayMonitors(HDC.Null, null, It.IsNotNull<MONITORENUMPROC>(), 0))
            .Returns(false);

        var monitorService = new MonitorService(_windowsAPIMock.Object, _loggerApiMock.Object);
        var exception = Assert.Throws<Exception>(monitorService.LoadMonitors);

        Assert.Equal("Monitor enumeration failed", exception.Message);
    }

    [Fact]
    public void LoadPhysicalMonitors_GetMonitorInfoReturnsFalse()
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
            .Setup(m => m.GetMonitorInfo(It.IsAny<HMONITOR>(), ref It.Ref<MONITORINFOEXW>.IsAny))
            .Returns(false);

        var monitorService = new MonitorService(_windowsAPIMock.Object, _loggerApiMock.Object);
        var exception = Assert.Throws<Exception>(monitorService.LoadMonitors);

        Assert.Equal("Getting monitor information failed", exception.Message);
    }

    [Fact]
    public void LoadPhysicalMonitors_GetNumberOfPhysicalMonitorsReturnsFalse()
    {
        List<LoadPhysicalMonitors_Monitor> loadPhysicalMonitorsData = [
            new () { monitorHandle = 12345, device = "dEvIcEnAmE 1",
                physicalMonitors = [
                    new () { physicalHandle = 54321, description = "dEsCrIpTiOn 1" }
                ]
            }
        ];
        SetupLoadPhysicalMonitors(loadPhysicalMonitorsData);

        uint pdwNumberOfPhysicalMonitors = 0;
        _windowsAPIMock
            .Setup(m => m.GetNumberOfPhysicalMonitorsFromHMONITOR(It.IsAny<HMONITOR>(), out pdwNumberOfPhysicalMonitors))
            .Returns(false);

        var monitorService = new MonitorService(_windowsAPIMock.Object, _loggerApiMock.Object);
        var exception = Assert.Throws<Exception>(monitorService.LoadMonitors);

        Assert.Equal("Getting physical monitor number failed", exception.Message);
    }

    [Fact]
    public void LoadPhysicalMonitors_GetNumberOfPhysicalMonitorsWithNoMonitors()
    {
        List<LoadPhysicalMonitors_Monitor> loadPhysicalMonitorsData = [
            new () { monitorHandle = 12345, device = "dEvIcEnAmE 1",
                physicalMonitors = [
                    new () { physicalHandle = 54321, description = "dEsCrIpTiOn 1" }
                ]
            }
        ];
        SetupLoadPhysicalMonitors(loadPhysicalMonitorsData);

        uint pdwNumberOfPhysicalMonitors = 0;
        _windowsAPIMock
            .Setup(m => m.GetNumberOfPhysicalMonitorsFromHMONITOR(It.IsAny<HMONITOR>(), out pdwNumberOfPhysicalMonitors))
            .Returns(true);

        var monitorService = new MonitorService(_windowsAPIMock.Object, _loggerApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Empty(monitors);
    }

    [Fact]
    public void LoadPhysicalMonitors_GetPhysicalMonitorsReturnsFalse()
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
            .Setup(m => m.GetPhysicalMonitorsFromHMONITOR(It.IsAny<HMONITOR>(), It.IsAny<PHYSICAL_MONITOR[]>()))
            .Returns(false);

        var monitorService = new MonitorService(_windowsAPIMock.Object, _loggerApiMock.Object);
        var exception = Assert.Throws<Exception>(monitorService.LoadMonitors);

        Assert.Equal("Getting physical monitor information failed", exception.Message);
    }

    [Fact]
    public void LoadPhysicalMonitors_OneMonitorOnePhysical()
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

        _windowsAPIMock
            .Setup(m => m.SelectAllWMIMonitorIDs())
            .Returns([]);

        var monitorService = new MonitorService(_windowsAPIMock.Object, _loggerApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Collection(monitors, monitor =>
        {
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Null(monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
            Assert.Equal((nint)54321u, monitor.Handle.DangerousGetHandle());
        });
    }

    [Fact]
    public void LoadPhysicalMonitors_OneMonitorTwoPhysical()
    {
        List<LoadPhysicalMonitors_Monitor> loadPhysicalMonitorsData = [
            new () { monitorHandle = 12345, device = "dEvIcEnAmE 1",
                physicalMonitors = [
                    new () { physicalHandle = 54321, description = "dEsCrIpTiOn 1.1" },
                    new () { physicalHandle = 65432, description = "dEsCrIpTiOn 1.2" }
                ]
            }
        ];
        SetupLoadPhysicalMonitors(loadPhysicalMonitorsData);

        _windowsAPIMock
            .Setup(m => m.GetPhysicalMonitorsFromHMONITOR(It.IsAny<HMONITOR>(), It.IsAny<PHYSICAL_MONITOR[]>()))
            .Returns(false);

        var monitorService = new MonitorService(_windowsAPIMock.Object, _loggerApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Empty(monitors);

        _loggerApiMock.Verify(m => m.Warning("Multiple physical monitors connected via one port are not supported"));
    }

    [Fact]
    public void LoadPhysicalMonitors_TwoMonitorsOnePhysical()
    {
        List<LoadPhysicalMonitors_Monitor> loadPhysicalMonitorsData = [
            new () { monitorHandle = 12345, device = "dEvIcEnAmE 1",
                physicalMonitors = [
                    new () { physicalHandle = 54321, description = "dEsCrIpTiOn 1" }
                ]
            },
            new () { monitorHandle = 23456, device = "dEvIcEnAmE 2",
                physicalMonitors = [
                    new () { physicalHandle = 76543, description = "dEsCrIpTiOn 2" }
                ]
            }
        ];
        SetupLoadPhysicalMonitors(loadPhysicalMonitorsData);

        _windowsAPIMock
            .Setup(m => m.EnumDisplayDevices(It.IsAny<string?>(), It.IsAny<uint>(), ref It.Ref<DISPLAY_DEVICEW>.IsAny, 1))
            .Returns(false);

        _windowsAPIMock
            .Setup(m => m.SelectAllWMIMonitorIDs())
            .Returns([]);

        var monitorService = new MonitorService(_windowsAPIMock.Object, _loggerApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Collection(monitors,
        monitor =>
        {
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Null(monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
            Assert.Equal((nint)54321u, monitor.Handle.DangerousGetHandle());
        },
        monitor =>
        {
            Assert.Equal("dEsCrIpTiOn 2", monitor.Description);
            Assert.Null(monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
            Assert.Equal((nint)76543u, monitor.Handle.DangerousGetHandle());
        });
    }
}
