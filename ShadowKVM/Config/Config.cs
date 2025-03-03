using Serilog;
using Serilog.Events;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ShadowKVM;

internal class Config
{
    public static Config Load(string dataDirectory)
    {
        // TODO handle missing config, create it automatically/display config window

        var configPath = Path.Combine(dataDirectory, "config.yaml");
        Log.Information("Loading configuration from {ConfigPath}", configPath);

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(HyphenatedNamingConvention.Instance)
                .WithTypeConverter(new TriggerDeviceConverter())
                .WithTypeConverter(new OpenEnumByteYamlTypeConverter<VcpCodeEnum>())
                .WithTypeConverter(new OpenEnumByteYamlTypeConverter<VcpValueEnum>())
                .Build();

            Config config;
            using (var input = new StreamReader(configPath))
            {
                config = deserializer.Deserialize<Config>(input);
            }

            if (config.Version != 1)
            {
                throw new ConfigException($"Unsupported configuration version (found {config.Version}, supporting 1)");
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
    DVI1 = 0x03,
    DVI2 = 0x04,
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
    HDMI1 = 0x11,
    HDMI2 = 0x12
}

internal class ActionConfig
{
    public OpenEnumByte<VcpCodeEnum> Code { get; set; } = new OpenEnumByte<VcpCodeEnum>();
    public OpenEnumByte<VcpValueEnum> Value { get; set; } = new OpenEnumByte<VcpValueEnum>();
}
