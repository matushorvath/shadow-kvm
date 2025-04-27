using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace ShadowKVM;

[ExcludeFromCodeCoverage(Justification = "User interface code")]
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // Close the window when the view model requests it
        DataContext.RequestClose += sender => Dispatcher.Invoke(Close);
    }

    public new AboutViewModel DataContext
    {
        get => (AboutViewModel)base.DataContext;
        set => base.DataContext = value;
    }
}
