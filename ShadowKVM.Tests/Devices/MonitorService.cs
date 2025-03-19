using System.Management;
using System.Text;
using Moq;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace ShadowKVM.Tests;

public class MonitorServiceTests
{
    Mock<IMonitorAPI> _monitorApiMock = new Mock<IMonitorAPI>();

    class LoadPhysicalMonitors_Monitor
    {
        public required nint monitorHandle;
        public required string device;
        public IList<LoadPhysicalMonitors_PhysicalMonitor> physicalMonitors = new List<LoadPhysicalMonitors_PhysicalMonitor>();
    }

    class LoadPhysicalMonitors_PhysicalMonitor
    {
        public required string description;
        public required nint physicalHandle;
    }

    class LoadDisplayDevices_Adapter
    {
        public required string deviceName;
        public required string deviceString;
        public IList<LoadDisplayDevices_Monitor> monitors = new List<LoadDisplayDevices_Monitor>();
    }

    class LoadDisplayDevices_Monitor
    {
        public required string deviceID;
    }

    [Fact]
    public void LoadMonitors_FullyLoadsOneMonitor()
    {
        var loadPhysicalMonitorsData = new List<LoadPhysicalMonitors_Monitor> {
            new LoadPhysicalMonitors_Monitor {
                monitorHandle = 12345,
                device = "dEvIcEnAmE 1",
                physicalMonitors = {
                    new LoadPhysicalMonitors_PhysicalMonitor {
                        physicalHandle = 97531,
                        description = "dEsCrIpTiOn 1"
                    }
                }
            }
        };

        var loadDisplayDevicesData = new List<LoadDisplayDevices_Adapter> {
            new LoadDisplayDevices_Adapter {
                deviceName = "dEvIcEnAmE 1",
                deviceString = "aDaPtEr 1",
                monitors = {
                    new LoadDisplayDevices_Monitor {
                        deviceID = @"\\?\DISPLAY#DELA1CE#5&fc538b4&0&UID4357#{5f310f81-8c58-4028-a7b8-564cb8324c94}"
                    }
                }
            }
        };

        var loadWmiMonitorIdsData = new List<Dictionary<string, object>> {
            new Dictionary<string, object> {
                ["SerialNumberID"] = Encoding.ASCII.GetBytes("sErIaL 1\0\0".ToCharArray()).Select(b => (ushort)b).ToArray(),
                ["InstanceName"] = @"DISPLAY\DELA1CE\5&fc538b4&0&UID4357_0"
            }
        };

        SetupLoadPhysicalMonitors(loadPhysicalMonitorsData);
        SetupLoadDisplayDevices(loadDisplayDevicesData);
        SetupLoadWmiMonitorIds(loadWmiMonitorIdsData);

        var monitorService = new MonitorService(_monitorApiMock.Object);
        var monitors = monitorService.LoadMonitors();

        Assert.Collection(monitors, monitor =>
        {
            Assert.Equal("dEvIcEnAmE 1", monitor.Device);
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Equal("aDaPtEr 1", monitor.Adapter);
            Assert.Equal("sErIaL 1", monitor.SerialNumber);
            Assert.Equal((nint)97531u, monitor.Handle.DangerousGetHandle());
        });
    }

