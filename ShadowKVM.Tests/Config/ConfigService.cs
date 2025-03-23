using System.IO.Abstractions.TestingHelpers;

namespace ShadowKVM.Tests;

public class ConfigServiceTests
{
    [Fact]
    public void NeedReloadConfig_ReturnsFalseWithSameContent()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = "mOcKcOnFiG"
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = new Config
        {
            // MD5 checksum of "mOcKcOnFiG"
            LoadedChecksum = [0x75, 0x38, 0x23, 0x20, 0xb1, 0xd0, 0x19, 0x23, 0x3d, 0x9f, 0xd0, 0xf4, 0x3c, 0x3b, 0x93, 0xbf]
        };

        Assert.False(configService.NeedReloadConfig(config));
    }

    [Fact]
    public void NeedReloadConfig_ReturnsTrueWithNoChecksum()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = "mOcKcOnFiG"
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
            [@"x:\mOcKfS\config.yaml"] = "mOcKcOnFiG"
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = new Config
        {
            // MD5 checksum of "mOcKcOnFiG"
            LoadedChecksum = [0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0, 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0]
        };

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
            [@"x:\mOcKfS\config.yaml"] = "pRoP: - iNvAlIdYaMl"
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var exception = Assert.Throws<ConfigFileException>(configService.LoadConfig);

        Assert.Equal(@"x:\mOcKfS\config.yaml(1,7): Block sequence entries are not allowed in this context.", exception.Message);
    }

    [Fact]
    public void LoadConfig_LoadsMinimumValidConfig()
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

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Equal(1, config.Version);
        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.Equal("mOnItOr 1", monitor.Description);
        });
    }

    [Fact]
    public void LoadConfig_LoadsValidConfigWithComments()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = """
                # cOmMeNt1
                version: 1 # cOmMeNt2
                # cOmMeNt3
                monitors:
                  # cOmMeNt4
                  - description: mOnItOr 1 # cOmMeNt5
                    attach:
                      code: input-select
                      value: hdmi1
                  # cOmMeNt6
                """
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        Assert.Equal(1, config.Version);
        Assert.Collection(config.Monitors ?? [], monitor =>
        {
            Assert.Equal("mOnItOr 1", monitor.Description);
        });
    }

    [Fact]
    public void LoadConfig_CalculatesCorrectChecksum()
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
                """.ReplaceLineEndings("\r\n")
        });

        var configService = new ConfigService(@"x:\mOcKfS", fileSystem);
        var config = configService.LoadConfig();

        // generate: String.Join(", ", config.LoadedChecksum.Select(x => $"0x{x:x}"))
        byte[]? correctChecksum = [0xec, 0x2f, 0xb0, 0x72, 0x68, 0x99, 0x1f, 0xfa, 0x9a, 0x52, 0x2, 0xa1, 0xa3, 0xbb, 0xfb, 0x23];
        Assert.Equal(correctChecksum, config.LoadedChecksum);
    }
}
