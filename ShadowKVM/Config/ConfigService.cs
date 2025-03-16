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
    public ConfigService(string dataDirectory, IFileSystem fileSystem)
    {
        ConfigPath = Path.Combine(dataDirectory, "config.yaml");
        FileSystem = fileSystem;
    }

    public string ConfigPath { get; }
    IFileSystem FileSystem { get; }

    public bool NeedReloadConfig(Config config)
    {
        if (config.LoadedChecksum == null)
        {
            return true;
        }

        using (var stream = FileSystem.File.OpenRead(ConfigPath))
        using (var md5 = MD5.Create())
        {
            return !config.LoadedChecksum.SequenceEqual(md5.ComputeHash(stream));
        }
    }

    public Config LoadConfig()
    {
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

            using (var stream = FileSystem.File.OpenRead(ConfigPath))
            using (var input = new StreamReader(stream))
            {
                config = deserializer.Deserialize<Config>(input);
            }

            ValidateConfig(config);

            using (var stream = FileSystem.File.OpenRead(ConfigPath))
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

    void ValidateConfig(Config config)
    {
        if (config.Version != 1)
        {
            throw new ConfigException($"Unsupported configuration version (found {config.Version}, supporting 1)");
        }

        if (config.Monitors == null || config.Monitors.Count == 0)
        {
            throw new ConfigException($"At least one monitor needs to be specified in configuration");
        }

        foreach (var monitor in config.Monitors ?? [])
        {
            if (monitor.Description == null && monitor.Adapter == null && monitor.SerialNumber == null)
            {
                throw new ConfigException($"Either description, adapter or serial-number needs to be specified for each monitor");
            }

            if (monitor.Attach == null && monitor.Detach == null)
            {
                throw new ConfigException($"Either attach or detach action needs to be specified for each monitor");
            }
        }
    }
}
