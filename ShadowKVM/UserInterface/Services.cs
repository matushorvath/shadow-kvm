using System.IO.Abstractions;
using Serilog;

namespace ShadowKVM;

internal class Services
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

        DeviceNotificationFactory = new DeviceNotificationFactory(WindowsAPI);
    }

    public ICapabilitiesParser CapabilitiesParser { get; }
    public IConfigGenerator ConfigGenerator { get; }
    public IConfigService ConfigService { get; }
    public IDeviceNotificationFactory DeviceNotificationFactory { get; }
    public IFileSystem FileSystem { get; }
    public IWindowsAPI WindowsAPI { get; }
    public IMonitorInputService MonitorInputService { get; }
    public IMonitorService MonitorService { get; }
}
