using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;
using Serilog;

namespace ShadowKVM;

public interface IAutostart
{
    bool IsEnabled();
    void SetEnabled(bool value);
    bool IsConfigured();
}

[ExcludeFromCodeCoverage(Justification = "Productive implementation of the Autostart interface")]
public class Autostart(ILogger logger) : IAutostart
{
    static string EnabledRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    static string EnabledRegistryValue = "Shadow KVM";

    // This key should match the key used in installer
    static string ConfiguredRegistryKey = @"Software\Matúš Horváth\Shadow KVM";
    static string ConfiguredRegistryValue = "AutostartConfigured";

    // Is autostart enabled?
    public bool IsEnabled()
    {
        return Registry.CurrentUser.OpenSubKey(EnabledRegistryKey)?.GetValue(EnabledRegistryValue) != null;
    }

    // Enable/disable autostart
    public void SetEnabled(bool value)
    {
        if (value)
        {
            string executable = $"\"{Environment.ProcessPath}\"";
            Registry.CurrentUser.CreateSubKey(EnabledRegistryKey, true).SetValue(EnabledRegistryValue, executable);
        }
        else
        {
            Registry.CurrentUser.OpenSubKey(EnabledRegistryKey, true)?.DeleteValue(EnabledRegistryValue, false);
        }

        Registry.CurrentUser.CreateSubKey(ConfiguredRegistryKey, true).SetValue(ConfiguredRegistryValue, 1);

        logger.Information("Autostart is now {Value}", value ? "enabled" : "disabled");
    }

    // Have we already configured autostart for this user in the past?
    public bool IsConfigured()
    {
        return Registry.CurrentUser.OpenSubKey(ConfiguredRegistryKey)?.GetValue(ConfiguredRegistryValue) != null;
    }
}
