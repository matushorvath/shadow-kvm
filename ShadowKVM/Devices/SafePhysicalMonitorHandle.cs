using Microsoft.Win32.SafeHandles;
using Windows.Win32.Foundation;

namespace ShadowKVM;

public class SafePhysicalMonitorHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafePhysicalMonitorHandle(IWindowsAPI windowsAPI, HANDLE preexistingHandle, bool ownsHandle)
        : base(ownsHandle)
    {
        _windowsAPI = windowsAPI;
        handle = preexistingHandle;
    }

    override protected bool ReleaseHandle()
    {
        return _windowsAPI.DestroyPhysicalMonitor(new HANDLE(handle));
    }

    public static SafePhysicalMonitorHandle Null => new SafePhysicalMonitorHandle(null!, new HANDLE(), false);

    IWindowsAPI _windowsAPI;
}
