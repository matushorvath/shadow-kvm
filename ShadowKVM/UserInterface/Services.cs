using System.IO.Abstractions;

namespace ShadowKVM;

internal class Services
{
    public void ConstructDefault(string dataDirectory)
    {
        FileSystem = new FileSystem();
        ConfigService = new ConfigService(dataDirectory, FileSystem);
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
}
