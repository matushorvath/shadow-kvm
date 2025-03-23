using System.Collections;
using Moq;
using Windows.Win32.Foundation;

namespace ShadowKVM.Tests;

public class MonitorsTests
{
    Mock<IWindowsAPI> _windowsAPIMock = new();

    [Fact]
    public void IEnumerable()
    {
        IEnumerable enumerable = new Monitors
        {
            new()
            {
                Description = "dEsCrIpTiOn 1",
                Adapter = "aDaPtEr 1",
                SerialNumber = "sErIaL 1",
                Handle = new SafePhysicalMonitorHandle(_windowsAPIMock.Object, (HANDLE)54321u, false)
            },
            new()
            {
                Description = "dEsCrIpTiOn 2",
                Adapter = "aDaPtEr 2",
                SerialNumber = "sErIaL 2",
                Handle = new SafePhysicalMonitorHandle(_windowsAPIMock.Object, (HANDLE)65432u, false)
            },
            new()
            {
                Description = "dEsCrIpTiOn 3",
                Adapter = "aDaPtEr 3",
                SerialNumber = "sErIaL 3",
                Handle = new SafePhysicalMonitorHandle(_windowsAPIMock.Object, (HANDLE)76543u, false)
            }
        };

        var index = 0;
        foreach (var item in enumerable)
        {
            Assert.IsType<Monitor>(item);
            Assert.Equal($"dEsCrIpTiOn {index + 1}", ((Monitor)item).Description);
            index++;
        }
    }

    [Fact]
    public void IDisposable()
    {
        _windowsAPIMock.Setup(m => m.DestroyPhysicalMonitor(It.IsAny<HANDLE>())).Returns(true);

        using (var monitors = new Monitors())
        {
            monitors.Add(new()
            {
                Description = "dEsCrIpTiOn 1",
                Adapter = "aDaPtEr 1",
                SerialNumber = "sErIaL 1",
                Handle = new SafePhysicalMonitorHandle(_windowsAPIMock.Object, (HANDLE)54321u, false)
            });
            monitors.Add(new()
            {
                Description = "dEsCrIpTiOn 2",
                Adapter = "aDaPtEr 2",
                SerialNumber = "sErIaL 2",
                Handle = new SafePhysicalMonitorHandle(_windowsAPIMock.Object, (HANDLE)65432u, true)
            });
        }

        _windowsAPIMock.Verify(m => m.DestroyPhysicalMonitor((HANDLE)54321u), Times.Never());
        _windowsAPIMock.Verify(m => m.DestroyPhysicalMonitor((HANDLE)65432u), Times.Once());
    }
}
