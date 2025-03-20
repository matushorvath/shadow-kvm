using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace ShadowKVM;

public partial class NotifyIconViewModel : ObservableObject
{
    public NotifyIconViewModel()
    {
        IsAutostart = Autostart.IsEnabled();
    }

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
    private bool isEnabled = true;

    partial void OnIsEnabledChanged(bool value)
    {
        App.IsEnabled = value;
    }

    App App => (App)Application.Current;
}
