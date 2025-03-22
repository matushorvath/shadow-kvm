using Microsoft.Win32.SafeHandles;
using Windows.Win32.Foundation;

namespace ShadowKVM;

internal class SafePhysicalMonitorHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafePhysicalMonitorHandle(IMonitorAPI monitorAPI, HANDLE preexistingHandle, bool ownsHandle)
        : base(ownsHandle)
    {
        _monitorAPI = monitorAPI;
        handle = preexistingHandle;
    }

    override protected bool ReleaseHandle()
    {
        return _monitorAPI.DestroyPhysicalMonitor(new HANDLE(handle));
    }

    public static SafePhysicalMonitorHandle Null => new SafePhysicalMonitorHandle(null!, new HANDLE(), false);

    IMonitorAPI _monitorAPI;
}
