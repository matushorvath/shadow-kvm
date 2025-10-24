using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Serilog;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;

namespace ShadowKVM;

public interface IDeviceNotificationService
{
    IDeviceNotification Register(Guid deviceClassGuid, int? filterPid, int? filterVid);
}

public class DeviceNotificationService(IWindowsAPI windowsAPI, ILogger logger) : IDeviceNotificationService
{
    public IDeviceNotification Register(Guid deviceClassGuid, int? filterVid, int? filterPid)
    {
        var notification = new DeviceNotification(windowsAPI, logger);
        notification.Register(deviceClassGuid, filterVid, filterPid);
        return notification;
    }
}

public interface IDeviceNotification : IDisposable
{
    enum Action
    {
        Arrival, Removal
    }

    ChannelReader<Action> Reader { get; }
}

public partial class DeviceNotification : IDeviceNotification
{
    public DeviceNotification(IWindowsAPI windowsAPI, ILogger logger)
    {
        _windowsAPI = windowsAPI;
        _logger = logger;

        var channelOptions = new BoundedChannelOptions(16);
        channelOptions.FullMode = BoundedChannelFullMode.DropOldest;
        channelOptions.SingleReader = true;
        channelOptions.SingleWriter = true;

        _channel = Channel.CreateBounded<IDeviceNotification.Action>(channelOptions);
    }

    public unsafe void Register(Guid filterDeviceClassGuid, int? filterVid, int? filterPid)
    {
        if (_notification != null)
        {
            throw new Exception("Device notification is already registered");
        }

        var filter = new CM_NOTIFY_FILTER();
        filter.cbSize = (uint)Marshal.SizeOf(filter);
        filter.FilterType = CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE;
        filter.u.DeviceInterface.ClassGuid = filterDeviceClassGuid;

        CONFIGRET res = _windowsAPI.CM_Register_Notification(filter, 0, DeviceCallback, out _notification);
        if (res != CONFIGRET.CR_SUCCESS)
        {
            throw new Exception($"Registration for device notifications failed, result {res}");
        }

        _filterVid = filterVid;
        _filterPid = filterPid;
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

        var outsideAction =
            action == CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL
            ? IDeviceNotification.Action.Arrival : IDeviceNotification.Action.Removal;

        // Filter by VID/PID if specified
        var (vid, pid) = GetDeviceInfo(evt);
        _logger.Debug("Trigger device vendor id: {VendorId:x}, product id: {ProductId:x}", vid, pid);

        if (_filterVid != null && _filterVid != vid)
        {
            // VID does not match, skip notification
            _logger.Debug("Device vendor id {DeviceVendorId:x} does not match configured vendor id {ConfigVendorId:x}, ignoring trigger", vid, _filterVid);
            return (uint)WIN32_ERROR.ERROR_SUCCESS;
        }

        if (_filterPid != null && _filterPid != pid)
        {
            // PID does not match, skip notification
            _logger.Debug("Device product id {DeviceProductId:x} does not match configured product id {ConfigProductId:x}, ignoring trigger", pid, _filterPid);
            return (uint)WIN32_ERROR.ERROR_SUCCESS;
        }

        _channel.Writer.TryWrite(outsideAction);    // always succeeds

        return (uint)WIN32_ERROR.ERROR_SUCCESS;
    }

    [GeneratedRegex(@"VID[&_](?<vid>[0-9A-Fa-f]+)", RegexOptions.IgnoreCase)]
    private static partial Regex VidRegex();

    [GeneratedRegex(@"PID[&_](?<pid>[0-9A-Fa-f]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PidRegex();

    unsafe (int? vid, int? pid) GetDeviceInfo(CM_NOTIFY_EVENT_DATA* evt)
    {
        string? symbolicLink;
        fixed (char* symbolicLinkPtr = &evt->u.DeviceInterface.SymbolicLink.e0)
        {
            symbolicLink = Marshal.PtrToStringUni((nint)symbolicLinkPtr);
        }

        if (symbolicLink == null)
        {
            return (null, null);
        }

        var vidMatch = VidRegex().Match(symbolicLink);
        int? vid = vidMatch.Success ? int.Parse(vidMatch.Groups["vid"].Value, NumberStyles.HexNumber) : null;

        var pidMatch = PidRegex().Match(symbolicLink);
        int? pid = pidMatch.Success ? int.Parse(pidMatch.Groups["pid"].Value, NumberStyles.HexNumber) : null;

        return (vid, pid);
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
                _notification.Dispose();
                _notification = null;
            }
        }
    }

    public ChannelReader<IDeviceNotification.Action> Reader => _channel.Reader;

    IWindowsAPI _windowsAPI;
    ILogger _logger;

    Channel<IDeviceNotification.Action> _channel;
    public CM_Unregister_NotificationSafeHandle? _notification; // public for unit tests, don't use

    int? _filterVid;
    int? _filterPid;
}
