using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShadowKVM;

// TODO remove the App dependency, write unit tests

public partial class NotifyIconViewModel : ObservableObject
{
    public NotifyIconViewModel()
    {
        _enabledIcon = new BitmapImage(new Uri("pack://application:,,,/UserInterface/TrayEnabled.ico"));
        _disabledIcon = new BitmapImage(new Uri("pack://application:,,,/UserInterface/TrayDisabled.ico"));

        isAutostart = Autostart.IsEnabled();
    }

    [RelayCommand(FlowExceptionsToTaskScheduler = true)]
    public async Task Configure()
    {
        // Making this async grays out the menu item while editing config
        await App.EditConfig();
        App.ReloadConfig(message: true);
    }

    [RelayCommand]
    public void Exit()
    {
        // TODO probably use an event to avoid tight coupling
        Application.Current.Shutdown();
    }

    [RelayCommand(FlowExceptionsToTaskScheduler = true)]
    public Task About()
    {
        // Making this async grays out the menu item while the window is open
        var tcs = new TaskCompletionSource();

        var aboutWindow = new AboutWindow();
        aboutWindow.Closed += (_, _) => tcs.SetResult();
        aboutWindow.Show();

        return tcs.Task;
    }

    [ObservableProperty]
    bool isAutostart;

    partial void OnIsAutostartChanged(bool value)
    {
        Autostart.SetEnabled(value);
    }

    [RelayCommand]
    public void EnableDisable()
    {
        App.BackgroundTask.Enabled = !App.BackgroundTask.Enabled;

        OnPropertyChanged(nameof(Icon));
        OnPropertyChanged(nameof(EnableDisableText));
    }

    public string EnableDisableText => App.BackgroundTask.Enabled ? "Disable" : "Enable";

    ImageSource _enabledIcon;
    ImageSource _disabledIcon;

    public ImageSource Icon => App.BackgroundTask.Enabled ? _enabledIcon : _disabledIcon;

    App App => (App)Application.Current;
}
