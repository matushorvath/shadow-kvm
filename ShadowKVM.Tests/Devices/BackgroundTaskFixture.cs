using Moq;
using Serilog;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace ShadowKVM.Tests;

// TODO BackgroundTask tests
// matching using description, adapter, serial - combinations
//   missing description, adapter, serial in config (should be ignored)
//   missing description, adapter, serial in device (still tries to match)
//   missing description, adapter, serial in both
// matches one monitor with multiple configs, with different vcp code/value
// matches multiple monitors with one config

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
}
