using System.Runtime.InteropServices;
using System.Threading.Channels;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;

namespace ShadowKVM;

internal interface IDeviceNotificationFactory
{
    DeviceNotification Create();
}

internal class DeviceNotificationFactory(IWindowsAPI windowsAPI) : IDeviceNotificationFactory
{
    public DeviceNotification Create()
    {
        return new DeviceNotification(windowsAPI);
    }
}

internal class DeviceNotification : IDisposable
{
    public enum Action
    {
        Arrival, Removal
    }

    public DeviceNotification(IWindowsAPI windowsAPI)
    {
        _windowsAPI = windowsAPI;

        var channelOptions = new BoundedChannelOptions(16);
        channelOptions.FullMode = BoundedChannelFullMode.DropOldest;
        channelOptions.SingleReader = true;
        channelOptions.SingleWriter = true;

        _channel = Channel.CreateUnbounded<Action>();
    }

    public unsafe void Register(Guid deviceClassGuid)
    {
        if (_notification != null)
        {
            // Already registered, nothing to do
            return;
        }

        CM_NOTIFY_FILTER filter = new CM_NOTIFY_FILTER();
        filter.cbSize = (uint)Marshal.SizeOf(filter);
        filter.FilterType = CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE;
        filter.u.DeviceInterface.ClassGuid = deviceClassGuid;

        CONFIGRET res = PInvoke.CM_Register_Notification(filter, null, DeviceCallback, out _notification);
        if (res != CONFIGRET.CR_SUCCESS)
        {
            throw new Exception($"Registration for device notifications failed, result {res}");
        }
    }

    unsafe uint DeviceCallback(HCMNOTIFICATION notification, void* context,
        CM_NOTIFY_ACTION action, CM_NOTIFY_EVENT_DATA* evt, uint eventDataSize)
    {
        if (evt->FilterType != CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE)
        {
            return (uint)WIN32_ERROR.ERROR_SUCCESS;
        }

        if (action != CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL
            && action != CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEINTERFACEREMOVAL)
        {
            return (uint)WIN32_ERROR.ERROR_SUCCESS;
        }

        var outsideAction = action == CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL ? Action.Arrival : Action.Removal;
        _channel?.Writer.TryWrite(outsideAction);    // always succeeds

        return (uint)WIN32_ERROR.ERROR_SUCCESS;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_notification != null)
            {
                _notification?.Dispose();
                _notification = null;
            }
        }
    }

    public ChannelReader<Action> Reader
    {
        get
        {
            if (_channel == null)
            {
                throw new Exception("Channel does not exist");
            }
            return _channel.Reader;
        }
    }

    IWindowsAPI _windowsAPI;
    Channel<Action>? _channel;
    CM_Unregister_NotificationSafeHandle? _notification;
}
