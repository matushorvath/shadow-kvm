using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace ShadowKVM;

public partial class NotifyIconViewModel : ObservableObject
{
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

    App App => (App)Application.Current;
}
