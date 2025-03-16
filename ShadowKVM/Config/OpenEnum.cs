using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Core.Events;

namespace ShadowKVM;

internal abstract class OpenEnum<TEnum, TRaw>
    where TEnum : struct, Enum
{
    public OpenEnum()
    {
    }

    public OpenEnum(TEnum enumValue)
    {
        _enumValue = enumValue;
        _rawValue = ConvertEnumToRaw(enumValue);
    }

    public OpenEnum(TRaw rawValue)
    {
        _rawValue = rawValue;
    }

    public static implicit operator TRaw(OpenEnum<TEnum, TRaw> openEnum) => openEnum.Raw;

    TEnum? _enumValue;
    public TEnum? Enum
    {
        get => _enumValue;
        set
        {
            _enumValue = value;

            if (_enumValue != null)
            {
                _rawValue = ConvertEnumToRaw(_enumValue ?? default);
            }
        }
    }

    TRaw? _rawValue;
    public TRaw Raw
    {
        get => _rawValue ?? throw new NullReferenceException("Null reference while getting OpenEnum value");
        set
        {
            _rawValue = value;
            _enumValue = default;
        }
    }

    protected abstract TRaw ConvertEnumToRaw(TEnum enumValue);
}

internal abstract class OpenEnumYamlTypeConverter<TOpenEnum, TEnum, TRaw>(INamingConvention namingConvention) : IYamlTypeConverter
    where TOpenEnum : OpenEnum<TEnum, TRaw>, new()
    where TEnum : struct, Enum
{
    public bool Accepts(Type type) => type == typeof(TOpenEnum);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var startMark = parser.Current?.Start ?? Mark.Empty;
        var endMark = parser.Current?.End ?? Mark.Empty;

        var scalar = parser.Consume<Scalar>().Value;
        var reversedScalar = namingConvention.Reverse(scalar);

        TEnum enumValue;
        if (Enum.TryParse(reversedScalar, out enumValue) && Enum.IsDefined<TEnum>(enumValue))
        {
            var openEnum = new TOpenEnum();
            openEnum.Enum = enumValue;
            return openEnum;
        }

        TRaw rawValue;
        if (TryConvertStringToRaw(scalar, out rawValue))
        {
            var openEnum = new TOpenEnum();
            openEnum.Raw = rawValue;
            return openEnum;
        }

        throw new YamlException(startMark, endMark, $"Invalid value \"{scalar}\"");
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var openEnum = (TOpenEnum)value!;

        if (openEnum.Enum != null)
        {
            var yamlEnumValue = namingConvention.Apply(openEnum.Enum?.ToString() ?? string.Empty);
            emitter.Emit(new Scalar(yamlEnumValue));
        }
        else
        {
            emitter.Emit(new Scalar(openEnum.Raw!.ToString() ?? string.Empty));
        }
    }

    protected abstract bool TryConvertStringToRaw(string str, out TRaw rawValue);
}
