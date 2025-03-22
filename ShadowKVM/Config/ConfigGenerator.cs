using HandlebarsDotNet;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using YamlDotNet.Serialization.NamingConventions;

namespace ShadowKVM;

public class ConfigGeneratorStatus
{
    public int Current { get; set; }
    public int Maximum { get; set; }
}

internal interface IConfigGenerator
{
    public string Generate(IProgress<ConfigGeneratorStatus>? progress = null);
}

internal class ConfigGenerator(IMonitorService monitorService, IMonitorInputService monitorInputService) : IConfigGenerator
{
    public unsafe string Generate(IProgress<ConfigGeneratorStatus>? progress = null)
    {
        var template = LoadTemplate();

        return template(new
        {
            Common = new CommonTemplateData(),
            Monitors = LoadMonitorData(progress)
        });
    }

    HandlebarsTemplate<object, object> LoadTemplate()
    {
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ShadowKVM.Config.DefaultConfig.hbs"))
        using (var reader = new StreamReader(stream!))
        {
            return Handlebars.Compile(reader.ReadToEnd());
        }
    }

    List<MonitorTemplateData> LoadMonitorData(IProgress<ConfigGeneratorStatus>? progress)
    {
        var monitorData = new List<MonitorTemplateData>();

        using (var monitors = monitorService.LoadMonitors())
        {
            var status = new ConfigGeneratorStatus { Current = 0, Maximum = monitors.Count() };
            progress?.Report(status);

            foreach (var monitor in monitors)
            {
                // Determine input sources for this monitor
                MonitorInputs? inputs;
                monitorInputService.TryLoadMonitorInputs(monitor, out inputs);

                status.Current++;
                progress?.Report(status);

                monitorData.Add(new MonitorTemplateData
                {
                    Monitor = monitor,
                    Inputs = new MonitorInputsTemplateData(inputs)
                });
            }
        }

        return monitorData;
    }
}

internal class CommonTemplateData
{
    public string AllCodes => FormatAllEnumValues<VcpCodeEnum>();
    public string AllValues => FormatAllEnumValues<VcpValueEnum>();

    string FormatAllEnumValues<TEnum>()
    {
        var values =
            from field in typeof(TEnum).GetFields()
            where (field.Attributes & FieldAttributes.SpecialName) == 0
            select $"{HyphenatedNamingConvention.Instance.Apply(field.Name)} ({field.GetRawConstantValue()})";

        var builders = new List<StringBuilder>();
        foreach (var value in values)
        {
            if (builders.LastOrDefault() == null || builders.Last().Length + value.Length > 90)
            {
                builders.Add(new StringBuilder());
                builders.Last().Append("#   ");
            }
            else
            {
                builders.Last().Append(' ');
            }
            builders.Last().Append(value);
        }

        return string.Join('\n', builders.Select(b => b.ToString()));
    }
}

internal class MonitorTemplateData
{
    public required Monitor Monitor { get; set; }
    public required MonitorInputsTemplateData Inputs { get; set; }
}

internal class MonitorInputsTemplateData(MonitorInputs? inputs)
{
    public string CommentUnsupported => inputs != null ? "  " : "# ";

    public string SelectedInputString => FormatInputString(inputs?.SelectedInput);

    public string UnselectedInputStringAndComment
    {
        get
        {
            // Choose a random input that wasn't selected
            var unselectedInputs = (
                from input in inputs?.ValidInputs ?? []
                where input != inputs?.SelectedInput
                select input
            ).ToArray();

            if (inputs?.SelectedInput == null && unselectedInputs.Length == 0)
            {
                return $"{FormatInputString(null)}";
            }
            else if (unselectedInputs.Length == 0)
            {
                // There is just one input; use that, although it doesn't make much sense
                return $"{FormatInputString(inputs?.SelectedInput)}    # warning: only one input source found for this monitor";
            }
            else if (unselectedInputs.Length == 1)
            {
                // There is exactly one other input
                return $"{FormatInputString(unselectedInputs[0])}";
            }
            else
            {
                // Multiple other inputs, use the first one and comment the rest
                var rest = string.Join(' ', unselectedInputs.Skip(1).Select(i => FormatInputString(i)));
                return $"{FormatInputString(unselectedInputs[0])}    # other options: {rest}";
            }
        }
    }

    static string FormatInputString(byte? inputByte)
    {
        if (inputByte == null)
        {
            return "# warning: no input sources found for this monitor";
        }

        var inputEnum = (
            from field in typeof(VcpValueEnum).GetFields()
            where (field.Attributes & FieldAttributes.SpecialName) == 0
                && field.GetRawConstantValue()!.Equals(inputByte)
            select field.Name
        ).SingleOrDefault();

        if (inputEnum != null)
        {
            return HyphenatedNamingConvention.Instance.Apply(inputEnum);
        }
        else
        {
            return inputByte.ToString() ?? string.Empty;
        }
    }
}
