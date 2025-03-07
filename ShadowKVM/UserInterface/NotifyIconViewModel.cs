using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Windows;

namespace ShadowKVM;

public partial class NotifyIconViewModel : ObservableObject
{
    [RelayCommand(FlowExceptionsToTaskScheduler = true)]
    public async Task Configure()
    {
        await App.EditConfigAsync();
        App.ReloadConfig(message: true); // TODO this doesn't throw when config is invalid
    }

    [RelayCommand]
    public void Exit()
    {
        Application.Current.Shutdown();
    }

    App App => (App)Application.Current;
}
