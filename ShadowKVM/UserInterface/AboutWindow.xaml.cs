using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace ShadowKVM;

[ExcludeFromCodeCoverage(Justification = "User interface code")]
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        ViewModel.RequestClose += Close;
    }

    public AboutViewModel ViewModel => (AboutViewModel)DataContext;
}
