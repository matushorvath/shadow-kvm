using System.ComponentModel;
using System.Windows;

namespace ShadowKVM;

public partial class ProgressWindow : Window
{
    public ProgressWindow()
    {
        InitializeComponent();
    }

    public static void Execute(Action<IProgress<ConfigGeneratorStatus>> work)
    {
        var window = new ProgressWindow();

        window.Loaded += (_, args) =>
        {
            var progress = new Progress<ConfigGeneratorStatus>(status =>
            {
                window.progressBar.Maximum = status.Maximum;
                window.progressBar.Value = status.Current;
            });

            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += (_, _) => work(progress);
            worker.RunWorkerCompleted += (_, _) => window.Close();

            worker.RunWorkerAsync();
        };

        window.ShowDialog();
    }
}
