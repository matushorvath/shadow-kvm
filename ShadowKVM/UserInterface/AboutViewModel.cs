using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShadowKVM;

// TODO write unit tests

public partial class AboutViewModel : ObservableObject
{
    public event EventHandler? RequestClose;

    [RelayCommand]
    public void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    public ImageSource Icon => new BitmapImage(new Uri("pack://application:,,,/UserInterface/Application.ico"));

    [ObservableProperty]
    string version = GitVersionInformation.FullSemVer;

    [RelayCommand]
    public void OpenLicense()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://opensource.org/license/mit",
            UseShellExecute = true
        });
    }

    [RelayCommand]
    public void OpenManual()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/matushorvath/shadow-kvm#readme-ov-file",
            UseShellExecute = true
        });
    }

    [RelayCommand]
    public void OpenReleases()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/matushorvath/shadow-kvm/releases",
            UseShellExecute = true
        });
    }
}
