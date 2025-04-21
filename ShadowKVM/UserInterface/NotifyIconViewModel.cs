using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShadowKVM;

public partial class NotifyIconViewModel : ObservableObject
{
    public NotifyIconViewModel()
        : this(Services.Instance.AppControl, Services.Instance.BackgroundTask, Services.Instance.ConfigEditor, Services.Instance.Autostart)
    {
    }

    public NotifyIconViewModel(IAppControl appControl, IBackgroundTask backgroundTask, IConfigEditor configEditor, IAutostart autostart)
    {
        AppControl = appControl;
        BackgroundTask = backgroundTask;
        ConfigEditor = configEditor;
        Autostart = autostart;

        isAutostart = Autostart.IsEnabled();

        // Disable the menu item while the config editor is open
        configEditor.ConfigEditorOpened += () => IsConfigEditorEnabled = false;
        configEditor.ConfigEditorClosed += () => IsConfigEditorEnabled = true;
    }

    [RelayCommand(FlowExceptionsToTaskScheduler = true)]
    public async Task Configure()
    {
        await ConfigEditor.EditConfig();
    }

    [ObservableProperty]
    bool isConfigEditorEnabled = true;

    [RelayCommand]
    public void Exit()
    {
        AppControl.Shutdown();
    }

    [RelayCommand(FlowExceptionsToTaskScheduler = true)]
    public Task About()
    {
        // TODO refactor to make this testable (do not open a window directly)

        // Making this async grays out the menu item while the window is open
        var tcs = new TaskCompletionSource();

        var aboutWindow = new AboutWindow();
        aboutWindow.Closed += (_, _) => tcs.SetResult();
        aboutWindow.Show();

        return tcs.Task;
    }

    [ObservableProperty]
    bool isAutostart = false;

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
