using HandlebarsDotNet;
using System.IO;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization.NamingConventions;

namespace ShadowKVM;

public class ConfigGeneratorStatus
{
    public int Current { get; set; }
    public int Maximum { get; set; }
}

internal interface IConfigGenerator
{
    public string Generate(IProgress<ConfigGeneratorStatus>? progress);
}

internal class ConfigGenerator(IMonitorService monitorService) : IConfigGenerator
{
    class Data
    {
        public required Monitor Monitor { get; set; }
        public required MonitorInputs Inputs { get; set; }
    }

    public unsafe string Generate(IProgress<ConfigGeneratorStatus>? progress)
    {
        var resource = App.GetResourceStream(new Uri("pack://application:,,,/Config/DefaultConfig.hbs"));
        var template = Handlebars.Compile(new StreamReader(resource.Stream).ReadToEnd());

        var data = new List<Data>();

        using (var monitors = monitorService.LoadMonitors())
        {
            var status = new ConfigGeneratorStatus { Current = 0, Maximum = monitors.Count() };
            progress?.Report(status);

            foreach (var monitor in monitors)
            {
                // Determine input sources for this monitor
                // TODO each inputs.Load takes 2 seconds, do them in parallel
                var inputs = new MonitorInputsForConfigTemplate();
                inputs.Load(monitor.Handle);

                status.Current++;
                progress?.Report(status);

                data.Add(new Data { Monitor = monitor, Inputs = inputs });
            }
        };

        var common = new CommonDataForConfigTemplate();
        return template(new { Common = common, Monitors = data });
    }
}

internal class CommonDataForConfigTemplate
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

// MonitorInputs with additional properties needed by the template
internal class MonitorInputsForConfigTemplate : MonitorInputs
{
    public string CommentUnsupported => SupportsInputs ? "  " : "# ";

    public string SelectedInputString => FormatInputString(SelectedInput);

    public string UnselectedInputStringAndComment
    {
        get
        {
            // Choose a random input that wasn't selected
            var unselectedInputs = (
                from input in ValidInputs
                where input != SelectedInput
                select input
            ).ToArray();

            if (SelectedInput == null && unselectedInputs.Length == 0)
            {
                return $"{FormatInputString(null)}";
            }
            else if (unselectedInputs.Length == 0)
            {
                // There is just one input; use that, although it doesn't make much sense
                return $"{FormatInputString(SelectedInput)}    # warning: only one input source found for this monitor";
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
