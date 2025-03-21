using System.IO.Abstractions;
using Serilog;

namespace ShadowKVM;

internal class Services
{
    public Services(string dataDirectory)
    {
        FileSystem = new FileSystem();
        ConfigService = new ConfigService(dataDirectory, FileSystem);

        MonitorAPI = new MonitorAPI();
        MonitorService = new MonitorService(MonitorAPI, Log.Logger);
        MonitorInputService = new MonitorInputService(MonitorAPI, Log.Logger);

        ConfigGenerator = new ConfigGenerator(MonitorService, MonitorInputService);
    }

    public IConfigGenerator ConfigGenerator { get; }
    public IConfigService ConfigService { get; }
    public IFileSystem FileSystem { get; }
    public IMonitorAPI MonitorAPI { get; }
    public IMonitorService MonitorService { get; }
    public IMonitorInputService MonitorInputService { get; }
}
