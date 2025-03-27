using Moq;
using Serilog;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Windows.Win32.Foundation;

namespace ShadowKVM.Tests;

// TODO BackgroundTask tests
// matching using description, adapter, serial - combinations
//   missing description, adapter, serial in config (should be ignored)
//   missing description, adapter, serial in device (still tries to match)
//   missing description, adapter, serial in both
// matches multiple monitors with multiple configs
// matches one monitor with multiple configs, with different vcp code/value
// matches multiple monitors with one config

public class BackgroundTaskTests
{
    Mock<IDeviceNotificationService> _deviceNotificationServiceMock = new();
    Mock<IMonitorService> _monitorServiceMock = new();
    Mock<IWindowsAPI> _windowsAPIMock = new();
    Mock<ILogger> _loggerMock = new();

    Guid _testGuid = new("{3f527904-28d8-4cda-b1c3-08cca9dc3dff}");

    Mock<IDeviceNotification> SetupNotification(Channel<IDeviceNotification.Action> channel)
    {
        // Registering the notification will return a mock notification
        var notificationMock = new Mock<IDeviceNotification>();

        _deviceNotificationServiceMock
            .Setup(m => m.Register(_testGuid))
            .Returns(notificationMock.Object)
            .Verifiable();

        notificationMock
            .Setup(m => m.Reader)
            .Returns(channel.Reader)
            .Verifiable();

        return notificationMock;
    }

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
        var config = new Config { TriggerDevice = new() { Raw = _testGuid } };

