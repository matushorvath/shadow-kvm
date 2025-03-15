using Serilog;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ShadowKVM;

internal class ConfigService
{
    public ConfigService(string dataDirectory)
        : this(dataDirectory, new FileSystem())
    {
    }

    public ConfigService(string dataDirectory, IFileSystem fileSystem)
    {
        ConfigPath = Path.Combine(dataDirectory, "config.yaml");
        _fileSystem = fileSystem;
    }

    public string ConfigPath { get; private set; }

    IFileSystem _fileSystem;

    public bool NeedReloadConfig(Config config)
    {
        if (config.LoadedChecksum == null)
        {
            return true;
        }

        using (var stream = _fileSystem.File.OpenRead(ConfigPath))
        using (var md5 = MD5.Create())
        {
            return !config.LoadedChecksum.SequenceEqual(md5.ComputeHash(stream));
        }
    }

    public Config LoadConfig()
    {
        Log.Information("Loading configuration from {ConfigPath}", ConfigPath);

        try
        {
            var namingConvention = HyphenatedNamingConvention.Instance;

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(namingConvention)
                .WithTypeConverter(new TriggerDeviceConverter(namingConvention))
                .WithTypeConverter(new OpenEnumByteYamlTypeConverter<VcpCodeEnum>(namingConvention))
                .WithTypeConverter(new OpenEnumByteYamlTypeConverter<VcpValueEnum>(namingConvention))
                .Build();

            Config config;

            using (var stream = _fileSystem.File.OpenRead(ConfigPath))
            using (var input = new StreamReader(stream))
            {
                config = deserializer.Deserialize<Config>(input);
            }

            if (config.Version != 1)
            {
                throw new ConfigException($"Unsupported configuration version (found {config.Version}, supporting 1)");
            }

            using (var stream = _fileSystem.File.OpenRead(ConfigPath))
            using (var md5 = MD5.Create())
            {
                config.LoadedChecksum = md5.ComputeHash(stream);
            }

            return config;
        }
        catch (YamlException exception)
        {
            throw new ConfigFileException(ConfigPath, exception);
        }
    }
}
