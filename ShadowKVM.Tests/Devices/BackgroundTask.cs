using Moq;
using Serilog;
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

    (Mock<IDeviceNotification>, Channel<IDeviceNotification.Action>) SetupNotification()
    {
        // Registering the notification will return a mock notification
        var notificationMock = new Mock<IDeviceNotification>();

        _deviceNotificationServiceMock
            .Setup(m => m.Register(It.IsAny<Guid>()))
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
}
