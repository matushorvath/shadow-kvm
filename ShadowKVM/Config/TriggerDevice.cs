using Windows.Win32;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Core.Events;

namespace ShadowKVM;

internal class TriggerDevice
{
    public enum DeviceTypeEnum { Keyboard, Mouse }

    public TriggerDevice(DeviceTypeEnum deviceType)
    {
        DeviceType = deviceType;
    }

    public TriggerDevice(Guid guid)
    {
        Guid = guid;
    }

    DeviceTypeEnum? _deviceType;
    public DeviceTypeEnum? DeviceType
    {
        get => _deviceType;
        set
        {
            _deviceType = value;

            switch (_deviceType)
            {
                case DeviceTypeEnum.Keyboard:
                    _guid = PInvoke.GUID_DEVINTERFACE_KEYBOARD;
                    break;
                case DeviceTypeEnum.Mouse:
                    _guid = PInvoke.GUID_DEVINTERFACE_MOUSE;
                    break;
                default:
                    throw new ConfigException($"Invalid trigger device type {value} in configuration file");
            }
        }
    }

    Guid _guid;
    public Guid Guid
    {
        get => _guid;
        set
        {
            _guid = value;
            _deviceType = null;
        }
    }
}

internal class TriggerDeviceConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(TriggerDevice);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var startMark = parser.Current?.Start ?? Mark.Empty;
        var endMark = parser.Current?.End ?? Mark.Empty;

        var value = parser.Consume<Scalar>().Value;

        TriggerDevice.DeviceTypeEnum deviceType;
        if (Enum.TryParse(value, true, out deviceType))
        {
            return new TriggerDevice(deviceType);
        }

        Guid guid;
        if (Guid.TryParse(value, out guid))
        {
            return new TriggerDevice(guid);
        }

        throw new YamlException(startMark, endMark, $"Invalid trigger device \"{value}\"");
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var triggerDevice = (TriggerDevice)value!;

        if (triggerDevice.DeviceType != null)
        {
            emitter.Emit(new Scalar(triggerDevice.DeviceType?.ToString() ?? string.Empty));
        }
        else
        {
            emitter.Emit(new Scalar(triggerDevice.Guid.ToString("D")));
        }
    }
}
