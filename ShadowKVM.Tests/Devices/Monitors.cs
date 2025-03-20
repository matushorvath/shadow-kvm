using System.Collections;
using Moq;
using Windows.Win32.Foundation;

namespace ShadowKVM.Tests;

public class MonitorsTests
{
    Mock<IMonitorAPI> _monitorApiMock = new();

    [Fact]
    public void IEnumerable()
    {
        IEnumerable enumerable = new Monitors
        {
            new Monitor
            {
                Device = "dEvIcEnAmE 1",
                Description = "dEsCrIpTiOn 1",
                Adapter = "aDaPtEr 1",
                SerialNumber = "sErIaL 1",
                Handle = new SafePhysicalMonitorHandle(_monitorApiMock.Object, (HANDLE)54321u, false)
            },
            new Monitor
            {
                Device = "dEvIcEnAmE 2",
                Description = "dEsCrIpTiOn 2",
                Adapter = "aDaPtEr 2",
                SerialNumber = "sErIaL 2",
                Handle = new SafePhysicalMonitorHandle(_monitorApiMock.Object, (HANDLE)65432u, false)
            },
            new Monitor
            {
                Device = "dEvIcEnAmE 3",
                Description = "dEsCrIpTiOn 3",
                Adapter = "aDaPtEr 3",
                SerialNumber = "sErIaL 3",
                Handle = new SafePhysicalMonitorHandle(_monitorApiMock.Object, (HANDLE)76543u, false)
            }
        };

        var index = 0;
        foreach (var item in enumerable)
        {
            Assert.IsType<Monitor>(item);
            Assert.Equal($"dEvIcEnAmE {index + 1}", ((Monitor)item).Device);
            index++;
        }
    }

    [Fact]
    public void IDisposable()
    {
        _monitorApiMock.Setup(m => m.DestroyPhysicalMonitor(It.IsAny<HANDLE>())).Returns(true);

        using (var monitors = new Monitors())
        {
            monitors.Add(new Monitor
            {
                Device = "dEvIcEnAmE 1",
                Description = "dEsCrIpTiOn 1",
                Adapter = "aDaPtEr 1",
                SerialNumber = "sErIaL 1",
                Handle = new SafePhysicalMonitorHandle(_monitorApiMock.Object, (HANDLE)54321u, false)
            });
            monitors.Add(new Monitor
            {
                Device = "dEvIcEnAmE 2",
                Description = "dEsCrIpTiOn 2",
                Adapter = "aDaPtEr 2",
                SerialNumber = "sErIaL 2",
                Handle = new SafePhysicalMonitorHandle(_monitorApiMock.Object, (HANDLE)65432u, true)
            });
        };

        _monitorApiMock.Verify(m => m.DestroyPhysicalMonitor((HANDLE)54321u), Times.Never());
        _monitorApiMock.Verify(m => m.DestroyPhysicalMonitor((HANDLE)65432u), Times.Once());
    }
}
