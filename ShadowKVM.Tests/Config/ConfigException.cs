namespace ShadowKVM.Tests;

public class ConfigExceptionTests
{
    [Fact]
    public void Constructor_WorksWithoutInnerException()
    {
        var exception = Assert.Throws<ConfigException>(
            void () => throw new ConfigException("tEsT mEsSaGe"));

        Assert.Equal("tEsT mEsSaGe", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_SavesInnerException()
    {
        var exception = Assert.Throws<ConfigException>(
            void () => throw new ConfigException("tEsT mEsSaGe", new Exception("iNnEr mEsSaGe")));

        Assert.Equal("tEsT mEsSaGe", exception.Message);
        Assert.NotNull(exception.InnerException);
        Assert.Equal("iNnEr mEsSaGe", exception.InnerException.Message);
    }
}
