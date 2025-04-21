using System.IO.Abstractions.TestingHelpers;
using Moq;
using Serilog;

namespace ShadowKVM.Tests;

public class ConfigServiceTests
{
    protected Mock<ILogger> _loggerMock = new();

    string _validConfig = """
        version: 1
        monitors:
          - description: mOnItOr 1
            attach:
                code: input-select
                value: hdmi1
        """;

    [Fact]
    public void ReloadConfig_ReturnsFalseWithSameContent()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = _validConfig
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        // First load returns true
        Assert.True(configService.ReloadConfig());
        // Second load returns false
        Assert.False(configService.ReloadConfig());
    }

    [Fact]
    public void ReloadConfig_ReturnsTrueWithNoChecksum()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = _validConfig
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        // First load returns true
        Assert.True(configService.ReloadConfig());

        configService.Config.LoadedChecksum = null;

        // Second load returns true, since there is no checksum
        Assert.True(configService.ReloadConfig());
    }

    [Fact]
    public void ReloadConfig_ReturnsTrueWithDifferentContent()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = _validConfig
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        // First load returns true
        Assert.True(configService.ReloadConfig());

        var differentValidConfig = """
            version: 1
            monitors:
              - description: mOnItOr DiFfErEnT
                attach:
                    code: input-select
                    value: hdmi1
            """;

        fileSystem.AddFile(@"x:\mOcKfS\config.yaml", differentValidConfig);

        // Second load returns true, since the content is different
        Assert.True(configService.ReloadConfig());
    }

    [Fact]
    public void ReloadConfig_ThrowsWithNoConfigFile()
    {
        var fileSystem = new MockFileSystem();
        var configService = new ConfigService(fileSystem, _loggerMock.Object);

        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.Throws<FileNotFoundException>(() => configService.ReloadConfig());
    }

    [Fact]
    public void ReloadConfig_ThrowsWithInvalidYaml()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = "pRoP: - iNvAlIdYaMl"
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        var exception = Assert.Throws<ConfigFileException>(() => configService.ReloadConfig());

        Assert.Equal(@"x:\mOcKfS\config.yaml(1,7): Block sequence entries are not allowed in this context.", exception.Message);
    }

    [Fact]
    public void ReloadConfig_LoadsMinimumValidConfig()
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
        Assert.Collection(configService.Config.Monitors ?? [], monitor =>
        {
            Assert.Equal("mOnItOr 1", monitor.Description);
        });
    }

    [Fact]
    public void ReloadConfig_LoadsValidConfigWithComments()
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

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        Assert.Equal(1, configService.Config.Version);
        Assert.Collection(configService.Config.Monitors ?? [], monitor =>
        {
            Assert.Equal("mOnItOr 1", monitor.Description);
        });
    }

    [Fact]
    public void ReloadConfig_CalculatesCorrectChecksum()
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

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.True(configService.ReloadConfig());

        // generate: String.Join(", ", config.LoadedChecksum.Select(x => $"0x{x:x}"))
        byte[]? correctChecksum = [0xec, 0x2f, 0xb0, 0x72, 0x68, 0x99, 0x1f, 0xfa, 0x9a, 0x52, 0x2, 0xa1, 0xa3, 0xbb, 0xfb, 0x23];
        Assert.Equal(correctChecksum, configService.Config.LoadedChecksum);
    }

    [Fact]
    public void ReloadConfig_InvokesEvent()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"x:\mOcKfS\config.yaml"] = _validConfig
        });

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        var eventTriggered = false;
        configService.ConfigChanged += (sender) =>
        {
            Assert.Equal(configService, sender);
            eventTriggered = true;
        };

        configService.ReloadConfig();

        Assert.True(eventTriggered);
    }

    [Fact]
    public void Config_ThrowsBeforeLoading()
    {
        var fileSystem = new MockFileSystem();

        var configService = new ConfigService(fileSystem, _loggerMock.Object);
        configService.SetDataDirectory(@"x:\mOcKfS");

        Assert.Throws<InvalidOperationException>(() => configService.Config);
    }
}
