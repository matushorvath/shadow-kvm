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
    void SetDataDirectory(string dataDirectory);

    Config Config { get; }
    event Action<IConfigService>? ConfigChanged;

    bool ReloadConfig();
}

public class ConfigService(IFileSystem fileSystem, ILogger logger) : IConfigService
{
    string? _configPath;
    public string ConfigPath => _configPath ?? throw new InvalidOperationException("Data directory is not set");

    public void SetDataDirectory(string dataDirectory)
    {
        _configPath = Path.Combine(dataDirectory, "config.yaml");
    }

    Config? _config;
    public Config Config => _config ?? throw new InvalidOperationException("Configuration is not loaded");

    public event Action<IConfigService>? ConfigChanged;

    public bool ReloadConfig()
    {
        if (!IsConfigChanged())
        {
            logger.Information("Configuration file {ConfigPath} has not changed, skipping reload", ConfigPath);
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

        using (var stream = fileSystem.File.OpenRead(ConfigPath))
        using (var md5 = MD5.Create())
        {
            return !_config.LoadedChecksum.SequenceEqual(md5.ComputeHash(stream));
        }
    }

    void LoadConfig()
    {
        try
        {
            logger.Information("Loading configuration from {ConfigPath}", ConfigPath);

            var namingConvention = HyphenatedNamingConvention.Instance;

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(namingConvention)
                .WithTypeConverter(new TriggerDeviceTypeConverter())
                .WithTypeConverter(new TriggerDeviceClassTypeConverter(namingConvention))
                .WithTypeConverter(new OpenEnumByteYamlTypeConverter<VcpCodeEnum>(namingConvention))
                .WithTypeConverter(new OpenEnumByteYamlTypeConverter<VcpValueEnum>(namingConvention))
                .Build();

            using (var stream = fileSystem.File.OpenRead(ConfigPath))
            using (var input = new StreamReader(stream))
            {
                _config = deserializer.Deserialize<Config>(input);
            }

            ValidateConfig();

            using (var stream = fileSystem.File.OpenRead(ConfigPath))
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
        ValidateVersion();

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

    void ValidateVersion()
    {
        if (Config.Version == 1)
        {
            if (Config.TriggerDevice.LoadedVersion != null && Config.TriggerDevice.LoadedVersion > 1)
            {
                throw new ConfigException("Invalid TriggerDevice format for configuration version 1");
            }
        }
        else if (Config.Version == 2)
        {
            if (Config.TriggerDevice.LoadedVersion != null && Config.TriggerDevice.LoadedVersion < 2)
            {
                throw new ConfigException("Invalid TriggerDevice format for configuration version 2");
            }
        }
        else
        {
            throw new ConfigException($"Unsupported configuration version (found {Config.Version}, supporting <= 2)");
        }
    }
}
