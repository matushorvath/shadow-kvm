using Windows.Win32;
using YamlDotNet.Serialization;

namespace ShadowKVM;

public enum TriggerDeviceType { Keyboard, Mouse }

public class TriggerDeviceClass : OpenEnum<TriggerDeviceType, Guid>
{
    public TriggerDeviceClass()
    {
    }

    public TriggerDeviceClass(TriggerDeviceType enumValue)
    {
        Enum = enumValue;
    }

    public TriggerDeviceClass(Guid rawValue)
    {
        Raw = rawValue;
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

public class TriggerDeviceClassTypeConverter(INamingConvention namingConvention)
        : OpenEnumYamlTypeConverter<TriggerDeviceClass, TriggerDeviceType, Guid>(namingConvention)
{
    protected override bool TryConvertStringToRaw(string str, out Guid rawValue)
    {
        return Guid.TryParse(str, out rawValue);
    }
}
