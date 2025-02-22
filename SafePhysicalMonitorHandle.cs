using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;

internal class SafePhysicalMonitorHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafePhysicalMonitorHandle(HANDLE preexistingHandle, bool ownsHandle)
        : base(ownsHandle)
    {
        handle = preexistingHandle;
    }

    override protected bool ReleaseHandle()
    {
        return PInvoke.DestroyPhysicalMonitor(new HANDLE(handle));
    }
}
