using HandlebarsDotNet;
using System.IO;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization.NamingConventions;

namespace ShadowKVM;

internal class ConfigGenerator
{
    class Data
    {
        public required Monitor Monitor { get; set; }
        public required MonitorInputs Inputs { get; set; }
    }

    public unsafe static string Generate()
    {
        var resource = App.GetResourceStream(new Uri("pack://application:,,,/Config/DefaultConfig.hbs"));
        var template = Handlebars.Compile(new StreamReader(resource.Stream).ReadToEnd());

        var data = new List<Data>();

        using (var monitors = new Monitors())
        {
            monitors.Load();

            foreach (var monitor in monitors)
            {
                // Determine input sources for this monitor
                var inputs = new MonitorInputsForConfigTemplate();
                inputs.Load(monitor.Handle);

                // TODO if !MonitorInputs.SupportsInputs, include the monitor but comment it out
                // TODO in the template, filter out missing display name, serial number or adapter
                if (inputs.SupportsInputs)
                {
                    data.Add(new Data { Monitor = monitor, Inputs = inputs });
                }
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

            if (unselectedInputs.Length == 0)
            {
                // There is just one input; use that, although it doesn't make much sense
                return $"{FormatInputString(SelectedInput ?? 0)}    # single valid input source found";
            }
            else if (unselectedInputs.Length == 1)
            {
                // There is exactly one other input
                return $"{FormatInputString(unselectedInputs[0])}    # no other input sources found";
            }
            else
            {
                // Multiple other inputs, use the first one and comment the rest
                var rest = string.Join(' ', unselectedInputs.Skip(1).Select(i => FormatInputString(i)));
                return $"{FormatInputString(unselectedInputs[0])}    # other input sources: {rest}";
            }
        }
    }

    static string FormatInputString(byte? inputByte)
    {
        if (inputByte == null)
        {
            return "(no input)";
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
