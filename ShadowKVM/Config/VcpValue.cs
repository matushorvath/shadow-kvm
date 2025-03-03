using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Core.Events;
using System.Globalization;

namespace ShadowKVM;

internal class VcpValue
{
    public VcpValue(ValueEnum knownValue)
    {
        KnownValue = knownValue;
    }

    public VcpValue(byte value)
    {
        Value = value;
    }

    public static implicit operator byte(VcpValue vcpValue) => vcpValue.Value;

    public enum ValueEnum : byte
    {
        Analog1 = 0x01,
        Analog2 = 0x02,
        DVI1 = 0x03,
        DVI2 = 0x04,
        Composite1 = 0x05,
        Composite2 = 0x06,
        SVideo1 = 0x07,
        SVideo2 = 0x08,
        Tuner1 = 0x09,
        Tuner2 = 0x0A,
        Tuner3 = 0x0B,
        Component1 = 0x0C,
        Component2 = 0x0D,
        Component3 = 0x0E,
        DisplayPort1 = 0x0F,
        DisplayPort2 = 0x10,
        HDMI1 = 0x11,
        HDMI2 = 0x12
    }

    ValueEnum? _knownValue;
    public ValueEnum? KnownValue
    {
        get => _knownValue;
        set
        {
            _knownValue = value;
            _value = _knownValue != null ? (byte)_knownValue : (byte)0;
        }
    }

    byte _value;
    public byte Value
    {
        get => _value;
        set
        {
            _value = value;
            _knownValue = null;
        }
    }
}

internal class VcpValueConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(VcpValue);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var startMark = parser.Current?.Start ?? Mark.Empty;
        var endMark = parser.Current?.End ?? Mark.Empty;

        var scalar = parser.Consume<Scalar>().Value.Replace("-", string.Empty);;

        VcpValue.ValueEnum knownValue;
        if (Enum.TryParse(scalar, true, out knownValue))
        {
            return new VcpValue(knownValue);
        }

        if (scalar.StartsWith("0x", true, null))
        {
            byte value;
            if (byte.TryParse(scalar.Substring(2), NumberStyles.AllowHexSpecifier, null, out value))
            {
                return new VcpValue(value);
            }
        }
        else
        {
            byte value;
            if (byte.TryParse(scalar, NumberStyles.AllowHexSpecifier, null, out value))
            {
                return new VcpValue(value);
            }
        }

        throw new YamlException(startMark, endMark, $"Invalid VCP value \"{scalar}\"");
    }

    public void WriteYaml(IEmitter emitter, object? obj, Type type, ObjectSerializer serializer)
    {
        var vcpValue = (VcpValue)obj!;

        if (vcpValue.KnownValue != null)
        {
            emitter.Emit(new Scalar(vcpValue.KnownValue?.ToString() ?? string.Empty));
        }
        else
        {
            emitter.Emit(new Scalar(vcpValue.Value.ToString()));
        }
    }
}
