using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace ShadowKVM.Tests;

public class ConfigServiceTests
{
    [Fact]
    public void Constructor_WorksWithRealFilesystem()
    {
        var configService = new ConfigService(@"x:\mOcKfS");
        Assert.IsType<FileSystem>(configService.FileSystem);
    }

    [Fact]
    public void NeedReloadConfig_ReturnsFalseWithSameContent()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData("mOcKcOnFiG") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);

        var config = new Config();

        // MD5 checksum of "mOcKcOnFiG"
        config.LoadedChecksum = [0x75, 0x38, 0x23, 0x20, 0xb1, 0xd0, 0x19, 0x23, 0x3d, 0x9f, 0xd0, 0xf4, 0x3c, 0x3b, 0x93, 0xbf];

        Assert.False(configService.NeedReloadConfig(config));
    }

    [Fact]
    public void NeedReloadConfig_ReturnsTrueWithNoChecksum()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData("mOcKcOnFiG") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = new Config();

        Assert.True(configService.NeedReloadConfig(config));
    }

    [Fact]
    public void NeedReloadConfig_ReturnsTrueWithDifferentContent()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData("mOcKcOnFiG") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);

        var config = new Config();

        // MD5 checksum of "mOcKcOnFiG"
        config.LoadedChecksum = [0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0, 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0];

        Assert.True(configService.NeedReloadConfig(config));
    }

    [Fact]
    public void LoadConfig_ThrowsWithNoConfigFile()
    {
        var fileSystem = new MockFileSystem();
        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);

        Assert.Throws<FileNotFoundException>(configService.LoadConfig);
    }

    [Fact]
    public void LoadConfig_ThrowsWithInvalidYaml()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"x:\mOcKfS\config.yaml", new MockFileData("pRoP: - iNvAlIdYaMl") }
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);

        var exception = Assert.Throws<ConfigFileException>(configService.LoadConfig);

        Assert.Matches(@"^x:\\mOcKfS\\config\.yaml\(1,7\): .+\.$", exception.Message);
    }

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

    // TODO
    // test with default log level
    // test with custom log level (debug, verbose)
    // fail with invalid log level (asdf)
    // test with default trigger device
    // test with custom trigger device (mouse)
    // fail with invalid trigger device (asdf)
    // test with a guid trigger device
    // fail with invalid guid trigger device (bad guid format)
    // fail with no monitors
    // test with description/adapter/serial missing
    // fail with all three missing
    // test with three monitors
    // test with comments
    // test that checksum gets calculated correctly
    // test with no actions, attach or detach, both
    // test with all valid vcp values, vcp codes
    // fail with invalid string vcpvalue, vcp code
    // test with numeric vcp value, vcp code
    // test with > 256 vcp value, vcp code, also with < 0
    // test also serialization of vcp value/code, or disable the code

    [Fact]
    public void LoadConfig_LoadsMinimumValidConfig()
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
        Assert.Collection(config.Monitors, monitor =>
        {
            Assert.Equal("mOnItOr 1", monitor.Description);
        });
    }
}
