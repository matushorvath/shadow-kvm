using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace ShadowKVM;

[ExcludeFromCodeCoverage(Justification = "User interface code")]
public partial class ConfigGeneratorWindow : Window
{
    public ConfigGeneratorWindow()
    {
        InitializeComponent();
    }

    public ConfigGeneratorViewModel ViewModel => (ConfigGeneratorViewModel)DataContext;
}
