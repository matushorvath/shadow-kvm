using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Serilog;

namespace ShadowKVM;

[ExcludeFromCodeCoverage(Justification = "Productive implementations of the service interfaces")]
public class Services : IDisposable
{
    public Services(string dataDirectory)
    {
        FileSystem = new FileSystem();
        ConfigService = new ConfigService(dataDirectory, FileSystem);

        WindowsAPI = new WindowsAPI();
        MonitorService = new MonitorService(WindowsAPI, Log.Logger);

        CapabilitiesParser = new CapabilitiesParser(Log.Logger);
        MonitorInputService = new MonitorInputService(WindowsAPI, CapabilitiesParser, Log.Logger);

        ConfigGenerator = new ConfigGenerator(MonitorService, MonitorInputService);

        DeviceNotificationService = new DeviceNotificationService(WindowsAPI);
        BackgroundTask = new BackgroundTask(DeviceNotificationService, MonitorService, WindowsAPI, Log.Logger);

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

    bool _disposed = false;
}
