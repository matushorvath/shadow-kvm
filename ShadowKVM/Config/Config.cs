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
                .WithTypeConverter(new VcpCodeConverter())
                .WithTypeConverter(new VcpValueConverter())
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

internal class ActionConfig
{
    public VcpCode Code { get; set; } = new VcpCode((byte)0);
    public VcpValue Value { get; set; } = new VcpValue((byte)0);
}
