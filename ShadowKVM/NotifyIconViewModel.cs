using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;

namespace ShadowKVM;

public partial class NotifyIconViewModel : ObservableObject
{
    // [ObservableProperty]
    // [NotifyCanExecuteChangedFor(nameof(ConfigureCommand))]
    // public bool canExecuteConfigure = true;

    // [RelayCommand(CanExecute = nameof(CanExecuteConfigure))]
    // public void Configure()
    // {
    //     Application.Current.MainWindow ??= new MainWindow();
    //     Application.Current.MainWindow.Show(disableEfficiencyMode: true);
    //     CanExecuteConfigure = false;
    //     // TODO set CanExecuteConfigure to true once configuration is closed
    // }

    [RelayCommand]
    public void Exit()
    {
        Application.Current.Shutdown();
    }
}
