using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace ShadowKVM;

public partial class NotifyIconViewModel : ObservableObject
{
    public NotifyIconViewModel()
    {
        IsAutostartEnabled = Autostart.IsEnabled();
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
    private bool isAutostartEnabled;

    partial void OnIsAutostartEnabledChanged(bool value)
    {
        Autostart.SetEnabled(value);
    }

    App App => (App)Application.Current;
}
