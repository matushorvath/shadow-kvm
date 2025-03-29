using System.Windows;

namespace ShadowKVM;

public partial class ConfigGeneratorWindow : Window
{
    public ConfigGeneratorWindow()
    {
        InitializeComponent();
    }

    public ConfigGeneratorViewModel ViewModel => (ConfigGeneratorViewModel)DataContext;
}
