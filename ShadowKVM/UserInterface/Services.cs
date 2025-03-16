using System.IO.Abstractions;

namespace ShadowKVM;

internal class Services
{
    public Services(string dataDirectory)
    {
        FileSystem = new FileSystem();
        ConfigService = new ConfigService(dataDirectory, FileSystem);

        MonitorAPI = new MonitorAPI();
        MonitorService = new MonitorService(MonitorAPI);
        ConfigGenerator = new ConfigGenerator(MonitorService);
    }

    public IConfigGenerator ConfigGenerator { get; }
    public IConfigService ConfigService { get; }
    public IFileSystem FileSystem { get; }
    public IMonitorAPI MonitorAPI { get; }
    public IMonitorService MonitorService { get; }
}
