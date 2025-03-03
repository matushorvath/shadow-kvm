using Serilog;
using Serilog.Events;
using System.IO;
using System.Text;
using Windows.Win32;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Core.Events;

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

internal class TriggerDevice
{
    public enum DeviceTypeEnum { Keyboard, Mouse }

    public TriggerDevice(DeviceTypeEnum deviceType)
    {
        DeviceType = deviceType;
    }

    public TriggerDevice(Guid guid)
    {
        Guid = guid;
    }

    DeviceTypeEnum? _deviceType;
    public DeviceTypeEnum? DeviceType
    {
        get => _deviceType;
        set
        {
            _deviceType = value;

            switch (_deviceType)
            {
                case DeviceTypeEnum.Keyboard:
                    _guid = PInvoke.GUID_DEVINTERFACE_KEYBOARD;
                    break;
                case DeviceTypeEnum.Mouse:
                    _guid = PInvoke.GUID_DEVINTERFACE_MOUSE;
                    break;
                default:
                    throw new ConfigException($"Invalid trigger device type {value} in configuration file");
            }
        }
    }

    Guid _guid;
    public Guid Guid
    {
        get => _guid;
        set
        {
            _guid = value;
            _deviceType = null;
        }
    }
}

internal class TriggerDeviceConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(TriggerDevice);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var startMark = parser.Current?.Start ?? Mark.Empty;
        var endMark = parser.Current?.End ?? Mark.Empty;

        var value = parser.Consume<Scalar>().Value;

        TriggerDevice.DeviceTypeEnum deviceType;
        if (Enum.TryParse(value, true, out deviceType))
        {
            return new TriggerDevice(deviceType);
        }

        Guid guid;
        if (Guid.TryParse(value, out guid))
        {
            return new TriggerDevice(guid);
        }

        throw new YamlException(startMark, endMark, $"Invalid trigger device \"{value}\"");
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var triggerDevice = (TriggerDevice)value!;

        if (triggerDevice.DeviceType != null)
        {
            emitter.Emit(new Scalar(triggerDevice.DeviceType?.ToString() ?? string.Empty));
        }
        else
        {
            emitter.Emit(new Scalar(triggerDevice.Guid.ToString("D")));
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

internal class ActionConfig
{
    public byte Code { get; set; }
    public byte Value { get; set; }
}

internal class ConfigException : Exception
{
    public ConfigException(string message)
        : base(message)
    {
    }

    public ConfigException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

internal class ConfigFileException : ConfigException
{
    public ConfigFileException(string path, YamlException exception)
        : base(string.Empty, exception)
    {
        _path = path;
    }

    public override string Message
    {
        get
        {
            if (InnerException is YamlException)
            {
                return FormatYamlMessage();
            }

            var innerMessage = InnerException?.Message != null ? $" ({InnerException?.Message})" : "";
            return $"Config file processing error{innerMessage}";
        }
    }

    private string FormatYamlMessage()
    {
        var builder = new StringBuilder();

        builder.Append(_path);

        var yamlException = InnerException as YamlException;
        if (yamlException?.Start != null)
        {
            builder.Append('(');
            builder.Append(yamlException?.Start.Line);
            builder.Append(',');
            builder.Append(yamlException?.Start.Column);
            builder.Append(')');
        }

        builder.Append(": ");

        if (InnerException?.Message != null)
        {
            builder.Append(InnerException.Message);

            if (InnerException.InnerException?.Message != null)
            {
                builder.Append(": ");
                builder.Append(InnerException.InnerException.Message);
            }
        }
        else 
        {
            builder.Append("YAML processing error");
        }

        return builder.ToString();
    }

    string _path;
}
