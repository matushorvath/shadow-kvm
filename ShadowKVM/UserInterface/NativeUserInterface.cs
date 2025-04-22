using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace ShadowKVM;

public interface INativeUserInterface
{
    Task OpenEditor(string path);
    void OpenUrl(string url);
    void InfoBox(string message, string title);
}

[ExcludeFromCodeCoverage(Justification = "Productive implementation of the native UI interface")]
public class NativeUserInterface : INativeUserInterface
{
    public async Task OpenEditor(string path)
    {
        var process = Process.Start("notepad.exe", path);
        if (process == null)
        {
            throw new Exception("Failed to start notepad");
        }

        await process.WaitForExitAsync();
    }

    public void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }

    public void InfoBox(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
