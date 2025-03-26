using Moq;
using Serilog;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Windows.Win32.Foundation;

namespace ShadowKVM.Tests;

// TODO
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
        // The mock notification will pass data to this channel
        var channel = Channel.CreateUnbounded<IDeviceNotification.Action>();
        var notificationMock = SetupNotification(channel);

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

    static SafePhysicalMonitorHandle H(nuint value) => new SafePhysicalMonitorHandle(null!, (HANDLE)value, false);

    static Dictionary<string, (Monitors monitorDevices, MonitorConfig[] monitorConfigs, IDeviceNotification.Action action, Dictionary<nint, SetVCPFeatureInvocation> expectedInvocations)> TestData => new()
    {
        ["attach one monitor"] = new()
        {
            monitorDevices = new()
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x12345u) }
            },
            monitorConfigs =
            [
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Attach = new () { Code = new(17), Value = new (98) }
                }
            ],
            action = IDeviceNotification.Action.Arrival,
            expectedInvocations = new Dictionary<nint, SetVCPFeatureInvocation>
            {
                [0x12345] = new() { Code = 17, Value = 98 }
            }
        },
        ["detach one monitor"] = new()
        {
            monitorDevices = new()
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x23456u) }
            },
            monitorConfigs =
            [
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Detach = new () { Code = new(42), Value = new (76) }
                }
            ],
            action = IDeviceNotification.Action.Removal,
            expectedInvocations = new Dictionary<nint, SetVCPFeatureInvocation>
            {
                [0x23456] = new() { Code = 42, Value = 76 }
            }
        },
        ["no monitors"] = new()
        {
            monitorDevices = new()
            {
            },
            monitorConfigs =
            [
            ],
            action = IDeviceNotification.Action.Arrival,
            expectedInvocations = new Dictionary<nint, SetVCPFeatureInvocation>
            {
            }
        },
        ["no configured monitors"] = new()
        {
            monitorDevices = new()
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x34567u) },
                new() { Description = "dEsCrIpTiOn 2", Handle = H(0x45689u) }
            },
            monitorConfigs =
            [
            ],
            action = IDeviceNotification.Action.Removal,
            expectedInvocations = new Dictionary<nint, SetVCPFeatureInvocation>
            {
            }
        },
        ["no monitor devices"] = new()
        {
            monitorDevices = new()
            {
            },
            monitorConfigs =
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
            action = IDeviceNotification.Action.Arrival,
            expectedInvocations = new Dictionary<nint, SetVCPFeatureInvocation>
            {
            }
        },
        ["attach with missing attach config"] = new()
        {
            monitorDevices = new()
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x56789u) }
            },
            monitorConfigs =
            [
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Detach = new () { Code = new(17), Value = new (98) }
                }
            ],
            action = IDeviceNotification.Action.Arrival,
            expectedInvocations = new Dictionary<nint, SetVCPFeatureInvocation>
            {
            }
        },
        ["detach with missing attach config"] = new()
        {
            monitorDevices = new()
            {
                new() { Description = "dEsCrIpTiOn 1", Handle = H(0x56789u) }
            },
            monitorConfigs =
            [
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Attach = new () { Code = new(17), Value = new (98) }
                }
            ],
            action = IDeviceNotification.Action.Removal,
            expectedInvocations = new Dictionary<nint, SetVCPFeatureInvocation>
            {
            }
        },
    };

    public static TheoryData<string> TestDataKeys => [.. TestData.Keys.AsEnumerable()];

    class SetVCPFeatureInvocation
    {
        public required byte Code { get; set; }
        public required uint Value { get; set; }
    }

    void SetupForProcessNotification(
        Monitors monitorDevices,
        IDictionary<nint, SetVCPFeatureInvocation> expectedInvocations,
        ManualResetEventSlim processingActionFinished)
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
                    SetVCPFeatureInvocation? expectedInvocation;
                    Assert.True(expectedInvocations.TryGetValue(
                        hMonitor.DangerousGetHandle(), out expectedInvocation));

                    Assert.Equal(expectedInvocation.Code, bVCPCode);
                    Assert.Equal(expectedInvocation.Value, dwNewValue);

                    return 1;
                }
            )
            .Verifiable();

        // Once we receive this log message, we assume all SetVCPFeature calls were made
        _loggerMock
            .Setup(m => m.Debug("Device notification processed"))
            .Callback(() => processingActionFinished.Set());
    }

    [Theory, MemberData(nameof(TestDataKeys))]
    public void ProcessNotification_Succeeds(string testDataKey)
    {
        var (monitorDevices, monitorConfigs, action, expectedInvocations) = TestData[testDataKey];

        _deviceNotificationServiceMock.Reset();
        _monitorServiceMock.Reset();
        _windowsAPIMock.Reset();
        _loggerMock.Reset();

        // The mock notification will pass data to this channel
        var channel = Channel.CreateUnbounded<IDeviceNotification.Action>();
        var notificationMock = SetupNotification(channel);

        var processingActionFinished = new ManualResetEventSlim(false);
        SetupForProcessNotification(monitorDevices, expectedInvocations, processingActionFinished);

        var config = new Config
        {
            TriggerDevice = new() { Raw = _testGuid },
            Monitors = monitorConfigs.ToList()
        };

        var backgroundTask = new BackgroundTask(_deviceNotificationServiceMock.Object,
            _monitorServiceMock.Object, _windowsAPIMock.Object, _loggerMock.Object);
        backgroundTask.Restart(config);

        // Task is now running, send it an action
        channel.Writer.TryWrite(action);

        // Wait for the background task to make all calls to SetVCPFeature
        Assert.True(processingActionFinished.Wait(TimeSpan.FromSeconds(5)));

        // TODO check no other SetVCPFeature calls were made

        _monitorServiceMock.Verify();
        _deviceNotificationServiceMock.Verify();
        notificationMock.Verify();
    }
}
