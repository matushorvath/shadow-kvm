using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace ShadowKVM;

public interface IAppControl
{
    void Shutdown();
}

[ExcludeFromCodeCoverage(Justification = "Productive implementation of the AppControl interface")]
public class AppControl : IAppControl
{
    public void Shutdown()
    {
        Application.Current.Shutdown();
    }
}
