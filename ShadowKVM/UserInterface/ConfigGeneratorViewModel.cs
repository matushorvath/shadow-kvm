using CommunityToolkit.Mvvm.ComponentModel;

namespace ShadowKVM;

// TODO write unit tests

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

    void UpdateProgress(ConfigGeneratorStatus status)
    {
        Maximum = status.Maximum;
        Current = status.Current;
    }
}
