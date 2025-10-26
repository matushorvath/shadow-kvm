using System.Diagnostics;
using System.Text;
using YamlDotNet.Core;

namespace ShadowKVM;

public class ConfigException : Exception
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

public class ConfigFileException : ConfigException
{
    public ConfigFileException(string path, YamlException exception)
        : base(string.Empty, exception)
    {
        _path = path;
    }

    public override string Message => FormatYamlMessage();

    string FormatYamlMessage()
    {
        var builder = new StringBuilder();

        builder.Append(_path);

        // Inner exception is mandatory in the constructor
        Debug.Assert(InnerException != null);
        YamlException yamlException = (YamlException)InnerException;

        builder.Append('(');
        builder.Append(yamlException.Start.Line);
        builder.Append(',');
        builder.Append(yamlException.Start.Column);
        builder.Append(')');

        builder.Append(": ");

        builder.Append(yamlException.Message);

        if (yamlException.InnerException?.Message != null)
        {
            builder.Append(": ");
            builder.Append(yamlException.InnerException.Message);
        }

        return builder.ToString();
    }

    string _path;
}
