using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShadowKVM;

public interface IUrlOpener
{
    void Open(string url);
}

[ExcludeFromCodeCoverage(Justification = "Productive implementation of the URL Opener interface")]
public class UrlOpener : IUrlOpener
{
    public void Open(string url)
    {
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }
}

public partial class AboutViewModel : ObservableObject
{
    public AboutViewModel(IUrlOpener? urlOpener = default)
    {
        UrlOpener = urlOpener ?? new UrlOpener(); // TODO use Services.UrlOpener?
    }

    IUrlOpener UrlOpener { get; }

    public event EventHandler? RequestClose;

    [RelayCommand]
    public void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [ObservableProperty]
    string version = GitVersionInformation.FullSemVer;

    [RelayCommand]
    public void OpenLicense()
    {
        UrlOpener.Open("https://opensource.org/license/mit");
    }

    [RelayCommand]
    public void OpenManual()
    {
        UrlOpener.Open("https://github.com/matushorvath/shadow-kvm#readme-ov-file");
    }

    [RelayCommand]
    public void OpenReleases()
    {
        UrlOpener.Open("https://github.com/matushorvath/shadow-kvm/releases");
    }
}
