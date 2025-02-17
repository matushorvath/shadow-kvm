using System.Runtime.InteropServices;
using System.Security;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;

// CM_NOTIFY_FILTER filter = new CM_NOTIFY_FILTER();
// filter.cbSize = 

//GUID_DEVINTERFACE_MOUSE  {378DE44C-56EF-11D1-BC8C-00A0C91405DD}
//GUID_DEVINTERFACE_KEYBOARD {884b96c3-56ef-11d1-bc8c-00a0c91405dd}


//CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE 

// TODO using? IDisposable?
//PInvoke.HCMNOTIFICATION notification;


//unsafe winmdroot.Devices.DeviceAndDriverInstallation.CONFIGRET CM_Register_Notification(in winmdroot.Devices.DeviceAndDriverInstallation.CM_NOTIFY_FILTER pFilter, void* pContext, winmdroot.Devices.DeviceAndDriverInstallation.PCM_NOTIFY_CALLBACK pCallback, out CM_Unregister_NotificationSafeHandle pNotifyContext)

unsafe {

CM_NOTIFY_FILTER filter = new CM_NOTIFY_FILTER();
filter.cbSize = (uint)Marshal.SizeOf(filter);
filter.FilterType = CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE;
filter.u.DeviceInterface.ClassGuid = PInvoke.GUID_DEVINTERFACE_KEYBOARD;

uint DeviceCallback(HCMNOTIFICATION notification, [Optional] void* Context,
    CM_NOTIFY_ACTION action, CM_NOTIFY_EVENT_DATA* eventData, uint eventDataSize)
{
    Console.WriteLine($"In callback, action {action}");
    return 0;
}

CM_Unregister_NotificationSafeHandle notification;
CONFIGRET res = PInvoke.CM_Register_Notification(filter, null, DeviceCallback, out notification);
if (res != CONFIGRET.CR_SUCCESS)
{
    Console.WriteLine($"Failed, result {res}");
}

Console.WriteLine("Succeeded");

Console.WriteLine("Press any key to continue...");
Console.ReadKey();

}
