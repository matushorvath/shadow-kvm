using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShadowKVM;

public partial class AboutViewModel : ObservableObject
{
    public AboutViewModel()
        : this(Services.Instance.NativeUserInterface)
    {
    }

    public AboutViewModel(INativeUserInterface nativeUserInterface)
    {
        NativeUserInterface = nativeUserInterface;
    }

    INativeUserInterface NativeUserInterface { get; }

    public event Action? RequestClose;

    [RelayCommand]
    public void Close()
    {
        RequestClose?.Invoke();
    }

    [ObservableProperty]
    string version = GitVersionInformation.FullSemVer;

    [RelayCommand]
    public void OpenLicense()
    {
        NativeUserInterface.OpenUrl("https://opensource.org/license/mit");
    }

    [RelayCommand]
    public void OpenManual()
    {
        NativeUserInterface.OpenUrl("https://github.com/matushorvath/shadow-kvm#readme-ov-file");
    }

    [RelayCommand]
    public void OpenReleases()
    {
        NativeUserInterface.OpenUrl("https://github.com/matushorvath/shadow-kvm/releases");
    }
}
