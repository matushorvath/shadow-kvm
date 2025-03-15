using Windows.Win32;
using YamlDotNet.Serialization;

namespace ShadowKVM;

public enum TriggerDeviceType { Keyboard, Mouse }

internal class TriggerDevice : OpenEnum<TriggerDeviceType, Guid>
{
    public TriggerDevice()
    {
    }

    public TriggerDevice(TriggerDeviceType enumValue)
    {
        Enum = enumValue;
    }

    protected override Guid ConvertEnumToRaw(TriggerDeviceType enumValue)
    {
        switch (enumValue)
        {
            case TriggerDeviceType.Keyboard: return PInvoke.GUID_DEVINTERFACE_KEYBOARD;
            case TriggerDeviceType.Mouse: return PInvoke.GUID_DEVINTERFACE_MOUSE;
            default: throw new ConfigException($"Invalid trigger device type {enumValue} in configuration file");
        }
    }
}

internal class TriggerDeviceConverter(INamingConvention namingConvention)
        : OpenEnumYamlTypeConverter<TriggerDevice, TriggerDeviceType, Guid>(namingConvention)
{
    protected override bool TryConvertStringToRaw(string str, out Guid rawValue)
    {
        return Guid.TryParse(str, out rawValue);
    }
}
