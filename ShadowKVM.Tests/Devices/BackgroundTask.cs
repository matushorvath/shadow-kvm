using Moq;
using Serilog;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Windows.Win32.Foundation;

namespace ShadowKVM.Tests;

public class BackgroundTaskTests
{
    Mock<IDeviceNotificationService> _deviceNotificationServiceMock = new();
    Mock<IMonitorService> _monitorServiceMock = new();
    Mock<IWindowsAPI> _windowsAPIMock = new();
    Mock<ILogger> _loggerMock = new();

    Guid _testGuid = new("{3f527904-28d8-4cda-b1c3-08cca9dc3dff}");
    SafePhysicalMonitorHandle _testHandle = new SafePhysicalMonitorHandle(null!, (HANDLE)12345u, false);

    (Mock<IDeviceNotification>, Channel<IDeviceNotification.Action>) SetupNotification()
    {
        // Registering the notification will return a mock notification
        var notificationMock = new Mock<IDeviceNotification>();

        _deviceNotificationServiceMock
            .Setup(m => m.Register(_testGuid))
            .Returns(notificationMock.Object)
            .Verifiable();

        // The mock notification will pass data to this channe
        var channel = Channel.CreateUnbounded<IDeviceNotification.Action>();

        notificationMock
            .Setup(m => m.Reader)
            .Returns(channel.Reader)
            .Verifiable();

        return (notificationMock, channel);
    }

    [Fact]
    public void Restart_WorksFirstTime()
    {
        var (notificationMock, channel) = SetupNotification();

        // The task will be restarted with this config
        var config = new Config { TriggerDevice = new() { Raw = _testGuid } };

        // The task will forever wait on channel.Reader, which will never read anything
        // since we never write anything to the channel
        var backgroundTask = new BackgroundTask(_deviceNotificationServiceMock.Object,
            _monitorServiceMock.Object, _windowsAPIMock.Object, _loggerMock.Object);
        backgroundTask.Restart(config);

        // Task was originally stopped, so we don't expect first two log messages
        _loggerMock.Verify(m => m.Debug("Stopping background task"), Times.Never);
        _loggerMock.Verify(m => m.Debug("Starting background task"), Times.Once);

        backgroundTask.Dispose();
        Assert.Null(backgroundTask._task);

        _deviceNotificationServiceMock.Verify();
        notificationMock.Verify();
    }

    [Fact]
    public void Restart_WorksSecondTime()
    {
        var (notificationMock, channel) = SetupNotification();

        // The task will be restarted with this config
        var config = new Config { TriggerDevice = new() { Raw = _testGuid } };

        // The task will forever wait on channel.Reader, which will never read anything
        // since we never write anything to the channel
        var backgroundTask = new BackgroundTask(_deviceNotificationServiceMock.Object,
            _monitorServiceMock.Object, _windowsAPIMock.Object, _loggerMock.Object);
        backgroundTask.Restart(config);

        // Task was originally stopped, so we don't expect first two log messages
        _loggerMock.Verify(m => m.Debug("Stopping background task"), Times.Never);
        _loggerMock.Verify(m => m.Debug("Starting background task"), Times.Once);

        backgroundTask.Restart(config);

        _loggerMock.Verify(m => m.Debug("Stopping background task"), Times.Once);
        _loggerMock.Verify(m => m.Debug("Background task stopped"), Times.Once);

        backgroundTask.Dispose();
        Assert.Null(backgroundTask._task);

        _deviceNotificationServiceMock.Verify();
        notificationMock.Verify();
    }

    [Fact]
    public void ProcessNotification_OneMonitor()
    {
        var (notificationMock, channel) = SetupNotification();

        _monitorServiceMock
            .Setup(m => m.LoadMonitors())
            .Returns(new Monitors() {
                new() { Description = "dEsCrIpTiOn 1", Handle = _testHandle }
            })
            .Verifiable();

        // The task will be restarted with this config
        var config = new Config
        {
            TriggerDevice = new() { Raw = _testGuid },
            Monitors = new()
            {
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Attach = new () { Code = new(0x42), Value = new (0x98) }
                }
            }
        };

        // The task is expected to call SetVCPFeature for each monitor (asynchronously)
        var setVcpFeatureCalled = new ManualResetEvent(false);
        _windowsAPIMock
            .Setup(m => m.SetVCPFeature(It.IsAny<SafeHandle>(), It.IsAny<byte>(), It.IsAny<uint>()))
            .Returns(
                (SafeHandle hMonitor, byte bVCPCode, uint dwNewValue) =>
                {
                    Assert.Equal(_testHandle.DangerousGetHandle(), hMonitor.DangerousGetHandle());
                    Assert.Equal(0x42, bVCPCode);
                    Assert.Equal(0x98u, dwNewValue);

                    setVcpFeatureCalled.Set();
                    return 1;
                }
            )
            .Verifiable();

        var backgroundTask = new BackgroundTask(_deviceNotificationServiceMock.Object,
            _monitorServiceMock.Object, _windowsAPIMock.Object, _loggerMock.Object);
        backgroundTask.Restart(config);

        // Task is now running, send it an action
        channel.Writer.TryWrite(IDeviceNotification.Action.Arrival);

        // Wait for the background task to call SetVCPFeature
        setVcpFeatureCalled.WaitOne();

        _monitorServiceMock.Verify();
        _deviceNotificationServiceMock.Verify();
        notificationMock.Verify();
    }
}
