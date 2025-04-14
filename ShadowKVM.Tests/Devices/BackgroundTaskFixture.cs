using System.Runtime.InteropServices;
using System.Threading.Channels;
using Moq;
using Serilog;
using Windows.Win32.Foundation;

namespace ShadowKVM.Tests;

public class BackgroundTaskFixture
{
    protected Mock<IDeviceNotificationService> _deviceNotificationServiceMock = new();
    protected Mock<IMonitorService> _monitorServiceMock = new();
    protected Mock<IWindowsAPI> _windowsAPIMock = new();
    protected Mock<ILogger> _loggerMock = new();

    protected Guid _testGuid = new("{3f527904-28d8-4cda-b1c3-08cca9dc3dff}");

    protected Mock<IDeviceNotification> SetupNotification(Channel<IDeviceNotification.Action> channel)
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

    protected class SetVCPFeatureInvocation
    {
        public required byte Code { get; set; }
        public required uint Value { get; set; }
    }

    protected void SetupForProcessOneNotification(
        Monitors monitorDevices,
        IDictionary<nint, List<SetVCPFeatureInvocation>> invocations,
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
                    List<SetVCPFeatureInvocation>? handleInvocations;
                    Assert.True(invocations.TryGetValue(
                        hMonitor.DangerousGetHandle(), out handleInvocations));

                    Assert.Contains(handleInvocations, i => i.Code == bVCPCode);
                    Assert.Contains(handleInvocations, i => i.Value == dwNewValue);

                    return 1;
                }
            )
            .Verifiable();

        // Once we receive this log message, we assume all SetVCPFeature calls were made
        _loggerMock
            .Setup(m => m.Debug("Device notification processed"))
            .Callback(() => finishedEvent.Set());
    }

    protected static SafePhysicalMonitorHandle H(nuint value)
        => new SafePhysicalMonitorHandle(null!, (HANDLE)value, false);

    protected record TestDatum(
        Monitors monitorDevices,
        MonitorConfig[]? monitorConfigs,
        IDeviceNotification.Action action,
        Dictionary<nint, List<SetVCPFeatureInvocation>> invocations);

    protected void TestOneNotification(Dictionary<string, TestDatum> testData, string testDataKey)
    {
        var (monitorDevices, monitorConfigs, action, invocations) = testData[testDataKey];

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
                Times.Exactly(invocations.Sum(i => i.Value.Count)));

        _monitorServiceMock.Verify();
        _deviceNotificationServiceMock.Verify();
        notificationMock.Verify();
    }
}