    unsafe void SetupLoadPhysicalMonitors(IList<LoadPhysicalMonitors_Monitor> results)
    {
        _monitorApiMock
            .Setup(m => m.EnumDisplayMonitors(HDC.Null, null, It.IsNotNull<MONITORENUMPROC>(), 0))
            .Returns(
                (HDC hdc, RECT? lprcClip, MONITORENUMPROC lpfnEnum, LPARAM dwData) =>
                {
                    foreach (var result in results)
                    {
                        Assert.True(lpfnEnum((HMONITOR)result.monitorHandle, HDC.Null, null, 0));
                    }

                    return (BOOL)true;
                });

        var currentResultIndex = -1;

        var getMonitorInfoInvocation = 0;
        _monitorApiMock
            .Setup(m => m.GetMonitorInfo(It.IsAny<HMONITOR>(), ref It.Ref<MONITORINFOEXW>.IsAny))
            .Returns(
                (HMONITOR hMonitor, ref MONITORINFOEXW lpmi) =>
                {
                    currentResultIndex++;
                    Assert.InRange(currentResultIndex, 0, results.Count - 1);

                    Assert.Equal(104u, lpmi.monitorInfo.cbSize); // 104 is size of MONITORINFOEXW
                    Assert.Equal(results[currentResultIndex].monitorHandle, hMonitor);

                    lpmi.szDevice = results[getMonitorInfoInvocation].device;

                    getMonitorInfoInvocation++;
                    return (BOOL)true;
                });


        uint pdwNumberOfPhysicalMonitors;
        _monitorApiMock
            .Setup(m => m.GetNumberOfPhysicalMonitorsFromHMONITOR(It.IsAny<HMONITOR>(), out pdwNumberOfPhysicalMonitors))
            .Returns(
                (HMONITOR hMonitor, out uint pdwNumberOfPhysicalMonitors) =>
                {
                    Assert.Equal(results[currentResultIndex].monitorHandle, hMonitor);
                    pdwNumberOfPhysicalMonitors = (uint)results[currentResultIndex].physicalMonitors.Count;
                    return (BOOL)true;
                }
            );


        _monitorApiMock
            .Setup(m => m.GetPhysicalMonitorsFromHMONITOR(It.IsAny<HMONITOR>(), It.IsAny<PHYSICAL_MONITOR[]>()))
            .Returns(
                 (HMONITOR hMonitor, PHYSICAL_MONITOR[] pPhysicalMonitorArray) =>
                 {
                    Assert.Equal(results[currentResultIndex].monitorHandle, hMonitor);
                    Assert.Equal(results[currentResultIndex].physicalMonitors.Count, pPhysicalMonitorArray.Length);

                    for (var physicalMonitorIndex = 0; physicalMonitorIndex < results[currentResultIndex].physicalMonitors.Count; physicalMonitorIndex++)
                    {
                        var physicalMonitor = results[currentResultIndex].physicalMonitors[physicalMonitorIndex];
                        pPhysicalMonitorArray[physicalMonitorIndex].szPhysicalMonitorDescription = physicalMonitor.description;
                        pPhysicalMonitorArray[physicalMonitorIndex].hPhysicalMonitor = (HANDLE)physicalMonitor.physicalHandle;
                    }

                    return (BOOL)true;
                 });
    }

    void SetupLoadDisplayDevices(List<LoadDisplayDevices_Adapter> adapters)
    {
        _monitorApiMock
            .Setup(m => m.EnumDisplayDevices(It.IsAny<string?>(), It.IsAny<uint>(), ref It.Ref<DISPLAY_DEVICEW>.IsAny, 1))
            .Returns(
                (string? lpDevice, uint iDevNum, ref DISPLAY_DEVICEW lpDisplayDevice, uint dwFlags) =>
                {
                    Assert.Equal(840u, lpDisplayDevice.cb); // 840 is size of DISPLAY_DEVICEW

                    if (lpDevice == null)
                    {
                        // Enum adapters
                        Assert.InRange((int)iDevNum, 0, adapters.Count);

                        if (iDevNum >= adapters.Count)
                        {
                            return (BOOL)false;
                        }

                        lpDisplayDevice.DeviceName = adapters[(int)iDevNum].deviceName;
                        lpDisplayDevice.DeviceString = adapters[(int)iDevNum].deviceString;

                        return (BOOL)true;
                    }
                    else
                    {
                        // Enum monitors
                        var monitors = (from a in adapters where a.deviceName == lpDevice select a).Single().monitors;

                        Assert.InRange((int)iDevNum, 0, monitors.Count);

                        if (iDevNum >= monitors.Count)
                        {
                            return (BOOL)false;
                        }

                        lpDisplayDevice.DeviceID = monitors[(int)iDevNum].deviceID;

                        return (BOOL)true;

                    }
                });
    }

    void SetupLoadWmiMonitorIds(List<Dictionary<string, object>> data)
    {
        _monitorApiMock
            .Setup(m => m.SelectAllWMIMonitorIDs())
            .Returns(data);
    }
}
