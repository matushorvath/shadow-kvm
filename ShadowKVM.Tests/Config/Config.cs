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
                    attach:
                      code: input-select
                      value: hdmi1
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
                    attach:
                      code: input-select
                      value: hdmi1
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
                    attach:
                      code: input-select
                      value: hdmi1
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
                    attach:
                      code: input-select
                      value: hdmi1
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
                    attach:
                      code: input-select
                      value: hdmi1
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
                    attach:
                      code: input-select
                      value: hdmi1
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
                    attach:
                      code: input-select
                      value: hdmi1
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
                    attach:
                      code: input-select
                      value: hdmi1
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
                    attach:
                      code: input-select
                      value: hdmi1
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
                    attach:
                      code: input-select
                      value: hdmi1
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

    [Fact]
    public void LoadConfig_ThrowsWithMissingMonitors()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var exception = Assert.Throws<ConfigException>(configService.LoadConfig);

        Assert.Equal(@"At least one monitor needs to be specified in configuration", exception.Message);
    }

    [Fact]
    public void LoadConfig_ThrowsWithZeroMonitors()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var exception = Assert.Throws<ConfigException>(configService.LoadConfig);

        Assert.Equal(@"At least one monitor needs to be specified in configuration", exception.Message);
    }

    [Fact]
    public void LoadConfig_ThrowsWithNoMonitorId()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                  - attach:
                      code: input-select
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var exception = Assert.Throws<ConfigException>(configService.LoadConfig);

        Assert.Equal(@"Either description, adapter or serial-number needs to be specified for each monitor", exception.Message);
    }

    [Fact]
    public void LoadConfig_LoadsMonitorWithDescription()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                  - description: dEsCrIpTiOn 1
                    attach:
                      code: input-select
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Null(monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
        });
    }

    [Fact]
    public void LoadConfig_LoadsMonitorWithAdapter()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                  - adapter: aDaPtEr 1
                    attach:
                      code: input-select
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.Null(monitor.Description);
            Assert.Equal("aDaPtEr 1", monitor.Adapter);
            Assert.Null(monitor.SerialNumber);
        });
    }

    [Fact]
    public void LoadConfig_LoadsMonitorWithSerialNumber()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                  - serial-number: sErIaLnUmBeR 1
                    attach:
                      code: input-select
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.Null(monitor.Description);
            Assert.Null(monitor.Adapter);
            Assert.Equal("sErIaLnUmBeR 1", monitor.SerialNumber);
        });
    }

    [Fact]
    public void LoadConfig_LoadsMonitorWithAllIds()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                  - description: dEsCrIpTiOn 1
                    adapter: aDaPtEr 1
                    serial-number: sErIaLnUmBeR 1
                    attach:
                      code: input-select
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.Equal("dEsCrIpTiOn 1", monitor.Description);
            Assert.Equal("aDaPtEr 1", monitor.Adapter);
            Assert.Equal("sErIaLnUmBeR 1", monitor.SerialNumber);
        });
    }

    [Fact]
    public void LoadConfig_ThrowsWithMissingActions()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                  - description: dEsCrIpTiOn 1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var exception = Assert.Throws<ConfigException>(configService.LoadConfig);

        Assert.Equal(@"Either attach or detach action needs to be specified for each monitor", exception.Message);
    }

    [Fact]
    public void LoadConfig_LoadsMonitorWithAttach()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                  - description: dEsCrIpTiOn 1
                    attach:
                      code: input-select
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.NotNull(monitor.Attach);
            Assert.Equal(VcpCodeEnum.InputSelect, monitor.Attach.Code.Enum);
            Assert.Null(monitor.Detach);
        });
    }

    [Fact]
    public void LoadConfig_LoadsMonitorWithDetach()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                  - description: dEsCrIpTiOn 1
                    detach:
                      code: input-select
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.Null(monitor.Attach);
            Assert.NotNull(monitor.Detach);
            Assert.Equal(VcpCodeEnum.InputSelect, monitor.Detach.Code.Enum);
        });
    }

    [Fact]
    public void LoadConfig_LoadsMonitorWithAttachAndDetach()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData(@"
                version: 1
                monitors:
                  - description: dEsCrIpTiOn 1
                    attach:
                      code: 0x42
                      value: hdmi2
                    detach:
                      code: input-select
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.NotNull(monitor.Attach);
            Assert.Equal(0x42, monitor.Attach.Code.Raw);
            Assert.NotNull(monitor.Detach);
            Assert.Equal(VcpCodeEnum.InputSelect, monitor.Detach.Code.Enum);
        });
    }

    public static IEnumerable<object[]> ValidEnumVcpCodes =>
    [
        ["input-select", VcpCodeEnum.InputSelect, 0x60]
    ];

    [Theory]
    [MemberData(nameof(ValidEnumVcpCodes))]
    public void LoadConfig_LoadsEnumVcpCode(string enumString, VcpCodeEnum enumValue, byte rawValue)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData($@"
                version: 1
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: {enumString}
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.NotNull(monitor.Attach);
            Assert.Equal(enumValue, monitor.Attach.Code.Enum);
            Assert.Equal(rawValue, monitor.Attach.Code.Raw);
        });
    }

    public static IEnumerable<object[]> ValidRawVcpCodes =>
    [
        ["0x4a", 0x4a],
        ["42", 42]
    ];

    [Theory]
    [MemberData(nameof(ValidRawVcpCodes))]
    public void LoadConfig_LoadsByteVcpCode(string rawString, byte rawValue)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData($@"
                version: 1
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: {rawString}
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.NotNull(monitor.Attach);
            Assert.Null(monitor.Attach.Code.Enum);
            Assert.Equal(rawValue, monitor.Attach.Code.Raw);
        });
    }

    public static IEnumerable<object[]> InvalidRawVcpCodes =>
    [
        ["iNvAlIdCoDe"], ["-1"], ["256"], ["0xzz"]
    ];

    [Theory]
    [MemberData(nameof(InvalidRawVcpCodes))]
    public void LoadConfig_ThrowsWithInvalidVcpCode(string invalidString)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData($@"
                version: 1
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: {invalidString}
                      value: hdmi1
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var exception = Assert.Throws<ConfigFileException>(configService.LoadConfig);

        Assert.Equal($"x:\\mOcKfS\\config.yaml(6,29): Invalid value \"{invalidString}\"", exception.Message);
    }

    public static IEnumerable<object[]> ValidEnumVcpValues =>
    [
        ["Analog1", VcpValueEnum.Analog1, 0x01],
        ["Analog2", VcpValueEnum.Analog2, 0x02],
        ["Dvi1", VcpValueEnum.Dvi1, 0x03],
        ["Dvi2", VcpValueEnum.Dvi2, 0x04],
        ["Composite1", VcpValueEnum.Composite1, 0x05],
        ["Composite2", VcpValueEnum.Composite2, 0x06],
        ["SVideo1", VcpValueEnum.SVideo1, 0x07],
        ["SVideo2", VcpValueEnum.SVideo2, 0x08],
        ["Tuner1", VcpValueEnum.Tuner1, 0x09],
        ["Tuner2", VcpValueEnum.Tuner2, 0x0A],
        ["Tuner3", VcpValueEnum.Tuner3, 0x0B],
        ["Component1", VcpValueEnum.Component1, 0x0C],
        ["Component2", VcpValueEnum.Component2, 0x0D],
        ["Component3", VcpValueEnum.Component3, 0x0E],
        ["DisplayPort1", VcpValueEnum.DisplayPort1, 0x0F],
        ["DisplayPort2", VcpValueEnum.DisplayPort2, 0x10],
        ["Hdmi1", VcpValueEnum.Hdmi1, 0x11],
        ["Hdmi2", VcpValueEnum.Hdmi2, 0x12]
    ];

    [Theory]
    [MemberData(nameof(ValidEnumVcpValues))]
    public void LoadConfig_LoadsEnumVcpValue(string enumString, VcpValueEnum enumValue, byte rawValue)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData($@"
                version: 1
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: {enumString}
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.NotNull(monitor.Attach);
            Assert.Equal(enumValue, monitor.Attach.Value.Enum);
            Assert.Equal(rawValue, monitor.Attach.Value.Raw);
        });
    }

    public static IEnumerable<object[]> ValidRawVcpValues =>
    [
        ["0x4a", 0x4a],
        ["42", 42]
    ];

    [Theory]
    [MemberData(nameof(ValidRawVcpValues))]
    public void LoadConfig_LoadsByteVcpValue(string rawString, byte rawValue)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData($@"
                version: 1
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: {rawString}
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.NotNull(monitor.Attach);
            Assert.Null(monitor.Attach.Value.Enum);
            Assert.Equal(rawValue, monitor.Attach.Value.Raw);
        });
    }

    public static IEnumerable<object[]> InvalidRawVcpValues =>
    [
        ["iNvAlIdVaLuE"], ["-1"], ["256"], ["0xzz"]
    ];

    [Theory]
    [MemberData(nameof(InvalidRawVcpValues))]
    public void LoadConfig_ThrowsWithInvalidVcpValue(string invalidString)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData($@"
                version: 1
                monitors:
                  - description: mOnItOr 1
                    attach:
                      code: input-select
                      value: {invalidString}
            ") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var exception = Assert.Throws<ConfigFileException>(configService.LoadConfig);

        Assert.Equal($"x:\\mOcKfS\\config.yaml(7,30): Invalid value \"{invalidString}\"", exception.Message);
    }

    enum OpenEnumByte_NonByteEnum { A, B, C };

    [Fact]
    public void OpenEnumByteVcpCodeEnumConstructor_ThrowsWithNonByteEnumValue()
    {
        // This code is not reachable through ConfigService
        var openEnum = new OpenEnumByte<OpenEnumByte_NonByteEnum>();
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            openEnum.Enum = (OpenEnumByte_NonByteEnum)(object)-1;
        });

        Assert.Equal($"OpenEnumByte cannot convert enum value to byte", exception.Message);
    }

    // TODO
    // test also serialization of vcp value/code, or disable the code
    // test with three monitors
}
