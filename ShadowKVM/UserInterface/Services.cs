using System.IO.Abstractions;

namespace ShadowKVM;

internal class Services
{
    // TODO can this be simply a constructor? then also simplify properties
    public void ConstructDefault(string dataDirectory)
    {
        FileSystem = new FileSystem();
        ConfigService = new ConfigService(dataDirectory, FileSystem);
        MonitorService = new MonitorService();
        ConfigGenerator = new ConfigGenerator(MonitorService);
    }

    ConfigGenerator? _configGenerator;
    public ConfigGenerator ConfigGenerator
    {
        get => _configGenerator!;
        set => _configGenerator = value;
    }

    ConfigService? _configService;
    public ConfigService ConfigService
    {
        get => _configService!;
        set => _configService = value;
    }

    IFileSystem? _fileSystem;
    public IFileSystem FileSystem
    {
        get => _fileSystem!;
        set => _fileSystem = value;
    }

    MonitorService? _monitorService;
    public MonitorService MonitorService
    {
        get => _monitorService!;
        set => _monitorService = value;
    }
}
