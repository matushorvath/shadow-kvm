using HandlebarsDotNet;
using System.IO;

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

        return template(new { Monitors = data });
    }
}

// MonitorInputs with additional properties needed by the template
internal class MonitorInputsForConfigTemplate : MonitorInputs
{
    public string SelectedInputHexString => $"0x{SelectedInput:X2}";

    public string UnselectedInputHexStringAndComment
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
                return $"0x{SelectedInput:X2}    # single valid input source found";
            }
            else if (unselectedInputs.Length == 1)
            {
                // There is exactly one other input
                return $"0x{unselectedInputs[0]:X2}    # no other input sources found";
            }
            else
            {
                // Multiple other inputs, use the first one and comment the rest
                var rest = string.Join(' ', unselectedInputs.Skip(1).Select(i => $"0x{i:X2}"));
                return $"0x{unselectedInputs[0]:X2}    # other input sources: {rest}";
            }
        }
    }
}
