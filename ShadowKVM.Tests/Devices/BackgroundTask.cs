using Moq;
using Serilog;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Windows.Win32.Foundation;

namespace ShadowKVM.Tests;

// TODO
// attach, detach event
// no configured monitors, no attached monitors, both
// matching using description, adapter, serial - combinations
//   missing description, adapter, serial in config (should be ignored)
//   missing description, adapter, serial in device (still tries to match)
//   missing description, adapter, serial in both
// matches no monitors, one monitor, multiple monitors
// attach with missing attach config, detach with missing detach config
// one monitor device matching multiple monitor configs with different vcp code/value

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

    class SetVCPFeatureInvocation
    {
        public required byte Code { get; set; }
        public required uint Value { get; set; }
    }

    void SetupForProcessNotification(
        Monitors monitorDevices,
        IDictionary<nint, SetVCPFeatureInvocation> expectedInvocations,
        CountdownEvent setVcpFeatureCalled)
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

                    setVcpFeatureCalled.Signal();
                    return 1;
                }
            )
            .Verifiable();
    }

    [Theory, MemberData(nameof(TestDataKeys))]
    public void ProcessNotification_Succeeds(string testDataKey)
    {
        var (monitorDevices, monitorConfigs, action, expectedInvocations) = TestData[testDataKey];

        // The mock notification will pass data to this channel
        var channel = Channel.CreateUnbounded<IDeviceNotification.Action>();
        var notificationMock = SetupNotification(channel);

        var setVcpFeatureCalled = new CountdownEvent(expectedInvocations.Count);
        SetupForProcessNotification(monitorDevices, expectedInvocations, setVcpFeatureCalled);

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
        Assert.True(setVcpFeatureCalled.Wait(TimeSpan.FromSeconds(5)));

        _monitorServiceMock.Verify();
        _deviceNotificationServiceMock.Verify();
        notificationMock.Verify();
    }

    static Dictionary<string, (Monitors monitorDevices, MonitorConfig[] monitorConfigs, IDeviceNotification.Action action, Dictionary<nint, SetVCPFeatureInvocation> expectedInvocations)> TestData => new()
    {
        ["one monitor"] = new()
        {
            monitorDevices = new()
            {
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Handle = new SafePhysicalMonitorHandle(null!, (HANDLE)12345u, false)
                }
            },
            monitorConfigs =
            [
                new()
                {
                    Description = "dEsCrIpTiOn 1",
                    Attach = new () { Code = new(0x42), Value = new (0x98) }
                }
            ],
            action = IDeviceNotification.Action.Arrival,
            expectedInvocations = new Dictionary<nint, SetVCPFeatureInvocation>
            {
                [12345] = new() { Code = 0x42, Value = 0x98 }
            }
        }
    };

    public static TheoryData<string> TestDataKeys => [.. TestData.Keys.AsEnumerable()];
}
