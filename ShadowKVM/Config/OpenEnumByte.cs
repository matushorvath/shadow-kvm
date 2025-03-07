using System.Globalization;
using YamlDotNet.Serialization;

namespace ShadowKVM;

internal class OpenEnumByte<TEnum> : OpenEnum<TEnum, byte>
    where TEnum : struct, Enum // TEnum should also use byte as underlying type
{
    protected override byte ConvertEnumToRaw(TEnum enumValue)
    {
        if (System.Enum.GetUnderlyingType(typeof(TEnum)) != typeof(byte))
        {
            throw new InvalidOperationException("OpenEnumByte cannot convert enum value to byte");
        }

        return (byte)(object)enumValue;
    }
}

internal class OpenEnumByteYamlTypeConverter<TEnum>(INamingConvention namingConvention)
        : OpenEnumYamlTypeConverter<OpenEnumByte<TEnum>, TEnum, byte>(namingConvention)
    where TEnum : struct, Enum
{
    protected override bool TryConvertStringToRaw(string str, out byte rawValue)
    {
        if (str.StartsWith("0x", true, null))
        {
            if (byte.TryParse(str.Substring(2), NumberStyles.AllowHexSpecifier, null, out rawValue))
            {
                return true;
            }
        }
        else
        {
            if (byte.TryParse(str, out rawValue))
            {
                return true;
            }
        }

        return false;
    }
}
