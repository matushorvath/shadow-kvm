using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

internal class Config
{
    public static Config Load(string path)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(HyphenatedNamingConvention.Instance)
                .Build();

            using (var input = new StreamReader(path))
            {
                return deserializer.Deserialize<Config>(input);
            }
        }
        catch (YamlException exception)
        {
            throw new ConfigException(path, exception);
        }
    }

    public Config()
    {
        Monitors = new List<MonitorConfig>();
    }

    public int Version { get; set; }
    public List<MonitorConfig> Monitors { get; set; }
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
    public ConfigException(string path, YamlException exception)
        : base(null, exception)
    {
        _path = path;
    }

    public override string Message
    {
        get
        {
            if (InnerException is YamlException)
            {
                return FormatMessageYaml();
            }

            var innerMessage = InnerException?.Message != null ? $" ({InnerException?.Message})" : "";
            return $"Config file processing error{innerMessage}";
        }
    }

    private string FormatMessageYaml()
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
