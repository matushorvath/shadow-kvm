using HandlebarsDotNet;
using System.IO;

namespace ShadowKVM;

internal class ConfigGenerator
{
    public static string Generate()
    {
        var resource = App.GetResourceStream(new Uri("pack://application:,,,/DefaultConfig.hbs"));
        var template = Handlebars.Compile(new StreamReader(resource.Stream).ReadToEnd());

        using (var monitors = new Monitors())
        {
            monitors.Refresh();

            return template(new { monitors });
        }
    }
}
