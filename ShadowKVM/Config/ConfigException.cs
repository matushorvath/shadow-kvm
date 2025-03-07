using System.Text;
using YamlDotNet.Core;

namespace ShadowKVM;

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
