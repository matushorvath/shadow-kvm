using Serilog.Events;
using System.IO.Abstractions.TestingHelpers;

namespace ShadowKVM.Tests;

public class ConfigTests
{
    [Fact]
    public void LoadConfig_ThrowsWithMissingVersion()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                monitors:
                - description: mOnItOr 1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var exception = Assert.Throws<ConfigException>(configService.LoadConfig);

        Assert.Equal(@"Unsupported configuration version (found 0, supporting 1)", exception.Message);
    }

    [Fact]
    public void LoadConfig_ThrowsWithUnsupportedVersion()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 987
                monitors:
                - description: mOnItOr 1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var exception = Assert.Throws<ConfigException>(configService.LoadConfig);

        Assert.Equal(@"Unsupported configuration version (found 987, supporting 1)", exception.Message);
    }

    [Fact]
    public void LoadConfig_LoadsVersion1()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                - description: mOnItOr 1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Equal(1, config.Version);
    }

    [Fact]
    public void LoadConfig_LoadsDefaultLogLevel()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                - description: mOnItOr 1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Equal(LogEventLevel.Information, config.LogLevel);
    }

    public static IEnumerable<object[]> ValidEnumLogLevels =>
    [
        ["verbose", LogEventLevel.Verbose],
        ["debug", LogEventLevel.Debug],
        ["information", LogEventLevel.Information],
        ["warning", LogEventLevel.Warning],
        ["error", LogEventLevel.Error],
        ["fatal", LogEventLevel.Fatal]
    ];

    [Theory]
    [MemberData(nameof(ValidEnumLogLevels))]
    public void LoadConfig_LoadsEnumLogLevel(string enumString, LogEventLevel enumValue)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData($@"
                version: 1
                log-level: {enumString}
                monitors:
                - description: mOnItOr 1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Equal(enumValue, config.LogLevel);
    }

    [Fact]
    public void LoadConfig_ThrowsWithInvalidEnumLogLevel()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                log-level: iNvAlIdLoGlEvEl
                monitors:
                - description: mOnItOr 1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var exception = Assert.Throws<ConfigFileException>(configService.LoadConfig);

        Assert.Equal(@"x:\mOcKfS\config.yaml(3,28): Exception during deserialization: Requested value 'iNvAlIdLoGlEvEl' was not found.", exception.Message);
    }

    [Fact]
    public void LoadConfig_LoadsDefaultTriggerDevice()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                - description: mOnItOr 1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Equal(TriggerDeviceType.Keyboard, config.TriggerDevice.Enum);
        Assert.Equal(new Guid("{884b96c3-56ef-11d1-bc8c-00a0c91405dd}"), config.TriggerDevice.Raw);
    }

    public static IEnumerable<object[]> ValidEnumTriggerDevices =>
    [
        ["keyboard", TriggerDeviceType.Keyboard, new Guid("{884b96c3-56ef-11d1-bc8c-00a0c91405dd}")],
        ["mouse", TriggerDeviceType.Mouse, new Guid("{378DE44C-56EF-11D1-BC8C-00A0C91405DD}")]
    ];

    [Theory]
    [MemberData(nameof(ValidEnumTriggerDevices))]
    public void LoadConfig_LoadsEnumTriggerDevice(string enumString, TriggerDeviceType enumValue, Guid rawValue)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData($@"
                version: 1
                trigger-device: {enumString}
                monitors:
                - description: mOnItOr 1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Equal(enumValue, config.TriggerDevice.Enum);
        Assert.Equal(rawValue, config.TriggerDevice.Raw);
    }

    [Fact]
    public void LoadConfig_LoadsGuidTriggerDevice()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                trigger-device: '{266976bd-7ba2-4d38-b21c-85bd406917bd}'
                monitors:
                - description: mOnItOr 1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Null(config.TriggerDevice.Enum);
        Assert.Equal(new Guid("{266976bd-7ba2-4d38-b21c-85bd406917bd}"), config.TriggerDevice.Raw);
    }

    [Fact]
    public void LoadConfig_ThrowsWithInvalidTriggerDevice()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                trigger-device: iNvAlIdDeViCe
                monitors:
                - description: mOnItOr 1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var exception = Assert.Throws<ConfigFileException>(configService.LoadConfig);

        Assert.Equal("x:\\mOcKfS\\config.yaml(3,33): Invalid value \"iNvAlIdDeViCe\"", exception.Message);
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

    // TODO
    // fail with no monitors
    // test with description/adapter/serial missing
    // fail with all three missing
    // test with three monitors
    // test with no actions, attach or detach, both
    // test with all valid vcp values, vcp codes
    // fail with invalid string vcpvalue, vcp code
    // test with numeric vcp value, vcp code
    // test with > 256 vcp value, vcp code, also with < 0
    // test also serialization of vcp value/code, or disable the code
}
