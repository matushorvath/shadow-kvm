using Moq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization.NamingConventions;

namespace ShadowKVM.Tests;

public class OpenEnumByteTests
{
    // Cover parts of OpenEnumByte that are not reachable through ConfigService

    enum ByteEnum : byte { A = 3, B = 5, C = 7 };
    enum NonByteEnum { A, B, C };

    [Fact]
    public void Constructor_ThrowsWithNonByteEnumValue()
    {
        var openEnum = new OpenEnumByte<NonByteEnum>();
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            openEnum.Enum = (NonByteEnum)(object)-1;
        });

        Assert.Equal($"OpenEnumByte cannot convert enum value to byte", exception.Message);
    }

    [Fact]
    public void Constructor_WorksWithEnumValue()
    {
        var openEnum = new OpenEnumByte<ByteEnum>(ByteEnum.B);

        Assert.Equal(ByteEnum.B, openEnum.Enum);
        Assert.Equal(5, openEnum.Raw);
    }

    [Fact]
    public void Constructor_WorksWithRawValue()
    {
        var openEnum = new OpenEnumByte<ByteEnum>(13);

        Assert.Null(openEnum.Enum);
        Assert.Equal(13, openEnum.Raw);
    }

    [Fact]
    public void Raw_ThrowsWhenNotInitialized()
    {
        var openEnum = new OpenEnumByte<ByteEnum>();

        Assert.Null(openEnum.Enum);
        Assert.Throws<NullReferenceException>(() => { var _ = openEnum.Raw; });
    }

    [Fact]
    public void OperatorTRaw_WorksWithEnumValue()
    {
        var openEnum = new OpenEnumByte<ByteEnum>(ByteEnum.C);

        Assert.Equal(7, (byte)openEnum);
    }

    [Fact]
    public void WriteYaml_ThrowsNotImplemented()
    {
        var converter = new OpenEnumByteYamlTypeConverter<ByteEnum>(HyphenatedNamingConvention.Instance);

        Assert.Throws<NotImplementedException>(() =>
            converter.WriteYaml(null!, null, typeof(byte), null!));
    }
}
