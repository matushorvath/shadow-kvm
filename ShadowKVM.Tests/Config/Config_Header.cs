using System.IO.Abstractions.TestingHelpers;
using Moq;
using Serilog;
using Serilog.Events;

namespace ShadowKVM.Tests;

public class ConfigHeaderTests
{
    protected Mock<ILogger> _loggerMock = new();

    [Fact]
    public void ReloadConfig_ThrowsWithMissingVersion()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        var exception = Assert.Throws<ConfigException>(() => configService.ReloadConfig());

        Assert.Equal(@"Unsupported configuration version (found 0, supporting 1)", exception.Message);
    }

    [Fact]
    public void ReloadConfig_ThrowsWithUnsupportedVersion()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 987
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        var exception = Assert.Throws<ConfigException>(() => configService.ReloadConfig());

        Assert.Equal(@"Unsupported configuration version (found 987, supporting 1)", exception.Message);
    }

    [Fact]
    public void ReloadConfig_LoadsVersion1()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 1
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        Assert.Equal(1, configService.Config.Version);
    }

    [Fact]
    public void ReloadConfig_LoadsDefaultLogLevel()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 1
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        Assert.Equal(LogEventLevel.Information, configService.Config.LogLevel);
    }

    [Theory]
    [InlineData("verbose", LogEventLevel.Verbose)]
    [InlineData("debug", LogEventLevel.Debug)]
    [InlineData("information", LogEventLevel.Information)]
    [InlineData("warning", LogEventLevel.Warning)]
    [InlineData("error", LogEventLevel.Error)]
    [InlineData("fatal", LogEventLevel.Fatal)]
    public void ReloadConfig_LoadsEnumLogLevel(string enumString, LogEventLevel enumValue)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = $"""
                version: 1
                log-level: {enumString}
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        Assert.Equal(enumValue, configService.Config.LogLevel);
    }

    [Fact]
    public void ReloadConfig_ThrowsWithInvalidEnumLogLevel()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 1
                log-level: iNvAlIdLoGlEvEl
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        var exception = Assert.Throws<ConfigFileException>(() => configService.ReloadConfig());

        Assert.Equal(@"x:\mOcKfS\config.yaml(2,12): Exception during deserialization: Requested value 'iNvAlIdLoGlEvEl' was not found.", exception.Message);
    }

    [Fact]
    public void ReloadConfig_LoadsDefaultTriggerDevice()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 1
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        Assert.Equal(TriggerDeviceType.Keyboard, configService.Config.TriggerDevice.Enum);
        Assert.Equal(new Guid("{884b96c3-56ef-11d1-bc8c-00a0c91405dd}"), configService.Config.TriggerDevice.Raw);
    }

    [Theory]
    [InlineData("keyboard", TriggerDeviceType.Keyboard, "{884b96c3-56ef-11d1-bc8c-00a0c91405dd}")]
    [InlineData("mouse", TriggerDeviceType.Mouse, "{378DE44C-56EF-11D1-BC8C-00A0C91405DD}")]
    public void ReloadConfig_LoadsEnumTriggerDevice(string enumString, TriggerDeviceType enumValue, Guid rawValue)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = $"""
                version: 1
                trigger-device: {enumString}
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        Assert.Equal(enumValue, configService.Config.TriggerDevice.Enum);
        Assert.Equal(rawValue, configService.Config.TriggerDevice.Raw);
    }

    [Fact]
    public void ReloadConfig_LoadsGuidTriggerDevice()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 1
                trigger-device: '{266976bd-7ba2-4d38-b21c-85bd406917bd}'
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        Assert.Null(configService.Config.TriggerDevice.Enum);
        Assert.Equal(new Guid("{266976bd-7ba2-4d38-b21c-85bd406917bd}"), configService.Config.TriggerDevice.Raw);
    }

    [Fact]
    public void ReloadConfig_ThrowsWithInvalidTriggerDevice()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 1
                trigger-device: iNvAlIdDeViCe
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: hdmi1
                """
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        var exception = Assert.Throws<ConfigFileException>(() => configService.ReloadConfig());

        Assert.Equal("x:\\mOcKfS\\config.yaml(2,17): Invalid value \"iNvAlIdDeViCe\"", exception.Message);
    }

    [Fact]
    public void TriggerDeviceConstructor_ThrowsWithInvalidEnumValue()
    {
        // This code is not reachable through ConfigService
        var exception = Assert.Throws<ConfigException>(() =>
        {
            var triggerDevice = new TriggerDevice((TriggerDeviceType)(object)(-1));
        });

        Assert.Equal("Invalid trigger device type -1 in configuration file", exception.Message);
    }
}
