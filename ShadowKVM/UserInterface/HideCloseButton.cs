using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ShadowKVM;

// TODO write unit tests

public class HideCloseButton : Behavior<Window>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Loaded += OnLoaded;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Loaded -= OnLoaded;
        base.OnDetaching();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new HWND(new WindowInteropHelper(AssociatedObject).Handle);

        var style = PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, style & ~(int)WINDOW_STYLE.WS_SYSMENU);
    }
}
