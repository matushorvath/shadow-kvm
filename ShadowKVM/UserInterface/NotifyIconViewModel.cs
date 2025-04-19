using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShadowKVM;

// TODO write unit tests
// TODO get Services without App dependency

public partial class NotifyIconViewModel : ObservableObject
{
    public NotifyIconViewModel()
        : this(App.Current, App.Current.Services.BackgroundTask, App.Current.Services.ConfigEditor, App.Current.Services.Autostart)
    {
    }

    public NotifyIconViewModel(IAppControl appControl, IBackgroundTask backgroundTask, IConfigEditor configEditor, IAutostart autostart)
    {
        AppControl = appControl;
        BackgroundTask = backgroundTask;
        ConfigEditor = configEditor;
        Autostart = autostart;

        isAutostart = Autostart.IsEnabled();
    }

    [RelayCommand(FlowExceptionsToTaskScheduler = true)]
    public async Task Configure()
    {
        // Making this async grays out the menu item while editing config
        await ConfigEditor.EditConfig();
    }

    [RelayCommand]
    public void Exit()
    {
        AppControl.Shutdown();
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
        BackgroundTask.Enabled = !BackgroundTask.Enabled;

        OnPropertyChanged(nameof(EnableDisableText));
        OnPropertyChanged(nameof(IconUri));
    }

    public string EnableDisableText => BackgroundTask.Enabled ? "Disable" : "Enable";

    public string IconUri => BackgroundTask.Enabled
        ? "pack://application:,,,/UserInterface/TrayEnabled.ico"
        : "pack://application:,,,/UserInterface/TrayDisabled.ico";

    IAppControl AppControl { get; }
    IBackgroundTask BackgroundTask { get; }
    IConfigEditor ConfigEditor { get; }
    IAutostart Autostart { get; }
}
