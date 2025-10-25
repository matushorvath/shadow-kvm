using System.Runtime.InteropServices;
using Moq;
using Serilog;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;

namespace ShadowKVM.Tests;

public class DeviceNotificationTests
{
    public Mock<IWindowsAPI> _windowsAPIMock = new();
    protected Mock<ILogger> _loggerMock = new();

    static Guid _testGuid = new("{582a0a58-a22c-431a-bffe-8e381e0522e7}");
    protected int _testVid = 0xabcde;
    protected int _testPid = 0xfedcba;

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

        var service = new DeviceNotificationService(_windowsAPIMock.Object, _loggerMock.Object);
        var exception = Assert.Throws<Exception>(() => service.Register(_testGuid, _testVid, _testPid));

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

        var service = new DeviceNotificationService(_windowsAPIMock.Object, _loggerMock.Object);
        service.Register(_testGuid, _testVid, _testPid);

        _windowsAPIMock.Verify();
    }

    [Fact]
    public void Register_SucceedsWithoutVidPid()
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

        var service = new DeviceNotificationService(_windowsAPIMock.Object, _loggerMock.Object);
        service.Register(_testGuid, null, null);

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

        var service = new DeviceNotificationService(_windowsAPIMock.Object, _loggerMock.Object);
        var notification = (DeviceNotification)service.Register(_testGuid, _testVid, _testPid);

        var exception = Assert.Throws<Exception>(() => notification.Register(_testGuid, _testVid, _testPid));

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

        var service = new DeviceNotificationService(_windowsAPIMock.Object, _loggerMock.Object);

        var notification = (DeviceNotification)service.Register(_testGuid, _testVid, _testPid);
        notification.Dispose();

        Assert.Null(notification._notification);

        _windowsAPIMock.Verify();
    }

    unsafe void SetupForCallback(CM_NOTIFY_ACTION callbackAction, CM_NOTIFY_EVENT_DATA* callbackEvent)
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
                    var result = pCallback((HCMNOTIFICATION)12345u, null,
                        callbackAction, callbackEvent, (uint)Marshal.SizeOf(*callbackEvent));

                    Assert.Equal((uint)WIN32_ERROR.ERROR_SUCCESS, result);

                    pNotifyContext = new CM_Unregister_NotificationSafeHandle(12345, false);
                    return CONFIGRET.CR_SUCCESS;
                })
            .Verifiable();
    }

    [Fact]
    public unsafe void Callback_IgnoresWrongFilterType()
    {
        CM_NOTIFY_EVENT_DATA evt = new() { FilterType = CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEHANDLE };
        SetupForCallback(CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL, &evt);

        var service = new DeviceNotificationService(_windowsAPIMock.Object, _loggerMock.Object);
        var notification = service.Register(_testGuid, _testVid, _testPid);

        _windowsAPIMock.Verify();

        // No action sent to the channel
        Assert.False(notification.Reader.TryRead(out _));
    }

    [Fact]
    public unsafe void Callback_IgnoresWrongAction()
    {
        CM_NOTIFY_EVENT_DATA evt = new() { FilterType = CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE };
        SetupForCallback(CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICECUSTOMEVENT, &evt);

        var service = new DeviceNotificationService(_windowsAPIMock.Object, _loggerMock.Object);
        var notification = service.Register(_testGuid, _testVid, _testPid);

        _windowsAPIMock.Verify();

        // No action sent to the channel
        Assert.False(notification.Reader.TryRead(out _));
    }

    [Theory]
    [InlineData(CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL, IDeviceNotification.Action.Arrival)]
    [InlineData(CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEINTERFACEREMOVAL, IDeviceNotification.Action.Removal)]
    public unsafe void Callback_Succeeds(CM_NOTIFY_ACTION callbackAction, IDeviceNotification.Action channelAction)
    {
        CM_NOTIFY_EVENT_DATA evt = new() { FilterType = CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE };
        SetupForCallback(callbackAction, &evt);

        var service = new DeviceNotificationService(_windowsAPIMock.Object, _loggerMock.Object);
        var notification = service.Register(_testGuid, _testVid, _testPid);

        _windowsAPIMock.Verify();

        // One action sent to the channel
        IDeviceNotification.Action action;
        var haveAction = notification.Reader.TryRead(out action);

        Assert.True(haveAction);
        Assert.Equal(channelAction, action);
    }

    unsafe static IntPtr CreateNotifyEventData(string symbolicLink = "")
    {
        // Offset of union 'u' inside CM_NOTIFY_EVENT_DATA
        int unionOffset = Marshal.OffsetOf<CM_NOTIFY_EVENT_DATA>("u").ToInt32();

        // Size of the DeviceInterface struct including charCount characters in the inline array
        int deviceInterfaceSize = CM_NOTIFY_EVENT_DATA._u_e__Union._DeviceInterface_e__Struct.SizeOf(symbolicLink.Length + 1);

        // Allocate the struct
        int totalSize = unionOffset + deviceInterfaceSize;
        IntPtr buffer = Marshal.AllocHGlobal(totalSize);

        Span<byte> span = new Span<byte>((void*)buffer, totalSize);
        span.Clear();

        var evt = (CM_NOTIFY_EVENT_DATA*)buffer.ToPointer();

        fixed (char* dest = &evt->u.DeviceInterface.SymbolicLink.e0)
        fixed (char* src = symbolicLink)
        {
            Buffer.MemoryCopy(src, dest, (symbolicLink.Length + 1) * sizeof(char), symbolicLink.Length * sizeof(char));
            dest[symbolicLink.Length] = '\0';
        }

        return buffer;
    }

    [Theory]
    [InlineData("""\\?\HID#VID_046D&PID_C52B&MI_00#c&1785d12d&0&0000#{884b96c3-56ef-11d1-bc8c-00a0c91405dd}""", 0x046D, 0xC52B)]
    [InlineData("""\\?\HID#{00001124-0000-1000-8000-00805f9b34fb}_VID&000204e8_PID&7021&Col01#b&2af070cf&0&0000#{884b96c3-56ef-11d1-bc8c-00a0c91405dd}""", 0x000204e8, 0x7021)]
    [InlineData("""\\?\HID#VID_046D&MI_00#c&1785d12d&0&0000#{884b96c3-56ef-11d1-bc8c-00a0c91405dd}""", 0x046D, null)]
    [InlineData("""\\?\HID#{00001124-0000-1000-8000-00805f9b34fb}_PID&7021&Col01#b&2af070cf&0&0000#{884b96c3-56ef-11d1-bc8c-00a0c91405dd}""", null, 0x7021)]
    [InlineData("nOnSeNsE", null, null)]
    [InlineData("", null, null)]
    public unsafe void Callback_LogsDeviceInfo(string symbolicLink, int? vid, int? pid)
    {
        var buffer = CreateNotifyEventData(symbolicLink);

        try
        {
            var evt = (CM_NOTIFY_EVENT_DATA*)buffer.ToPointer();
            evt->FilterType = CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE;

            SetupForCallback(CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL, evt);

            var service = new DeviceNotificationService(_windowsAPIMock.Object, _loggerMock.Object);
            var notification = service.Register(_testGuid, _testVid, _testPid);

            _windowsAPIMock.Verify();

            // Verify the data was logged
            _loggerMock.Verify(m => m.Debug("Trigger device vendor id: {VendorId:x}, product id: {ProductId:x}", vid, pid));

            // One action sent to the channel
            IDeviceNotification.Action action;
            var haveAction = notification.Reader.TryRead(out action);

            Assert.True(haveAction);
            Assert.Equal(IDeviceNotification.Action.Arrival, action);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}

// TODO select-device
// check vid and pid get passed to DeviceNotification and saved
// test with missing filters, one filter, two filters
// test with each filter matches and misses; check log message when misses
// GetDeviceInfo with missing symlink (null?), with not matching nonsense symlink, with missing VID or PID
