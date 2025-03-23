using System.Runtime.InteropServices;
using Moq;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;

namespace ShadowKVM.Tests;

public class DeviceNotificationTest
{
    public Mock<IWindowsAPI> _windowsAPIMock = new();

    static Guid _testGuid = new("{582a0a58-a22c-431a-bffe-8e381e0522e7}");

    [Fact]
    public void Register_CMRegisterNotification_Fails()
    {
        _windowsAPIMock
            .Setup(m => m.CM_Register_Notification(
                It.IsNotNull<CM_NOTIFY_FILTER>(), 0, It.IsNotNull<PCM_NOTIFY_CALLBACK>(),
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

    [Fact]
    public void Register_Succeeds()
    {
        _windowsAPIMock
            .Setup(m => m.CM_Register_Notification(
                It.IsNotNull<CM_NOTIFY_FILTER>(), 0, It.IsNotNull<PCM_NOTIFY_CALLBACK>(),
                out It.Ref<CM_Unregister_NotificationSafeHandle>.IsAny))
            .Returns(
                (CM_NOTIFY_FILTER pFilter, nuint pContext, PCM_NOTIFY_CALLBACK pCallback,
                    out CM_Unregister_NotificationSafeHandle pNotifyContext) =>
                {
                    Assert.Equal(416u, pFilter.cbSize); // size of CM_NOTIFY_FILTER
                    Assert.Equal(CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE, pFilter.FilterType);
                    Assert.Equal(_testGuid, pFilter.u.DeviceInterface.ClassGuid);

                    pNotifyContext = new CM_Unregister_NotificationSafeHandle(12345, false);
                    return CONFIGRET.CR_SUCCESS;
                })
            .Verifiable();

        var service = new DeviceNotificationService(_windowsAPIMock.Object);
        service.Register(_testGuid);

        _windowsAPIMock.Verify();
    }

    [Fact]
    public void Register_FailsSecondTime()
    {
        _windowsAPIMock
            .Setup(m => m.CM_Register_Notification(
                It.IsNotNull<CM_NOTIFY_FILTER>(), 0, It.IsNotNull<PCM_NOTIFY_CALLBACK>(),
                out It.Ref<CM_Unregister_NotificationSafeHandle>.IsAny))
            .Returns(
                (CM_NOTIFY_FILTER pFilter, nuint pContext, PCM_NOTIFY_CALLBACK pCallback,
                    out CM_Unregister_NotificationSafeHandle pNotifyContext) =>
                {
                    pNotifyContext = new CM_Unregister_NotificationSafeHandle(12345, false);
                    return CONFIGRET.CR_SUCCESS;
                })
            .Verifiable();

        var service = new DeviceNotificationService(_windowsAPIMock.Object);
        var notification = (DeviceNotification)service.Register(_testGuid);

        var exception = Assert.Throws<Exception>(() => notification.Register(_testGuid));

        _windowsAPIMock.Verify();

        Assert.Equal("Device notification is already registered", exception.Message);
    }

    [Fact]
    public void DeviceNotification_Disposes()
    {
        _windowsAPIMock
            .Setup(m => m.CM_Register_Notification(
                It.IsNotNull<CM_NOTIFY_FILTER>(), 0, It.IsNotNull<PCM_NOTIFY_CALLBACK>(),
                out It.Ref<CM_Unregister_NotificationSafeHandle>.IsAny))
            .Returns(
                (CM_NOTIFY_FILTER pFilter, nuint pContext, PCM_NOTIFY_CALLBACK pCallback,
                    out CM_Unregister_NotificationSafeHandle pNotifyContext) =>
                {
                    Assert.Equal(416u, pFilter.cbSize); // size of CM_NOTIFY_FILTER
                    Assert.Equal(CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE, pFilter.FilterType);
                    Assert.Equal(_testGuid, pFilter.u.DeviceInterface.ClassGuid);

                    pNotifyContext = new CM_Unregister_NotificationSafeHandle(12345, false);
                    return CONFIGRET.CR_SUCCESS;
                })
            .Verifiable();

        var service = new DeviceNotificationService(_windowsAPIMock.Object);

        var notification = (DeviceNotification)service.Register(_testGuid);
        notification.Dispose();

        Assert.Null(notification._notification);

        _windowsAPIMock.Verify();
    }

    unsafe void SetupForCallback(CM_NOTIFY_ACTION callbackAction, CM_NOTIFY_EVENT_DATA callbackEvent)
    {
        _windowsAPIMock
            .Setup(m => m.CM_Register_Notification(
                It.IsNotNull<CM_NOTIFY_FILTER>(), 0, It.IsNotNull<PCM_NOTIFY_CALLBACK>(),
                out It.Ref<CM_Unregister_NotificationSafeHandle>.IsAny))
            .Returns(
                (CM_NOTIFY_FILTER pFilter, nuint pContext, PCM_NOTIFY_CALLBACK pCallback,
                    out CM_Unregister_NotificationSafeHandle pNotifyContext) =>
                {
                    Assert.Equal(0u, pContext);

                    // The callback would normally be called asynchronously, but this is good enough for tests
                    var localCallbackEvent = callbackEvent;
                    var result = pCallback((HCMNOTIFICATION)12345u, null,
                        callbackAction, &localCallbackEvent, (uint)Marshal.SizeOf(localCallbackEvent));

                    Assert.Equal((uint)WIN32_ERROR.ERROR_SUCCESS, result);

                    pNotifyContext = new CM_Unregister_NotificationSafeHandle(12345, false);
                    return CONFIGRET.CR_SUCCESS;
                })
            .Verifiable();
    }

    [Fact]
    public void Callback_IgnoresWrongFilterType()
    {
        SetupForCallback(
            CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL,
            new() { FilterType = CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEHANDLE });

        var service = new DeviceNotificationService(_windowsAPIMock.Object);
        var notification = service.Register(_testGuid);

        _windowsAPIMock.Verify();

        // No action sent to the channel
        Assert.False(notification.Reader.TryRead(out _));
    }

    [Fact]
    public void Callback_IgnoresWrongAction()
    {
        SetupForCallback(
            CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICECUSTOMEVENT,
            new() { FilterType = CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE });

        var service = new DeviceNotificationService(_windowsAPIMock.Object);
        var notification = service.Register(_testGuid);

        _windowsAPIMock.Verify();

        // No action sent to the channel
        Assert.False(notification.Reader.TryRead(out _));
    }

    [Theory]
    [InlineData(CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL, IDeviceNotification.Action.Arrival)]
    [InlineData(CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEINTERFACEREMOVAL, IDeviceNotification.Action.Removal)]
    public void Callback_Succeeds(CM_NOTIFY_ACTION callbackAction, IDeviceNotification.Action channelAction)
    {
        SetupForCallback(
            callbackAction,
            new() { FilterType = CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE });

        var service = new DeviceNotificationService(_windowsAPIMock.Object);
        var notification = service.Register(_testGuid);

        _windowsAPIMock.Verify();

        // One action sent to the channel
        IDeviceNotification.Action action;
        var haveAction = notification.Reader.TryRead(out action);

        Assert.True(haveAction);
        Assert.Equal(channelAction, action);
    }
}
