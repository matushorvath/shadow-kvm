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
        var resource = App.GetResourceStream(new Uri("pack://application:,,,/DefaultConfig.hbs"));
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
                data.Add(new Data { Monitor = monitor, Inputs = inputs });
            }
        };

        return template(new { Monitors = data });
    }
}

// MonitorInputs with additional properties needed by the template
internal class MonitorInputsForConfigTemplate : MonitorInputs
{
    public string SelectedInputHexString => $"0x{SelectedInput:X2}";
}
