using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Core.Events;
using System.Globalization;

namespace ShadowKVM;

internal class VcpCode
{
    public VcpCode(CodeEnum knownCode)
    {
        KnownCode = knownCode;
    }

    public VcpCode(byte code)
    {
        Code = code;
    }

    public enum CodeEnum : byte
    {
        InputSelect = 0x60
    }

    CodeEnum? _knownCode;
    public CodeEnum? KnownCode
    {
        get => _knownCode;
        set
        {
            _knownCode = value;
            _code = _knownCode != null ? (byte)_knownCode : (byte)0;
        }
    }

    byte _code;
    public byte Code
    {
        get => _code;
        set
        {
            _code = value;
            _knownCode = null;
        }
    }
}

internal class VcpCodeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(VcpCode);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var startMark = parser.Current?.Start ?? Mark.Empty;
        var endMark = parser.Current?.End ?? Mark.Empty;

        var scalar = parser.Consume<Scalar>().Value.Replace("-", string.Empty);

        VcpCode.CodeEnum knownCode;
        if (Enum.TryParse(scalar, true, out knownCode))
        {
            return new VcpCode(knownCode);
        }

        if (scalar.StartsWith("0x", true, null))
        {
            byte code;
            if (byte.TryParse(scalar.Substring(2), NumberStyles.AllowHexSpecifier, null, out code))
            {
                return new VcpCode(code);
            }
        }
        else
        {
            byte code;
            if (byte.TryParse(scalar, out code))
            {
                return new VcpCode(code);
            }
        }

        throw new YamlException(startMark, endMark, $"Invalid VCP code \"{scalar}\"");
    }

    public void WriteYaml(IEmitter emitter, object? obj, Type type, ObjectSerializer serializer)
    {
        var vcpCode = (VcpCode)obj!;

        if (vcpCode.KnownCode != null)
        {
            emitter.Emit(new Scalar(vcpCode.KnownCode?.ToString() ?? string.Empty));
        }
        else
        {
            emitter.Emit(new Scalar(vcpCode.Code.ToString()));
        }
    }
}
