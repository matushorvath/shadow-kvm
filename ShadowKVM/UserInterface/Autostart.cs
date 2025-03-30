using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;
using Serilog;

namespace ShadowKVM;

[ExcludeFromCodeCoverage(Justification = "Wrapper for the Registry API")]
public static class Autostart
{
    static string EnabledRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    static string EnabledRegistryValue = "Shadow KVM";

    // This key should match the key used in installer
    static string ConfiguredRegistryKey = @"Software\Matúš Horváth\Shadow KVM";
    static string ConfiguredRegistryValue = "AutostartConfigured";

    // Is autostart enabled?
    public static bool IsEnabled()
    {
        return Registry.CurrentUser.OpenSubKey(EnabledRegistryKey)?.GetValue(EnabledRegistryValue) != null;
    }

    // Enable/disable autostart
    public static void SetEnabled(bool value)
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

        Log.Information("Autostart is now {Value}", value ? "enabled" : "disabled");
    }

    // Have we already configured autostart for this user in the past?
    public static bool IsConfigured()
    {
        return Registry.CurrentUser.OpenSubKey(ConfiguredRegistryKey)?.GetValue(ConfiguredRegistryValue) != null;
    }
}
