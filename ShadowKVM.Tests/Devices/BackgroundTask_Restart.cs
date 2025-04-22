using System.Threading.Channels;
using Moq;

namespace ShadowKVM.Tests;

public class BackgroundTask_RestartTests : BackgroundTaskFixture
{
    [Fact]
    public void Restart_WorksFirstTime()
    {
        // The mock notification will pass data to this channel
        var channel = Channel.CreateUnbounded<IDeviceNotification.Action>();
        var notificationMock = SetupNotification(channel);

        // Synchronize with log messages that happen in another thread
        var startedEvent = new AutoResetEvent(false);
        _loggerMock
            .Setup(m => m.Debug("Background task started"))
            .Callback(() => startedEvent.Set());

        var stoppedEvent = new AutoResetEvent(false);
        _loggerMock
            .Setup(m => m.Debug("Background task stopped"))
            .Callback(() => stoppedEvent.Set());

        // The task will be restarted with this config
        _configServiceMock
            .SetupGet(m => m.Config)
            .Returns(new Config { TriggerDevice = new() { Raw = _testGuid } });

        // The task will forever wait on channel.Reader, which will never read anything
        // since we never write anything to the channel
        using (var backgroundTask = new BackgroundTask(
            _configServiceMock.Object, _deviceNotificationServiceMock.Object,
            _monitorServiceMock.Object, _windowsAPIMock.Object, _loggerMock.Object))
        {
            backgroundTask.Restart();

            // Wait for the other thread to react
            Assert.True(startedEvent.WaitOne(TimeSpan.FromSeconds(5)));

            // Task was originally stopped, so we don't expect the "stop" log messages
            _loggerMock.Verify(m => m.Debug("Stopping background task"), Times.Never);
            _loggerMock.Verify(m => m.Debug("Background task stopped"), Times.Never);
            _loggerMock.Verify(m => m.Debug("Starting background task"), Times.Once);
            _loggerMock.Verify(m => m.Debug("Background task started"), Times.Once);

            // Explicitly dispose, so we can check that _task is null
            backgroundTask.Dispose();
            Assert.Null(backgroundTask._task);
        }

        Assert.True(stoppedEvent.WaitOne(TimeSpan.FromSeconds(5)));

        // Dispose will stop the task
        _loggerMock.Verify(m => m.Debug("Background task stopped"), Times.Once);

        _monitorServiceMock.Verify();
        _deviceNotificationServiceMock.Verify();
        notificationMock.Verify();
    }

    [Fact]
    public void Restart_WorksSecondTime()
    {
        // The mock notification will pass data to this channel
        var channel = Channel.CreateUnbounded<IDeviceNotification.Action>();
        var notificationMock = SetupNotification(channel);

        // Synchronize with log messages that happen in another thread
        var startedEvent = new AutoResetEvent(false);
        _loggerMock
            .Setup(m => m.Debug("Background task started"))
            .Callback(() => startedEvent.Set());

        var stoppedEvent = new AutoResetEvent(false);
        _loggerMock
            .Setup(m => m.Debug("Background task stopped"))
            .Callback(() => stoppedEvent.Set());

        // The task will be restarted with this config
        _configServiceMock
            .SetupGet(m => m.Config)
            .Returns(new Config { TriggerDevice = new() { Raw = _testGuid } });

        // The task will forever wait on channel.Reader, which will never read anything
        // since we never write anything to the channel
        using (var backgroundTask = new BackgroundTask(
            _configServiceMock.Object, _deviceNotificationServiceMock.Object,
            _monitorServiceMock.Object, _windowsAPIMock.Object, _loggerMock.Object))
        {
            backgroundTask.Restart();

            // Wait for the other thread to react
            Assert.True(startedEvent.WaitOne(TimeSpan.FromSeconds(5)));

            // Task was originally stopped, so we don't expect the "stop" log messages
            _loggerMock.Verify(m => m.Debug("Stopping background task"), Times.Never);
            _loggerMock.Verify(m => m.Debug("Background task stopped"), Times.Never);
            _loggerMock.Verify(m => m.Debug("Starting background task"), Times.Once);
            _loggerMock.Verify(m => m.Debug("Background task started"), Times.Once);

            // Restart second time
            backgroundTask.Restart();

            // Wait for the other thread to react
            Assert.True(stoppedEvent.WaitOne(TimeSpan.FromSeconds(5)));
            Assert.True(startedEvent.WaitOne(TimeSpan.FromSeconds(5)));

            // This time the task was running, so we also expect the "stop" log messagess
            _loggerMock.Verify(m => m.Debug("Stopping background task"), Times.Once);
            _loggerMock.Verify(m => m.Debug("Background task stopped"), Times.Once);
            _loggerMock.Verify(m => m.Debug("Starting background task"), Times.Exactly(2));
            _loggerMock.Verify(m => m.Debug("Background task started"), Times.Exactly(2));

            // Explicitly dispose, so we can check that _task is null
            backgroundTask.Dispose();
            Assert.Null(backgroundTask._task);
        }

        Assert.True(stoppedEvent.WaitOne(TimeSpan.FromSeconds(5)));

        // Dispose will stop the task
        _loggerMock.Verify(m => m.Debug("Background task stopped"), Times.Exactly(2));

        _monitorServiceMock.Verify();
        _deviceNotificationServiceMock.Verify();
        notificationMock.Verify();
    }
}
