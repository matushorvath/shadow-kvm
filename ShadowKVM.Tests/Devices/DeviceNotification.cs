using Moq;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;

namespace ShadowKVM.Tests;

public class DeviceNotificationTest
{
    internal Mock<IWindowsAPI> _windowsAPIMock = new();

    static Guid _testGuid = new("{582a0a58-a22c-431a-bffe-8e381e0522e7}");

    [Fact]
    public void Register_CMRegisterNotification_Fails()
    {
        _windowsAPIMock
            .Setup(m => m.CM_Register_Notification(
                It.IsAny<CM_NOTIFY_FILTER>(),
                It.IsAny<nuint>(),
                It.IsAny<PCM_NOTIFY_CALLBACK>(),
                out It.Ref<CM_Unregister_NotificationSafeHandle>.IsAny))
            .Returns(
                (CM_NOTIFY_FILTER pFilter, nuint pContext, PCM_NOTIFY_CALLBACK pCallback,
                    out CM_Unregister_NotificationSafeHandle pNotifyContext) =>
                {
                    pNotifyContext = new CM_Unregister_NotificationSafeHandle(12345, false);
                    return CONFIGRET.CR_FAILURE;
                })
            .Verifiable();

        var service = new DeviceNotificationService(_windowsAPIMock.Object);
        var exception = Assert.Throws<Exception>(() => service.Register(_testGuid));

        _windowsAPIMock.Verify();
        Assert.Equal("Registration for device notifications failed, result CR_FAILURE", exception.Message);
    }
}
