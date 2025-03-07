using Windows.Win32;
using YamlDotNet.Serialization;

namespace ShadowKVM;

internal class TriggerDevice : OpenEnum<TriggerDevice.DeviceTypeEnum, Guid>
{
    public TriggerDevice()
    {
    }

    public TriggerDevice(DeviceTypeEnum enumValue)
    {
        Enum = enumValue;
    }

    public enum DeviceTypeEnum { Keyboard, Mouse }

    protected override Guid ConvertEnumToRaw(DeviceTypeEnum enumValue)
    {
        switch (enumValue)
        {
            case DeviceTypeEnum.Keyboard: return PInvoke.GUID_DEVINTERFACE_KEYBOARD;
            case DeviceTypeEnum.Mouse: return PInvoke.GUID_DEVINTERFACE_MOUSE;
            default: throw new ConfigException($"Invalid trigger device type {enumValue} in configuration file");
        }
    }
}

internal class TriggerDeviceConverter(INamingConvention namingConvention)
        : OpenEnumYamlTypeConverter<TriggerDevice, TriggerDevice.DeviceTypeEnum, Guid>(namingConvention)
{
    protected override bool TryConvertStringToRaw(string str, out Guid rawValue)
    {
        return Guid.TryParse(str, out rawValue);
    }
}
