using YamlDotNet.Core;

namespace ShadowKVM.Tests;

public class ConfigFileExceptionTests
{
    [Fact]
    public void Constructor_SavesInnerException()
    {
        var exception = Assert.Throws<ConfigYamlException>(
            void () => throw new ConfigYamlException("tEsT pAtH",
                new YamlException("iNnEr mEsSaGe")));

        Assert.NotNull(exception.InnerException);
        Assert.Equal("iNnEr mEsSaGe", exception.InnerException.Message);
        Assert.IsType<YamlException>(exception.InnerException);
    }

    [Fact]
    public void Message_IsCorrectWithDefaultMark()
    {
        var exception = Assert.Throws<ConfigYamlException>(
            void () => throw new ConfigYamlException("tEsT pAtH",
                new YamlException("iNnEr mEsSaGe")));

        Assert.Equal("tEsT pAtH(1,1): iNnEr mEsSaGe", exception.Message);
    }

    [Fact]
    public void Message_IsCorrectWithSpecificMark()
    {
        var exception = Assert.Throws<ConfigYamlException>(
            void () => throw new ConfigYamlException("tEsT pAtH",
                new YamlException(new Mark(42, 78, 123), Mark.Empty, "iNnEr mEsSaGe")));

        Assert.Equal("tEsT pAtH(78,123): iNnEr mEsSaGe", exception.Message);
    }

    [Fact]
    public void Message_IsCorrectWithInnerInnerException()
    {
        var exception = Assert.Throws<ConfigYamlException>(
            void () => throw new ConfigYamlException("tEsT pAtH",
                new YamlException("iNnEr mEsSaGe",
                    new Exception("InNeR iNnEr mEsSaGe"))));

        Assert.Equal("tEsT pAtH(1,1): iNnEr mEsSaGe: InNeR iNnEr mEsSaGe", exception.Message);
    }
}
