using Serilog.Events;
using System.IO.Abstractions.TestingHelpers;

namespace ShadowKVM.Tests;

public class ConfigActionsTests
{
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
        ["analog1", VcpValueEnum.Analog1, 0x01],
        ["analog2", VcpValueEnum.Analog2, 0x02],
        ["dvi1", VcpValueEnum.Dvi1, 0x03],
        ["dvi2", VcpValueEnum.Dvi2, 0x04],
        ["composite1", VcpValueEnum.Composite1, 0x05],
        ["composite2", VcpValueEnum.Composite2, 0x06],
        ["s-video1", VcpValueEnum.SVideo1, 0x07],
        ["s-video2", VcpValueEnum.SVideo2, 0x08],
        ["tuner1", VcpValueEnum.Tuner1, 0x09],
        ["tuner2", VcpValueEnum.Tuner2, 0x0A],
        ["tuner3", VcpValueEnum.Tuner3, 0x0B],
        ["component1", VcpValueEnum.Component1, 0x0C],
        ["component2", VcpValueEnum.Component2, 0x0D],
        ["component3", VcpValueEnum.Component3, 0x0E],
        ["display-port1", VcpValueEnum.DisplayPort1, 0x0F],
        ["display-port2", VcpValueEnum.DisplayPort2, 0x10],
        ["hdmi1", VcpValueEnum.Hdmi1, 0x11],
        ["hdmi2", VcpValueEnum.Hdmi2, 0x12]
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
}
