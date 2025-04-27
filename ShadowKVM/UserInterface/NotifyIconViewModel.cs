using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

// TODO test About(), it's testable now

namespace ShadowKVM;

public partial class NotifyIconViewModel : ObservableObject
{
    public NotifyIconViewModel()
        : this(Services.Instance.AppControl, Services.Instance.Autostart, Services.Instance.BackgroundTask,
        Services.Instance.ConfigEditor, Services.Instance.NativeUserInterface)
    {
    }

    public NotifyIconViewModel(IAppControl appControl, IAutostart autostart, IBackgroundTask backgroundTask,
        IConfigEditor configEditor, INativeUserInterface nativeUserInterface)
    {
        AppControl = appControl;
        Autostart = autostart;
        BackgroundTask = backgroundTask;
        ConfigEditor = configEditor;
        NativeUserInterface = nativeUserInterface;

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
        // Making this async grays out the menu item while the window is open
        var tcs = new TaskCompletionSource();

        var aboutViewContext = new AboutViewModel();
        NativeUserInterface.ShowWindow((AboutWindow aboutWindow) =>
        {
            aboutWindow.DataContext = aboutViewContext;
            aboutWindow.Closed += (_, _) => tcs.SetResult();
        });

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
    IAutostart Autostart { get; }
    IBackgroundTask BackgroundTask { get; }
    IConfigEditor ConfigEditor { get; }
    INativeUserInterface NativeUserInterface { get; }
}
