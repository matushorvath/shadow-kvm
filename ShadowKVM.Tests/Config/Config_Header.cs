using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Moq;
using Serilog;
using Serilog.Events;
using YamlDotNet.Core;

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

        Assert.Equal(@"Unsupported configuration version (found 0, supporting <= 2)", exception.Message);
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

        Assert.Equal(@"Unsupported configuration version (found 987, supporting <= 2)", exception.Message);
    }

    [Fact]
    public void ReloadConfig_ThrowsWithExtraTriggerDeviceProperties()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 2
                trigger-device:
                    nOnSeNsE: BaD
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

        Assert.Equal(@"x:\mOcKfS\config.yaml(3,5): Unexpected property nOnSeNsE", exception.Message);
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
    public void ReloadConfig_LoadsVersion2()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 2
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

        Assert.Equal(2, configService.Config.Version);
    }

    [Fact]
    public void ReloadConfig_LoadsDefaultLogLevel()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 2
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
                version: 2
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
                version: 2
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
                version: 2
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

        Assert.Equal(TriggerDeviceType.Keyboard, configService.Config.TriggerDevice.Class.Enum);
        Assert.Equal(new Guid("{884b96c3-56ef-11d1-bc8c-00a0c91405dd}"), configService.Config.TriggerDevice.Class.Raw);
    }

    [Theory]
    [InlineData("keyboard", TriggerDeviceType.Keyboard, "{884b96c3-56ef-11d1-bc8c-00a0c91405dd}")]
    [InlineData("mouse", TriggerDeviceType.Mouse, "{378DE44C-56EF-11D1-BC8C-00A0C91405DD}")]
    public void ReloadConfig_LoadsEnumTriggerDeviceVersion1(string enumString, TriggerDeviceType enumValue, Guid rawValue)
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

        Assert.Equal(enumValue, configService.Config.TriggerDevice.Class.Enum);
        Assert.Equal(rawValue, configService.Config.TriggerDevice.Class.Raw);
    }

    [Theory]
    [InlineData("keyboard", TriggerDeviceType.Keyboard, "{884b96c3-56ef-11d1-bc8c-00a0c91405dd}")]
    [InlineData("mouse", TriggerDeviceType.Mouse, "{378DE44C-56EF-11D1-BC8C-00A0C91405DD}")]
    public void ReloadConfig_LoadsEnumTriggerDeviceVersion2(string enumString, TriggerDeviceType enumValue, Guid rawValue)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = $"""
                version: 2
                trigger-device:
                    class: {enumString}
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

        Assert.Equal(enumValue, configService.Config.TriggerDevice.Class.Enum);
        Assert.Equal(rawValue, configService.Config.TriggerDevice.Class.Raw);
    }

    [Fact]
    public void ReloadConfig_LoadsGuidTriggerDeviceVersion1()
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

        Assert.Null(configService.Config.TriggerDevice.Class.Enum);
        Assert.Equal(new Guid("{266976bd-7ba2-4d38-b21c-85bd406917bd}"), configService.Config.TriggerDevice.Class.Raw);
    }

    [Fact]
    public void ReloadConfig_LoadsGuidTriggerDeviceVersion2()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 2
                trigger-device:
                    class: '{266976bd-7ba2-4d38-b21c-85bd406917bd}'
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

        Assert.Null(configService.Config.TriggerDevice.Class.Enum);
        Assert.Equal(new Guid("{266976bd-7ba2-4d38-b21c-85bd406917bd}"), configService.Config.TriggerDevice.Class.Raw);
    }

    [Theory]
    [InlineData("{266976bd-7ba2-4d38-b21c-85bd406917bd}", null, 0xC52B)]
    [InlineData("{266976bd-7ba2-4d38-b21c-85bd406917bd}", 0x046D, null)]
    [InlineData("{266976bd-7ba2-4d38-b21c-85bd406917bd}", 0x046D, 0xC52B)]
    public void ReloadConfig_LoadsVidPidTriggerDeviceVersion2(string guid, int? vid, int? pid)
    {
        var triggerDeviceYaml = new StringBuilder();
        triggerDeviceYaml.Append($"    class: '{guid}'");

        // Intentionally swapped order of VID and PID, to test that the order does not matter
        if (pid != null)
        {
            triggerDeviceYaml.Append($"\n    product-id: {pid:x}");
        }
        if (vid != null)
        {
            triggerDeviceYaml.Append($"\n    vendor-id: {vid:x}");
        }

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = $"""
                version: 2
                trigger-device:
                {triggerDeviceYaml.ToString()}
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

        Assert.Null(configService.Config.TriggerDevice.Class.Enum);
        Assert.Equal(new Guid(guid), configService.Config.TriggerDevice.Class.Raw);

        Assert.Equal(vid, configService.Config.TriggerDevice.VendorId);
        Assert.Equal(pid, configService.Config.TriggerDevice.ProductId);
    }

    [Fact]
    public void ReloadConfig_ThrowsWithInvalidTriggerDeviceVersion1()
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
    public void ReloadConfig_ThrowsWithInvalidTriggerDeviceVersion2()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                version: 2
                trigger-device:
                    class: iNvAlIdDeViCe
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

        Assert.Equal("x:\\mOcKfS\\config.yaml(3,12): Invalid value \"iNvAlIdDeViCe\"", exception.Message);
    }

    [Fact]
    public void TriggerDeviceConstructor_ThrowsWithInvalidEnumValue()
    {
        // This code is not reachable through ConfigService
        var exception = Assert.Throws<ConfigException>(() =>
        {
            var triggerDevice = new TriggerDeviceClass((TriggerDeviceType)(object)(-1));
        });

        Assert.Equal("Invalid trigger device type -1 in configuration file", exception.Message);
    }
}
