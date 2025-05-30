using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace ShadowKVM;

public interface INativeUserInterface
{
    Task OpenEditor(string path);
    void OpenUrl(string url);

    void ErrorBox(string message, string title);
    void InfoBox(string message, string title);
    bool QuestionBox(string message, string title);

    void ShowWindow<TWindow, TDataContext>(TDataContext? dataContext = null, EventHandler? closedHandler = null)
        where TWindow : Window, new()
        where TDataContext : class;
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

    public void ErrorBox(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void InfoBox(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public bool QuestionBox(string message, string title)
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    public void ShowWindow<TWindow, TDataContext>(TDataContext? dataContext = null, EventHandler? closedHandler = null)
        where TWindow : Window, new()
        where TDataContext : class
    {
        var window = new TWindow();

        if (dataContext != null)
        {
            window.DataContext = dataContext;
        }

        if (closedHandler != null)
        {
            window.Closed += closedHandler;
        }

        window.Show();
    }
}
