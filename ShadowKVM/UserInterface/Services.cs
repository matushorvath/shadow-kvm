using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Serilog;

// TODO write unit tests, this looks testable

namespace ShadowKVM;

[ExcludeFromCodeCoverage(Justification = "Productive implementations of the service interfaces")]
public class Services : IDisposable
{
    static Services? _instance;
    public static Services Instance => _instance ?? (_instance = new Services());

    Services()
    {
        AppControl = new AppControl();

        FileSystem = new FileSystem();
        ConfigService = new ConfigService(FileSystem, Log.Logger);

        WindowsAPI = new WindowsAPI();
        MonitorService = new MonitorService(WindowsAPI, Log.Logger);

        CapabilitiesParser = new CapabilitiesParser(Log.Logger);
        MonitorInputService = new MonitorInputService(WindowsAPI, CapabilitiesParser, Log.Logger);

        ConfigGenerator = new ConfigGenerator(MonitorService, MonitorInputService);

        NativeUserInterface = new NativeUserInterface();
        ConfigEditor = new ConfigEditor(ConfigService, AppControl, NativeUserInterface);

        DeviceNotificationService = new DeviceNotificationService(WindowsAPI, Log.Logger);
        BackgroundTask = new BackgroundTask(ConfigService, DeviceNotificationService, MonitorService, WindowsAPI, Log.Logger);

        Autostart = new Autostart(Log.Logger);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!_disposed)
            {
                BackgroundTask.Dispose();
                _disposed = true;
            }
        }
    }

    public IAppControl AppControl { get; }
    public ICapabilitiesParser CapabilitiesParser { get; }
    public IConfigGenerator ConfigGenerator { get; }
    public IConfigService ConfigService { get; }
    public IDeviceNotificationService DeviceNotificationService { get; }
    public IFileSystem FileSystem { get; }
    public IWindowsAPI WindowsAPI { get; }
    public IMonitorInputService MonitorInputService { get; }
    public IMonitorService MonitorService { get; }
    public IBackgroundTask BackgroundTask { get; }
    public IAutostart Autostart { get; }
    public INativeUserInterface NativeUserInterface { get; }
    public IConfigEditor ConfigEditor { get; }

    bool _disposed = false;
}
