using System.Windows;
using Serilog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ShadowKVM;

// TODO move PInvoke to IWindowsAPI, write unit tests

// Hidden window that responds to WM_CLOSE, needed to exit cleanly when running the installer
public class HiddenWindow : IDisposable
{
    public unsafe void Create()
    {
        string className = "ShadowKVM_HiddenWindowClass";
        var hInstance = PInvoke.GetModuleHandle(null);

        fixed (char* pClassName = className)
        {
            var wndClass = new WNDCLASSW
            {
                lpfnWndProc = WndProc,
                hInstance = (HINSTANCE)hInstance.DangerousGetHandle(),
                lpszClassName = new PCWSTR(pClassName)
            };

            var atom = PInvoke.RegisterClass(in wndClass);
            if (atom == 0)
            {
                throw new Exception("Could not register hidden window class");
            }
        }

        _hwnd = PInvoke.CreateWindowEx(0, className, "ShadowKVM_HiddenWindow",
            0, 0, 0, 0, 0, HWND.Null, null, hInstance, null);
        if (_hwnd.IsNull)
        {
            throw new Exception("Could not create hidden window");
        }
    }

    static unsafe LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == PInvoke.WM_CLOSE)
        {
            Log.Information("Received WM_CLOSE, shutting down");
            App.Current.Dispatcher.Invoke(App.Current.Shutdown);
            return new LRESULT();
        }

        return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_hwnd.IsNull)
        {
            PInvoke.DestroyWindow(_hwnd);
            _hwnd = HWND.Null;
        }
    }

    HWND _hwnd;
}
