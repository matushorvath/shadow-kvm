using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShadowKVM;

public partial class NotifyIconViewModel : ObservableObject
{
    public NotifyIconViewModel()
    {
        _enabledIcon = new BitmapImage(new Uri("pack://application:,,,/UserInterface/Icon.ico"));
        _disabledIcon = new BitmapImage(new Uri("pack://application:,,,/UserInterface/IconDisabled.ico"));

        IsAutostart = Autostart.IsEnabled();
        IsEnabled = true;
    }

    ImageSource _enabledIcon;
    ImageSource _disabledIcon;

    public ImageSource Icon => IsEnabled ? _enabledIcon : _disabledIcon;

    [RelayCommand(FlowExceptionsToTaskScheduler = true)]
    public async Task Configure()
    {
        await App.EditConfig();
        App.ReloadConfig(message: true);
    }

    [RelayCommand]
    public void Exit()
    {
        Application.Current.Shutdown();
    }

    [ObservableProperty]
    private bool isAutostart;

    partial void OnIsAutostartChanged(bool value)
    {
        Autostart.SetEnabled(value);
    }

    [ObservableProperty]
    private bool isEnabled;

    partial void OnIsEnabledChanged(bool value)
    {
        App.IsEnabled = value;
        OnPropertyChanged(nameof(Icon));
    }

    App App => (App)Application.Current;
}
