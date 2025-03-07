using System.ComponentModel;
using System.Windows;

namespace ShadowKVM;

public partial class ProgressWindow : Window
{
    public ProgressWindow()
    {
        InitializeComponent();
    }

    public static void Execute(Action<IProgress<int>> work)
    {
        var window = new ProgressWindow();

        window.Loaded += (_, args) =>
        {
            Progress<int> progress = new Progress<int>(percent => window.progressTextBox.Text = $"{percent}%");

            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += (_, _) => work(progress);
            worker.RunWorkerCompleted += (_, _) => window.Close();

            worker.RunWorkerAsync();
        };

        window.ShowDialog();
    }
}
