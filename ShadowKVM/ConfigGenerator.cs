using HandlebarsDotNet;
using System.IO;
using Windows.Win32;
using Windows.Win32.Devices.Display;

namespace ShadowKVM;

internal class ConfigGenerator
{
    class Data
    {
        public required Monitor Device { get; set; }
        public required string SelectedInput { get; set; }
    }

    public unsafe static string Generate()
    {
        var resource = App.GetResourceStream(new Uri("pack://application:,,,/DefaultConfig.hbs"));
        var template = Handlebars.Compile(new StreamReader(resource.Stream).ReadToEnd());

        var data = new List<Data>();

        using (var monitors = new Monitors())
        {
            monitors.Refresh();

            foreach (var monitor in monitors)
            {
                // Find out which inputs are supported by this monitor
                // TODO

                // Find out which input is currently selected for this monitor
                var vct = new MC_VCP_CODE_TYPE();
                uint selectedInput;

                int success = PInvoke.GetVCPFeatureAndVCPFeatureReply(monitor.Handle, 0x60, &vct, out selectedInput, null);
                if (success != 1 || vct != MC_VCP_CODE_TYPE.MC_SET_PARAMETER)
                {
                    // TODO include the monitor but comment it out
                    continue;
                }
                selectedInput = selectedInput & 0xff;

                data.Add(new Data
                {
                    Device = monitor,
                    SelectedInput = $"0x{selectedInput:X2}"
                });
            }

            return template(new { Monitors = data });
        }
    }
}
