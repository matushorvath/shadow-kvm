using Moq;
using Serilog;

namespace ShadowKVM.Tests;

public class ConfigEditorTests
{
    Mock<IConfigService> _configService = new();
    Mock<INativeUserInterface> _nativeUserInterface = new();
    Mock<ILogger> _logger = new();

    [Fact]
    public async Task EditConfig_WithNoChanges()
    {
        _configService
            .Setup(m => m.ConfigPath)
            .Returns("cOnFiGpAtH")
            .Verifiable();

        _nativeUserInterface
            .Setup(m => m.OpenEditor("cOnFiGpAtH"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        _configService
            .Setup(m => m.ReloadConfig())
            .Returns(false)
            .Verifiable();

        _logger
            .Setup(m => m.Information("Configuration file has not changed, skipping reload"))
            .Verifiable();

        var editor = new ConfigEditor(_configService.Object, _nativeUserInterface.Object, _logger.Object);

        var openedEvent = false;
        var closedEvent = false;

        editor.ConfigEditorOpened += () => { Assert.False(closedEvent); openedEvent = true; };
        editor.ConfigEditorOpened += () => { Assert.True(openedEvent); closedEvent = true; };

        await editor.EditConfig();

        _configService.Verify();
        _nativeUserInterface.Verify();
        _logger.Verify();

        _nativeUserInterface.Verify(m => m.InfoBox(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        Assert.True(openedEvent);
        Assert.True(closedEvent);
    }

    [Fact]
    public async Task EditConfig_WithChanges()
    {
        _configService
            .Setup(m => m.ConfigPath)
            .Returns("cOnFiGpAtH")
            .Verifiable();

        _nativeUserInterface
            .Setup(m => m.OpenEditor("cOnFiGpAtH"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        _configService
            .Setup(m => m.ReloadConfig())
            .Returns(true)
            .Verifiable();

        _nativeUserInterface
            .Setup(m => m.InfoBox("Configuration file loaded successfully", "Shadow KVM"))
            .Verifiable();

        var editor = new ConfigEditor(_configService.Object, _nativeUserInterface.Object, _logger.Object);

        var openedEvent = false;
        var closedEvent = false;

        editor.ConfigEditorOpened += () => { Assert.False(closedEvent); openedEvent = true; };
        editor.ConfigEditorOpened += () => { Assert.True(openedEvent); closedEvent = true; };

        await editor.EditConfig();

        _configService.Verify();
        _nativeUserInterface.Verify();

        Assert.True(openedEvent);
        Assert.True(closedEvent);
    }

    [Fact]
    public async Task EditConfig_WithOpenEditorError()
    {
        _configService
            .Setup(m => m.ConfigPath)
            .Returns("cOnFiGpAtH")
            .Verifiable();

        _nativeUserInterface
            .Setup(m => m.OpenEditor("cOnFiGpAtH"))
            .Throws(new Exception("oPeNeDiToReRrOr"))
            .Verifiable();

        var editor = new ConfigEditor(_configService.Object, _nativeUserInterface.Object, _logger.Object);

        var openedEvent = false;
        var closedEvent = false;

        editor.ConfigEditorOpened += () => { Assert.False(closedEvent); openedEvent = true; };
        editor.ConfigEditorOpened += () => { Assert.True(openedEvent); closedEvent = true; };

        await Assert.ThrowsAsync<Exception>(() => editor.EditConfig());

        _configService.Verify();
        _nativeUserInterface.Verify();

        Assert.True(openedEvent);
        Assert.True(closedEvent);
    }

    [Fact]
    public async Task EditConfig_WithReloadConfigError()
    {
        _configService
            .Setup(m => m.ConfigPath)
            .Returns("cOnFiGpAtH")
            .Verifiable();

        _nativeUserInterface
            .Setup(m => m.OpenEditor("cOnFiGpAtH"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        _configService
            .Setup(m => m.ReloadConfig())
            .Throws(new Exception("rElOaDeRrOr"))
            .Verifiable();

        var editor = new ConfigEditor(_configService.Object, _nativeUserInterface.Object, _logger.Object);

        var openedEvent = false;
        var closedEvent = false;

        editor.ConfigEditorOpened += () => { Assert.False(closedEvent); openedEvent = true; };
        editor.ConfigEditorOpened += () => { Assert.True(openedEvent); closedEvent = true; };

        await Assert.ThrowsAsync<Exception>(() => editor.EditConfig());

        _configService.Verify();
        _nativeUserInterface.Verify();

        Assert.True(openedEvent);
        Assert.True(closedEvent);
    }

    [Fact]
    public async Task EditConfig_WithoutEventHandlers()
    {
        _configService
            .Setup(m => m.ConfigPath)
            .Returns("cOnFiGpAtH")
            .Verifiable();

        _nativeUserInterface
            .Setup(m => m.OpenEditor("cOnFiGpAtH"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        _configService
            .Setup(m => m.ReloadConfig())
            .Returns(false)
            .Verifiable();

        _logger
            .Setup(m => m.Information("Configuration file has not changed, skipping reload"))
            .Verifiable();

        var editor = new ConfigEditor(_configService.Object, _nativeUserInterface.Object, _logger.Object);
        await editor.EditConfig();

        _configService.Verify();
        _nativeUserInterface.Verify();
        _logger.Verify();

        _nativeUserInterface.Verify(m => m.InfoBox(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
