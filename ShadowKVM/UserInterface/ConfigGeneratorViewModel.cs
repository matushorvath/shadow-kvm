using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ShadowKVM;

public partial class ConfigGeneratorViewModel : ObservableObject
{
    public ConfigGeneratorViewModel()
    {
        Progress = new Progress<ConfigGeneratorStatus>(UpdateProgress);
    }

    [ObservableProperty]
    int current = 0;

    [ObservableProperty]
    int maximum = 1;

    public Progress<ConfigGeneratorStatus> Progress { get; init; }
    public event Action<object>? GenerationCompleted;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Current) || e.PropertyName == nameof(Maximum))
        {
            if (Current >= Maximum)
            {
                GenerationCompleted?.Invoke(this);
            }
        }

        base.OnPropertyChanged(e);
    }

    void UpdateProgress(ConfigGeneratorStatus status)
    {
        Maximum = status.Maximum;
        Current = status.Current;
    }
}
