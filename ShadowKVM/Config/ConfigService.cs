using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using Serilog;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ShadowKVM;

public interface IConfigService
{
    string ConfigPath { get; }
    Config Config { get; }
    event Action<IConfigService>? ConfigChanged;

    bool ReloadConfig();
}

public class ConfigService : IConfigService
{
    public ConfigService(string dataDirectory, IFileSystem fileSystem, ILogger logger)
    {
        ConfigPath = Path.Combine(dataDirectory, "config.yaml");

        FileSystem = fileSystem;
        Logger = logger;
    }

    public string ConfigPath { get; }

    Config? _config;
    public Config Config => _config ?? throw new InvalidOperationException("Configuration is not loaded");

    public event Action<IConfigService>? ConfigChanged;

    IFileSystem FileSystem { get; }
    ILogger Logger { get; }

    public bool ReloadConfig()
    {
        if (!IsConfigChanged())
        {
            Logger.Information("Configuration file {ConfigPath} has not changed, skipping reload", ConfigPath);
            return false;
        }

        LoadConfig();
        return true;
    }

    bool IsConfigChanged()
    {
        if (_config?.LoadedChecksum == null)
        {
            return true;
        }

        using (var stream = FileSystem.File.OpenRead(ConfigPath))
        using (var md5 = MD5.Create())
        {
            return !_config.LoadedChecksum.SequenceEqual(md5.ComputeHash(stream));
        }
    }

    void LoadConfig()
    {
        try
        {
            Logger.Information("Loading configuration from {ConfigPath}", ConfigPath);

            var namingConvention = HyphenatedNamingConvention.Instance;

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(namingConvention)
                .WithTypeConverter(new TriggerDeviceConverter(namingConvention))
                .WithTypeConverter(new OpenEnumByteYamlTypeConverter<VcpCodeEnum>(namingConvention))
                .WithTypeConverter(new OpenEnumByteYamlTypeConverter<VcpValueEnum>(namingConvention))
                .Build();

            using (var stream = FileSystem.File.OpenRead(ConfigPath))
            using (var input = new StreamReader(stream))
            {
                _config = deserializer.Deserialize<Config>(input);
            }

            ValidateConfig();

            using (var stream = FileSystem.File.OpenRead(ConfigPath))
            using (var md5 = MD5.Create())
            {
                Config.LoadedChecksum = md5.ComputeHash(stream);
            }

            ConfigChanged?.Invoke(this);
        }
        catch (YamlException exception)
        {
            throw new ConfigFileException(ConfigPath, exception);
        }
    }

    void ValidateConfig()
    {
        if (Config.Version != 1)
        {
            throw new ConfigException($"Unsupported configuration version (found {Config.Version}, supporting 1)");
        }

        if (Config.Monitors == null || Config.Monitors.Count == 0)
        {
            throw new ConfigException($"At least one monitor needs to be specified in configuration");
        }

        foreach (var monitor in Config.Monitors)
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
