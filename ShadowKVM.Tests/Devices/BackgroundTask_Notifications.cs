using Moq;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace ShadowKVM.Tests;

public class BackgroundTask_NotificationsTests : BackgroundTaskFixture
{
    [Fact]
    public void ProcessNotifications_IgnoreWhenDisabled()
    {
        // The mock notification will pass data to this channel
        var channel = Channel.CreateUnbounded<IDeviceNotification.Action>();
        var notificationMock = SetupNotification(channel);

        // Synchronize with log messages that happen in another thread
        var startedEvent = new AutoResetEvent(false);
        _loggerMock
            .Setup(m => m.Debug("Background task started"))
            .Callback(() => startedEvent.Set());

        var processedEvent = new AutoResetEvent(false);
        _loggerMock
            .Setup(m => m.Debug(
                It.Is<string>(s => s.StartsWith("Ignoring device notification while disabled")),
                It.IsAny<IDeviceNotification.Action>()))
            .Callback(() => processedEvent.Set());

        var config = new Config
        {
            TriggerDevice = new() { Raw = _testGuid },
            Monitors = new()
            {
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Attach = new () { Code = new(17), Value = new (98) }
                }
            }
        };

        using (var backgroundTask = new BackgroundTask(_deviceNotificationServiceMock.Object,
            _monitorServiceMock.Object, _windowsAPIMock.Object, _loggerMock.Object))
        {
            backgroundTask.Restart(config);

            // Make the test more challenging by waiting for the task to actually start before disabling it
            Assert.True(startedEvent.WaitOne(TimeSpan.FromSeconds(5)));

            backgroundTask.Enabled = false;

            // Task is now running but disabled, send it an action
            channel.Writer.TryWrite(IDeviceNotification.Action.Arrival);

            // Wait for the action to be processed (by ignoring it)
            Assert.True(processedEvent.WaitOne(TimeSpan.FromSeconds(5)));
        }

        // Don't expect any activity when background task is disabled
        _loggerMock.Verify(m => m.Debug(
            It.Is<string>(s => s.StartsWith("Ignoring device notification while disabled")),
            It.IsAny<IDeviceNotification.Action>()));

        _monitorServiceMock.Verify(m => m.LoadMonitors(), Times.Never);
        _windowsAPIMock.Verify(m => m.SetVCPFeature(It.IsAny<SafeHandle>(), It.IsAny<byte>(), It.IsAny<uint>()), Times.Never);

        _monitorServiceMock.Verify();
        _deviceNotificationServiceMock.Verify();
        notificationMock.Verify();
    }

    [Fact]
    public void ProcessNotifications_IgnoreDuplicateAction()
    {
        // The mock notification will pass data to this channel
        var channel = Channel.CreateUnbounded<IDeviceNotification.Action>();
        var notificationMock = SetupNotification(channel);

        // Return no monitor devices to simplify the test
        _monitorServiceMock
            .Setup(m => m.LoadMonitors())
            .Returns([])
            .Verifiable();

        // Synchronize with log messages that happen in another thread
        var startedEvent = new AutoResetEvent(false);
        _loggerMock
            .Setup(m => m.Debug("Background task started"))
            .Callback(() => startedEvent.Set());

        var processedEvent = new AutoResetEvent(false);
        _loggerMock
            .Setup(m => m.Debug(
                It.Is<string>(s => s.StartsWith("Ignoring duplicate device notification")),
                It.IsAny<IDeviceNotification.Action>()))
            .Callback(() => processedEvent.Set());

        var config = new Config
        {
            TriggerDevice = new() { Raw = _testGuid },
            Monitors = new()
            {
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Attach = new () { Code = new(17), Value = new (98) }
                }
            }
        };

        using (var backgroundTask = new BackgroundTask(_deviceNotificationServiceMock.Object,
            _monitorServiceMock.Object, _windowsAPIMock.Object, _loggerMock.Object))
        {
            backgroundTask.Restart(config);

            // Task is now running, send it an action, then the same action again
            channel.Writer.TryWrite(IDeviceNotification.Action.Arrival);
            channel.Writer.TryWrite(IDeviceNotification.Action.Arrival);

            // Wait for the second duplicate action to be processed (by ignoring it)
            Assert.True(processedEvent.WaitOne(TimeSpan.FromSeconds(5)));
        }

        // Expect a log message about ignoring the second action
        _loggerMock.Verify(m => m.Debug(
            It.Is<string>(s => s.StartsWith("Ignoring duplicate device notification")),
            It.IsAny<IDeviceNotification.Action>()));

        // Two actions, but LoadMonitors should have been only called for the first one
        _monitorServiceMock.Verify(m => m.LoadMonitors(), Times.Once);

        _monitorServiceMock.Verify();
        _deviceNotificationServiceMock.Verify();
        notificationMock.Verify();
    }

    [Fact]
    public void ProcessNotifications_FailsWhenRegisterThrows()
    {
        _deviceNotificationServiceMock
            .Setup(m => m.Register(_testGuid))
            .Throws(new Exception("rEgIsTeR"))
            .Verifiable();

        // Synchronize with log messages that happen in another thread
        var failedEvent = new AutoResetEvent(false);
        _loggerMock
            .Setup(m => m.Warning(
                It.Is<string>(s => s.StartsWith("Background task failed")),
                It.IsAny<Exception>()))
            .Callback(() => failedEvent.Set());

        using (var backgroundTask = new BackgroundTask(_deviceNotificationServiceMock.Object,
            _monitorServiceMock.Object, _windowsAPIMock.Object, _loggerMock.Object))
        {
            backgroundTask.Restart(new() { TriggerDevice = { Raw = _testGuid } });

            // Wait for the task to fail as a reaction to IDeviceNotification.Register throwing
            Assert.True(failedEvent.WaitOne(TimeSpan.FromSeconds(5)));
        }

        // Expect a log message about ignoring the second action
        _loggerMock.Verify(m => m.Warning(
            It.Is<string>(s => s.StartsWith("Background task failed")),
            It.IsAny<Exception>()));

        _monitorServiceMock.Verify();
        _deviceNotificationServiceMock.Verify();
    }

    [Fact]
    public async Task ProcessNotifications_ReturnsOnClosedChannel()
    {
        // This test mostly exists to achieve 100% coverage of this class,
        // in real use the channel will never be closed

        // The mock notification will pass data to this channel
        var channel = Channel.CreateUnbounded<IDeviceNotification.Action>();
        var notificationMock = SetupNotification(channel);

        var config = new Config
        {
            TriggerDevice = new() { Raw = _testGuid }
        };

        using (var backgroundTask = new BackgroundTask(_deviceNotificationServiceMock.Object,
            _monitorServiceMock.Object, _windowsAPIMock.Object, _loggerMock.Object))
        {
            backgroundTask.Restart(config);

            // Task is now running, close the channel
            channel.Writer.Complete();

            // Wait for the task to close cleanly
            Assert.NotNull(backgroundTask._task);
            await backgroundTask._task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(backgroundTask._task.IsCompletedSuccessfully);
        }

        _monitorServiceMock.Verify();
        _deviceNotificationServiceMock.Verify();
        notificationMock.Verify();
    }
}
