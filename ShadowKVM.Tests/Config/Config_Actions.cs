using System.IO.Abstractions.TestingHelpers;
using Moq;
using Serilog;

namespace ShadowKVM.Tests;

// TODO select-device check version validation, either version 1 or 2 is acceptable, but don't mix trigger device version with config version

public class ConfigActionsTests
{
    protected Mock<ILogger> _loggerMock = new();

    [Fact]
    public void ReloadConfig_ThrowsWithMissingActions()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 1
                monitors:
                - description: dEsCrIpTiOn 1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        var exception = Assert.Throws<ConfigException>(() => configService.ReloadConfig());

        Assert.Equal(@"Either attach or detach action needs to be specified for each monitor", exception.Message);
    }

    [Fact]
    public void ReloadConfig_LoadsMonitorWithAttach()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 1
                monitors:
                  - description: dEsCrIpTiOn 1
                    attach:
                      code: input-select
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        Assert.Collection(configService.Config.Monitors ?? [], monitor =>
        {
            Assert.NotNull(monitor.Attach);
            Assert.Equal(VcpCodeEnum.InputSelect, monitor.Attach.Code.Enum);
            Assert.Null(monitor.Detach);
        });
    }

    [Fact]
    public void ReloadConfig_LoadsMonitorWithDetach()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 1
                monitors:
                  - description: dEsCrIpTiOn 1
                    detach:
                      code: input-select
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        Assert.Collection(configService.Config.Monitors ?? [], monitor =>
        {
            Assert.Null(monitor.Attach);
            Assert.NotNull(monitor.Detach);
            Assert.Equal(VcpCodeEnum.InputSelect, monitor.Detach.Code.Enum);
        });
    }

    [Fact]
    public void ReloadConfig_LoadsMonitorWithAttachAndDetach()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 1
                monitors:
                  - description: dEsCrIpTiOn 1
                    attach:
                      code: 0x42
                      value: hdmi2
                    detach:
                      code: input-select
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        Assert.Collection(configService.Config.Monitors ?? [], monitor =>
        {
            Assert.NotNull(monitor.Attach);
            Assert.Equal(0x42, monitor.Attach.Code.Raw);
            Assert.NotNull(monitor.Detach);
            Assert.Equal(VcpCodeEnum.InputSelect, monitor.Detach.Code.Enum);
        });
    }

    [Theory]
    [InlineData("input-select", VcpCodeEnum.InputSelect, 0x60)]
    public void ReloadConfig_LoadsEnumVcpCode(string enumString, VcpCodeEnum enumValue, byte rawValue)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = $"""
                version: 1
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: {enumString}
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        Assert.Collection(configService.Config.Monitors ?? [], monitor =>
        {
            Assert.NotNull(monitor.Attach);
            Assert.Equal(enumValue, monitor.Attach.Code.Enum);
            Assert.Equal(rawValue, monitor.Attach.Code.Raw);
        });
    }

    [Theory]
    [InlineData("0x4a", 0x4a)]
    [InlineData("42", 42)]
    public void ReloadConfig_LoadsByteVcpCode(string rawString, byte rawValue)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = $"""
                version: 1
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: {rawString}
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        Assert.Collection(configService.Config.Monitors ?? [], monitor =>
        {
            Assert.NotNull(monitor.Attach);
            Assert.Null(monitor.Attach.Code.Enum);
            Assert.Equal(rawValue, monitor.Attach.Code.Raw);
        });
    }

    [Theory]
    [InlineData("iNvAlIdCoDe")]
    [InlineData("-1")]
    [InlineData("256")]
    [InlineData("0xzz")]
    public void ReloadConfig_ThrowsWithInvalidVcpCode(string invalidString)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = $"""
                version: 1
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: {invalidString}
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        var exception = Assert.Throws<ConfigFileException>(() => configService.ReloadConfig());

        Assert.Equal($"x:\\mOcKfS\\config.yaml(5,13): Invalid value \"{invalidString}\"", exception.Message);
    }

    [Theory]
    [InlineData("analog1", VcpValueEnum.Analog1, 0x01)]
    [InlineData("analog2", VcpValueEnum.Analog2, 0x02)]
    [InlineData("dvi1", VcpValueEnum.Dvi1, 0x03)]
    [InlineData("dvi2", VcpValueEnum.Dvi2, 0x04)]
    [InlineData("composite1", VcpValueEnum.Composite1, 0x05)]
    [InlineData("composite2", VcpValueEnum.Composite2, 0x06)]
    [InlineData("s-video1", VcpValueEnum.SVideo1, 0x07)]
    [InlineData("s-video2", VcpValueEnum.SVideo2, 0x08)]
    [InlineData("tuner1", VcpValueEnum.Tuner1, 0x09)]
    [InlineData("tuner2", VcpValueEnum.Tuner2, 0x0A)]
    [InlineData("tuner3", VcpValueEnum.Tuner3, 0x0B)]
    [InlineData("component1", VcpValueEnum.Component1, 0x0C)]
    [InlineData("component2", VcpValueEnum.Component2, 0x0D)]
    [InlineData("component3", VcpValueEnum.Component3, 0x0E)]
    [InlineData("display-port1", VcpValueEnum.DisplayPort1, 0x0F)]
    [InlineData("display-port2", VcpValueEnum.DisplayPort2, 0x10)]
    [InlineData("hdmi1", VcpValueEnum.Hdmi1, 0x11)]
    [InlineData("hdmi2", VcpValueEnum.Hdmi2, 0x12)]
    public void ReloadConfig_LoadsEnumVcpValue(string enumString, VcpValueEnum enumValue, byte rawValue)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = $"""
                version: 1
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: {enumString}
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        Assert.Collection(configService.Config.Monitors ?? [], monitor =>
        {
            Assert.NotNull(monitor.Attach);
            Assert.Equal(enumValue, monitor.Attach.Value.Enum);
            Assert.Equal(rawValue, monitor.Attach.Value.Raw);
        });
    }

    [Theory]
    [InlineData("0x4a", 0x4a)]
    [InlineData("42", 42)]
    public void ReloadConfig_LoadsByteVcpValue(string rawString, byte rawValue)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = $"""
                version: 1
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: {rawString}
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        Assert.Collection(configService.Config.Monitors ?? [], monitor =>
        {
            Assert.NotNull(monitor.Attach);
            Assert.Null(monitor.Attach.Value.Enum);
            Assert.Equal(rawValue, monitor.Attach.Value.Raw);
        });
    }

    [Theory]
    [InlineData("iNvAlIdVaLuE")]
    [InlineData("-1")]
    [InlineData("256")]
    [InlineData("0xzz")]
    public void ReloadConfig_ThrowsWithInvalidVcpValue(string invalidString)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = $"""
                version: 1
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: {invalidString}
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        var exception = Assert.Throws<ConfigFileException>(() => configService.ReloadConfig());

        Assert.Equal($"x:\\mOcKfS\\config.yaml(6,14): Invalid value \"{invalidString}\"", exception.Message);
    }
}
