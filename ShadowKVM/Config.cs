using Serilog.Events;
using System.IO;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Windows.Win32;

namespace ShadowKVM;

internal class Config
{
    public static Config Load(string dataDirectory)
    {
        // TODO handle missing config, create it automatically/display config window

        var configPath = Path.Combine(dataDirectory, "config.yaml");

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(HyphenatedNamingConvention.Instance)
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

    public Guid DeviceClassGuid
    {
        get
        {
            switch (DeviceType)
            {
                case DeviceTypeEnum.Keyboard: return PInvoke.GUID_DEVINTERFACE_KEYBOARD;
                case DeviceTypeEnum.Mouse: return PInvoke.GUID_DEVINTERFACE_MOUSE;
                default: return DeviceClass ?? throw new ConfigException("Either device-type or device-class must be set");
            }
        }
    }

    public int Version { get; set; }
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

    // TODO require one or the other
    public enum DeviceTypeEnum { Keyboard, Mouse }
    public DeviceTypeEnum? DeviceType { get; set; }
    public Guid? DeviceClass { get; set; }

    public List<MonitorConfig> Monitors { get; set; } = new List<MonitorConfig>();
}

internal class MonitorConfig
{
    public string? Description { get; set; }
    public string? Device { get; set; }
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
