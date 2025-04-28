using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace ShadowKVM;

[ExcludeFromCodeCoverage(Justification = "User interface code")]
public partial class ConfigGeneratorWindow : Window
{
    public ConfigGeneratorWindow()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // Close the window when the generation is completed
        DataContext.GenerationCompleted += sender => Dispatcher.Invoke(Close);
    }

    public new ConfigGeneratorViewModel DataContext
    {
        get => (ConfigGeneratorViewModel)base.DataContext;
        set => base.DataContext = value;
    }
}
