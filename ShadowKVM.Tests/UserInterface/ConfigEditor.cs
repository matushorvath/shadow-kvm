using Moq;
using Serilog;

namespace ShadowKVM.Tests;

public class ConfigEditorTests
{
    Mock<IConfigService> _configService = new();
    Mock<IAppControl> _appControl = new();
    Mock<INativeUserInterface> _nativeUserInterface = new();

    [Fact]
    public async Task EditConfig_WithSuccess()
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

        var editor = new ConfigEditor(_configService.Object, _appControl.Object, _nativeUserInterface.Object);

        var openedEvent = false;
        var closedEvent = false;

        editor.ConfigEditorOpened += () => { Assert.False(closedEvent); openedEvent = true; };
        editor.ConfigEditorOpened += () => { Assert.True(openedEvent); closedEvent = true; };

        await editor.EditConfig();

        _configService.Verify();
        _nativeUserInterface.Verify();

        _appControl.Verify(m => m.Shutdown(), Times.Never);

        Assert.True(openedEvent);
        Assert.True(closedEvent);
    }

    [Fact]
    public async Task EditConfig_WithRetry()
    {
        _configService
            .Setup(m => m.ConfigPath)
            .Returns("cOnFiGpAtH")
            .Verifiable();

        _nativeUserInterface
            .Setup(m => m.OpenEditor("cOnFiGpAtH"))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var attempt = 0;
        _configService
            .Setup(m => m.ReloadConfig())
            .Returns(
                () =>
                {
                    if (attempt == 0)
                    {
                        attempt++;
                        throw new ConfigException("rElOaDeRrOr");
                    }
                    else
                    {
                        return true;
                    }
                }
            )
            .Verifiable();

        var question = """
            Configuration file could not be loaded, retry editing?

            rElOaDeRrOr
            """;

        _nativeUserInterface
            .Setup(nativeUserInterface => nativeUserInterface.QuestionBox(question.ReplaceLineEndings(), "Shadow KVM"))
            .Returns(true)
            .Verifiable();

        _nativeUserInterface
            .Setup(m => m.InfoBox("Configuration file loaded successfully", "Shadow KVM"))
            .Verifiable();

        var editor = new ConfigEditor(_configService.Object, _appControl.Object, _nativeUserInterface.Object);

        var openedEvent = false;
        var closedEvent = false;

        editor.ConfigEditorOpened += () => { Assert.False(closedEvent); openedEvent = true; };
        editor.ConfigEditorOpened += () => { Assert.True(openedEvent); closedEvent = true; };

        await editor.EditConfig();

        _configService.Verify();
        _nativeUserInterface.Verify();

        _appControl.Verify(m => m.Shutdown(), Times.Never);

        Assert.True(openedEvent);
        Assert.True(closedEvent);
    }

    [Fact]
    public async Task EditConfig_WithAbort()
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
            .Throws(new ConfigException("rElOaDeRrOr"))
            .Verifiable();

        var question = """
            Configuration file could not be loaded, retry editing?

            rElOaDeRrOr
            """;

        _nativeUserInterface
            .Setup(nativeUserInterface => nativeUserInterface.QuestionBox(question.ReplaceLineEndings(), "Shadow KVM"))
            .Returns(false)
            .Verifiable();

        var editor = new ConfigEditor(_configService.Object, _appControl.Object, _nativeUserInterface.Object);

        var openedEvent = false;
        var closedEvent = false;

        editor.ConfigEditorOpened += () => { Assert.False(closedEvent); openedEvent = true; };
        editor.ConfigEditorOpened += () => { Assert.True(openedEvent); closedEvent = true; };

        await editor.EditConfig();

        _configService.Verify();
        _nativeUserInterface.Verify();

        _appControl.Verify(m => m.Shutdown(), Times.Once);

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

        var editor = new ConfigEditor(_configService.Object, _appControl.Object, _nativeUserInterface.Object);

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

        var editor = new ConfigEditor(_configService.Object, _appControl.Object, _nativeUserInterface.Object);

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

        _nativeUserInterface
            .Setup(m => m.InfoBox("Configuration file loaded successfully", "Shadow KVM"))
            .Verifiable();

        _configService
            .Setup(m => m.ReloadConfig())
            .Returns(false)
            .Verifiable();

        var editor = new ConfigEditor(_configService.Object, _appControl.Object, _nativeUserInterface.Object);
        await editor.EditConfig();

        _configService.Verify();
        _nativeUserInterface.Verify();
    }
}
