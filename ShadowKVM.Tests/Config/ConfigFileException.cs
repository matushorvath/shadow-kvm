using YamlDotNet.Core;

namespace ShadowKVM.Tests;

public class ConfigFileExceptionTests
{
    [Fact]
    public void SavesInnerException()
    {
        var exception = Assert.Throws<ConfigFileException>(
            void () => throw new ConfigFileException("tEsT pAtH",
                new YamlException("iNnEr mEsSaGe")));

        Assert.NotNull(exception.InnerException);
        Assert.Equal("iNnEr mEsSaGe", exception.InnerException.Message);
        Assert.IsType<YamlException>(exception.InnerException);
    }

    [Fact]
    public void FormatsMessageWithDefaultMark()
    {
        var exception = Assert.Throws<ConfigFileException>(
            void () => throw new ConfigFileException("tEsT pAtH",
                new YamlException("iNnEr mEsSaGe")));

        Assert.Equal("tEsT pAtH(1,1): iNnEr mEsSaGe", exception.Message);
    }

    [Fact]
    public void FormatsMessageWithGivenMark()
    {
        var exception = Assert.Throws<ConfigFileException>(
            void () => throw new ConfigFileException("tEsT pAtH",
                new YamlException(new Mark(42, 69, 123), Mark.Empty, "iNnEr mEsSaGe")));

        Assert.Equal("tEsT pAtH(69,123): iNnEr mEsSaGe", exception.Message);
    }

    [Fact]
    public void FormatsMessageWithInnerInnerException()
    {
        var exception = Assert.Throws<ConfigFileException>(
            void () => throw new ConfigFileException("tEsT pAtH",
                new YamlException("iNnEr mEsSaGe",
                    new Exception("InNeR iNnEr mEsSaGe"))));

        Assert.Equal("tEsT pAtH(1,1): iNnEr mEsSaGe: InNeR iNnEr mEsSaGe", exception.Message);
    }
}