        // The task will forever wait on channel.Reader, which will never read anything
        // since we never write anything to the channel
        using (var backgroundTask = new BackgroundTask(_deviceNotificationServiceMock.Object,
            _monitorServiceMock.Object, _windowsAPIMock.Object, _loggerMock.Object))
        {
            backgroundTask.Restart(config);

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
        var config = new Config { TriggerDevice = new() { Raw = _testGuid } };

        // The task will forever wait on channel.Reader, which will never read anything
        // since we never write anything to the channel
        using (var backgroundTask = new BackgroundTask(_deviceNotificationServiceMock.Object,
            _monitorServiceMock.Object, _windowsAPIMock.Object, _loggerMock.Object))
        {
            backgroundTask.Restart(config);

            // Wait for the other thread to react
            Assert.True(startedEvent.WaitOne(TimeSpan.FromSeconds(5)));

            // Task was originally stopped, so we don't expect the "stop" log messages
            _loggerMock.Verify(m => m.Debug("Stopping background task"), Times.Never);
            _loggerMock.Verify(m => m.Debug("Background task stopped"), Times.Never);
            _loggerMock.Verify(m => m.Debug("Starting background task"), Times.Once);
            _loggerMock.Verify(m => m.Debug("Background task started"), Times.Once);

            // Restart second time
            backgroundTask.Restart(config);

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

    static SafePhysicalMonitorHandle H(nuint value) => new SafePhysicalMonitorHandle(null!, (HANDLE)value, false);

    record TestDatum(
        Monitors monitorDevices,
        MonitorConfig[]? monitorConfigs,
        IDeviceNotification.Action action,
        Dictionary<nint, SetVCPFeatureInvocation> invocations);

    static Dictionary<string, TestDatum> TestData => new()
    {
        ["attach one monitor"] = new(
            new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x12345u) }
            },
            [
                new MonitorConfig
                {
                    Description = "dEsCrIpTiOn 1",
                    Attach = new () { Code = new(17), Value = new (98) }
                }
            ],
            IDeviceNotification.Action.Arrival,
            new Dictionary<nint, SetVCPFeatureInvocation>
            {
                [0x12345] = new() { Code = 17, Value = 98 }
            }
        ),
        ["detach one monitor"] = new(
            new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x23456u) }
            },
            [
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Detach = new () { Code = new(42), Value = new (76) }
                }
            ],
            IDeviceNotification.Action.Removal,
            new Dictionary<nint, SetVCPFeatureInvocation>
            {
                [0x23456] = new() { Code = 42, Value = 76 }
            }
        ),
        ["no monitors"] = new(
            new Monitors(),
            [],
            IDeviceNotification.Action.Arrival,
            new Dictionary<nint, SetVCPFeatureInvocation>()
        ),
        ["null configured monitors"] = new(
            new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x34567u) },
                new() { Description = "dEsCrIpTiOn 2", Handle = H(0x45689u) }
            },
            null,
            IDeviceNotification.Action.Removal,
            new Dictionary<nint, SetVCPFeatureInvocation>()
        ),
        ["no configured monitors"] = new(
            new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x34567u) },
                new() { Description = "dEsCrIpTiOn 2", Handle = H(0x45689u) }
            },
            [],
            IDeviceNotification.Action.Removal,
            new Dictionary<nint, SetVCPFeatureInvocation>()
        ),
        ["no monitor devices"] = new(
            new Monitors(),
            [
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Attach = new () { Code = new(42), Value = new (76) }
                },
                new()
                {
                    Description = "dEsCrIpTiOn 2",
                    Attach = new () { Code = new(43), Value = new (75) }
                }
            ],
            IDeviceNotification.Action.Arrival,
            new Dictionary<nint, SetVCPFeatureInvocation>()
        ),
        ["attach with missing attach config"] = new(
            new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x56789u) }
            },
            [
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Detach = new () { Code = new(17), Value = new (98) }
                }
            ],
            IDeviceNotification.Action.Arrival,
            new Dictionary<nint, SetVCPFeatureInvocation>()
        ),
        ["detach with missing attach config"] = new(
            new Monitors
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x56789u) }
            },
            [
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Attach = new () { Code = new(17), Value = new (98) }
                }
            ],
            IDeviceNotification.Action.Removal,
            new Dictionary<nint, SetVCPFeatureInvocation>()
        )
    };

    public static TheoryData<string> TestDataKeys => [.. TestData.Keys.AsEnumerable()];

    class SetVCPFeatureInvocation
    {
        public required byte Code { get; set; }
        public required uint Value { get; set; }
    }

    void SetupForProcessOneNotification(
        Monitors monitorDevices,
        IDictionary<nint, SetVCPFeatureInvocation> invocations,
        AutoResetEvent finishedEvent)
    {
        _monitorServiceMock
            .Setup(m => m.LoadMonitors())
            .Returns(monitorDevices)
            .Verifiable();

        _windowsAPIMock
            .Setup(m => m.SetVCPFeature(It.IsAny<SafeHandle>(), It.IsAny<byte>(), It.IsAny<uint>()))
            .Returns(
                (SafeHandle hMonitor, byte bVCPCode, uint dwNewValue) =>
                {
                    SetVCPFeatureInvocation? invocation;
                    Assert.True(invocations.TryGetValue(
                        hMonitor.DangerousGetHandle(), out invocation));

                    Assert.Equal(invocation.Code, bVCPCode);
                    Assert.Equal(invocation.Value, dwNewValue);

                    return 1;
                }
            )
            .Verifiable();

        // Once we receive this log message, we assume all SetVCPFeature calls were made
        _loggerMock
            .Setup(m => m.Debug("Device notification processed"))
            .Callback(() => finishedEvent.Set());
    }

    [Theory, MemberData(nameof(TestDataKeys))]
    public void ProcessOneNotification_Succeeds(string testDataKey)
    {
        var (monitorDevices, monitorConfigs, action, invocations) = TestData[testDataKey];

        // The mock notification will pass data to this channel
        var channel = Channel.CreateUnbounded<IDeviceNotification.Action>();
        var notificationMock = SetupNotification(channel);

        var finishedEvent = new AutoResetEvent(false);
        SetupForProcessOneNotification(monitorDevices, invocations, finishedEvent);

        var config = new Config
        {
            TriggerDevice = new() { Raw = _testGuid },
            Monitors = monitorConfigs?.ToList()
        };

        using (var backgroundTask = new BackgroundTask(_deviceNotificationServiceMock.Object,
            _monitorServiceMock.Object, _windowsAPIMock.Object, _loggerMock.Object))
        {
            backgroundTask.Restart(config);

            // Task is now running, send it an action
            channel.Writer.TryWrite(action);

            // Wait for the background task to make all calls to SetVCPFeature
            Assert.True(finishedEvent.WaitOne(TimeSpan.FromSeconds(5)));
        }

        // Check no other SetVCPFeature calls were made
        _windowsAPIMock
            .Verify(
                m => m.SetVCPFeature(It.IsAny<SafeHandle>(), It.IsAny<byte>(), It.IsAny<uint>()),
                Times.Exactly(invocations.Count));

        _monitorServiceMock.Verify();
        _deviceNotificationServiceMock.Verify();
        notificationMock.Verify();
    }
}
