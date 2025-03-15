using Serilog;
using Serilog.Events;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ShadowKVM;

internal class Config
{
    public static Config Load(string configPath)
    {
        return Load(configPath, new FileSystem());
    }

    public static Config Load(string configPath, IFileSystem fileSystem)
    {
        Log.Information("Loading configuration from {ConfigPath}", configPath);

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

            using (var stream = fileSystem.File.OpenRead(configPath))
            using (var input = new StreamReader(stream))
            {
                config = deserializer.Deserialize<Config>(input);
            }

            if (config.Version != 1)
            {
                throw new ConfigException($"Unsupported configuration version (found {config.Version}, supporting 1)");
            }

            using (var stream = fileSystem.File.OpenRead(configPath))
            using (var md5 = MD5.Create())
            {
                config._loadedChecksum = md5.ComputeHash(stream);
            }

            return config;
        }
        catch (YamlException exception)
        {
            throw new ConfigFileException(configPath, exception);
        }
    }

    public int Version { get; set; }
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;
    public TriggerDevice TriggerDevice { get; set; } = new TriggerDevice(TriggerDevice.DeviceTypeEnum.Keyboard);

    public List<MonitorConfig> Monitors { get; set; } = new List<MonitorConfig>();

    byte[]? _loadedChecksum;

    public bool HasChanged(string configPath)
    {
        return HasChanged(configPath, new FileSystem());
    }

    public bool HasChanged(string configPath, IFileSystem fileSystem)
    {
        if (_loadedChecksum == null)
        {
            return true;
        }

        using (var stream = fileSystem.File.OpenRead(configPath))
        using (var md5 = MD5.Create())
        {
            return !_loadedChecksum.SequenceEqual(md5.ComputeHash(stream));
        }
    }
}

internal class MonitorConfig
{
    public string? Description { get; set; }
    public string? Adapter { get; set; }
    public string? SerialNumber { get; set; }

    public ActionConfig? Attach { get; set; }
    public ActionConfig? Detach { get; set; }
}

internal enum VcpCodeEnum : byte
{
    InputSelect = 0x60
}

internal enum VcpValueEnum : byte
{
    Analog1 = 0x01,
    Analog2 = 0x02,
    Dvi1 = 0x03,
    Dvi2 = 0x04,
    Composite1 = 0x05,
    Composite2 = 0x06,
    SVideo1 = 0x07,
    SVideo2 = 0x08,
    Tuner1 = 0x09,
    Tuner2 = 0x0A,
    Tuner3 = 0x0B,
    Component1 = 0x0C,
    Component2 = 0x0D,
    Component3 = 0x0E,
    DisplayPort1 = 0x0F,
    DisplayPort2 = 0x10,
    Hdmi1 = 0x11,
    Hdmi2 = 0x12
}

internal class ActionConfig
{
    public OpenEnumByte<VcpCodeEnum> Code { get; set; } = new OpenEnumByte<VcpCodeEnum>();
    public OpenEnumByte<VcpValueEnum> Value { get; set; } = new OpenEnumByte<VcpValueEnum>();
}
